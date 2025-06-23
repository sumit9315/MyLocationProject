using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hestia.LocationsMDM.WebApi.Common;
using Hestia.LocationsMDM.WebApi.Config;
using Hestia.LocationsMDM.WebApi.Models;
using Hestia.LocationsMDM.WebApi.Models.CalendarEvent;
using MDM.Tools.Common;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hestia.LocationsMDM.WebApi.Services.Impl
{
    /// <summary>
    /// The Change History service.
    /// </summary>
    public class ChangeHistoryService : BaseCosmosService, IChangeHistoryService
    {
        private const string CampusObjectType = "campus";
        private const string RegionObjectType = "region";
        private const string ChildLocationObjectType = "childLocation";
        private const string PricingRegionMappingObjectType = "pricingRegionMapping";
        private const string CalendarEventMassUpdateObjectType = "calendarEventMassUpdate";

        private static readonly string[] ChangedPropsToIgnore = new string[]
        {
            "_rid",
            "_self",
            "_etag",
            "_attachments",
            "_ts",
            "id",
            "recStatus",
            "recEffDt",
            "recExpDt",
            "recSource",
            "recSourceFile",
            "partition_key"
        };

        private static readonly string[] SimpleStringArrayProps = new string[]
        {
            "valueAddedServices",
            "brands",
            "productOffering",
            "servicesAndCertification"
        };

        private static readonly string[] FinancialDataDates = new string[]
        {
            "lobCcEffDate",
            "lobCcCloseDate"
        };

        private static readonly string[] BooleanFields = new string[]
        {
            "openforBusinessFlag",
            "openforPurchaseFlag",
            "openforShipping",
            "openforReceiving",
            "proPickup",
            "selfCheckout",
            "staffUnStaffProPickup",
            "openforPublicDiscloure",
            "buyOnline",
            "availableToStorefront",
        };

        private readonly ILogger<ChangeHistoryService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeHistoryService" /> class.
        /// </summary>
        /// <param name="cosmosClient">The cosmos client.</param>
        /// <param name="appContextProvider">The application context provider.</param>
        /// <param name="cosmosConfig">The cosmos configuration.</param>
        /// <param name="logger">The logger.</param>
        public ChangeHistoryService(
            CosmosClient cosmosClient,
            IOptions<CosmosConfig> cosmosConfig,
            IAppContextProvider appContextProvider,
            ILogger<ChangeHistoryService> logger)
            : base(cosmosClient, cosmosConfig, appContextProvider)
        {
            _logger = logger;
        }

        #region Common Locations (Campus/Region/ChildLoc) change history

        /// <summary>
        /// Adds the pricing region change for given Locations.
        /// </summary>
        /// <param name="changedLocationObjects">The changed location objects.</param>
        public async Task AddLocationsPricingRegionChangeAsync(IList<ChangedObject> changedLocationObjects)
        {
            const string attrName = "pricingRegion";

            foreach (var changedObject in changedLocationObjects)
            {
                // get corresponding object type
                string pk = changedObject.NewObject.Value<string>("partition_key");
                NodeType nodeType = Util.GetNodeType(CosmosConfig, pk);
                string objectType = ToObjectType(nodeType);

                var changes = new List<AttributeChangeModel>();

                // get old/new values (for Child Loc compare strings, for Campus and Region compare attributes)
                if (nodeType == NodeType.ChildLoc)
                {
                    var oldArray = changedObject.OldObject.FinancialData();
                    var newArray = changedObject.NewObject.FinancialData();
                    for (int i = 0; i < oldArray.Count; i++)
                    {
                        string oldValue = oldArray[i].Value<string>(attrName);
                        string newValue = newArray[i].Value<string>(attrName);
                        changes.Add(new AttributeChangeModel
                        {
                            AttributeName = $"financialData.{attrName}",
                            OldValue = oldValue,
                            NewValue = newValue
                        });
                    }
                }
                else
                {
                    var oldValues = changedObject.OldObject.GetStringList(attrName);
                    var newValues = changedObject.NewObject.GetStringList(attrName);

                    string oldValue = oldValues.Except(newValues).FirstOrDefault() ?? "";
                    string newValue = newValues.Except(oldValues).FirstOrDefault() ?? "";
                    changes.Add(new AttributeChangeModel
                    {
                        AttributeName = attrName,
                        OldValue = oldValue,
                        NewValue = newValue
                    });
                }

                // get location node
                string locNode = changedObject.NewObject.Value<string>("node");

                // save history
                await AddObjectChangesAndUpdateSummary(objectType, locNode, changedObject.NewObject, changes);
            }
        }

        /// <summary>
        /// Changes the object Id ('node' for locations) in all change history/summary records.
        /// </summary>
        /// <param name="oldObjectId">The old object Id.</param>
        /// <param name="newObjectId">The new object Id.</param>
        public async Task ChangeObjectIdAsync(string oldObjectId, string newObjectId)
        {
            await ChangeObjectIdAsync(CosmosConfig.ChangeHistoryPartitionKey, "objectId", oldObjectId, newObjectId);
            await ChangeObjectIdAsync(CosmosConfig.ChangeSummaryPartitionKey, "locationId", oldObjectId, newObjectId);
        }

        private async Task ChangeObjectIdAsync(string partitionKey, string attrName, string oldObjectId, string newObjectId)
        {
            string sql = $"SELECT * FROM c WHERE c.partition_key='{partitionKey}'" +
                $" AND c.{attrName}='{oldObjectId}'";

            var items = await GetAllItemsAsync<JObject>(CosmosConfig.LocationsContainerName, sql, partitionKey);
            foreach (var item in items)
            {
                // set last change details
                item[attrName] = newObjectId;

                // create or update summary
                await UpsertItemAsync(CosmosConfig.LocationsContainerName, partitionKey, item);
            }
        }

        #endregion


        #region Campus change history

        /// <summary>
        /// Adds the Campus change history/summary.
        /// </summary>
        /// <param name="oldObj">The old object.</param>
        /// <param name="newObj">The new object.</param>
        public async Task AddCampusChangesAsync(JObject oldObj, JObject newObj)
        {
            // build attribute changes
            var attributeChanges = GetAttributeChanges(newObj, oldObj, path: "").ToList();

            string campusId = newObj.Value<string>("node");
            await AddObjectChangesAndUpdateSummary(CampusObjectType, campusId, newObj, attributeChanges);
        }

        /// <summary>
        /// Adds the Associates history for Campus.
        /// </summary>
        /// <param name="campus">The Campus.</param>
        /// <param name="oldAssociates">The old associates.</param>
        /// <param name="newAssociates">The new associates.</param>
        public async Task AddCampusAssociatesChangesAsync(JObject campus, IList<AssociateModel> oldAssociates, IList<AssociateModel> newAssociates)
        {
            var changes = GetAssociatesChanges(oldAssociates, newAssociates);

            string campusId = campus.Value<string>("node");
            await AddObjectChangesAndUpdateSummary(CampusObjectType, campusId, campus, changes);
        }

        #endregion


        #region Region change history

        /// <summary>
        /// Adds the Region change history/summary.
        /// </summary>
        /// <param name="oldObj">The old object.</param>
        /// <param name="newObj">The new object.</param>
        public async Task AddRegionChangesAsync(JObject oldObj, JObject newObj)
        {
            // build attribute changes
            var attributeChanges = GetAttributeChanges(newObj, oldObj, path: "").ToList();

            string regionId = newObj.Value<string>("node");
            await AddObjectChangesAndUpdateSummary(RegionObjectType, regionId, newObj, attributeChanges);
        }

        /// <summary>
        /// Adds the Associates history for Region.
        /// </summary>
        /// <param name="region">The Region.</param>
        /// <param name="oldAssociates">The old associates.</param>
        /// <param name="newAssociates">The new associates.</param>
        public async Task AddRegionAssociatesChangesAsync(JObject region, IList<AssociateModel> oldAssociates, IList<AssociateModel> newAssociates)
        {
            var changes = GetAssociatesChanges(oldAssociates, newAssociates);

            string regionId = region.Value<string>("node");
            await AddObjectChangesAndUpdateSummary(RegionObjectType, regionId, region, changes);
        }

        #endregion


        #region Child Location change history

        /// <summary>
        /// Adds the Child Location change history/summary.
        /// </summary>
        /// <param name="oldObj">The old object.</param>
        /// <param name="newObj">The new object.</param>
        public async Task AddChildLocationChangesAsync(JObject oldObj, JObject newObj)
        {
            // build attribute changes
            var attributeChanges = GetAttributeChanges(newObj, oldObj, path: "").ToList();

            string node = newObj.Value<string>("node");
            await AddObjectChangesAndUpdateSummary(ChildLocationObjectType, node, newObj, attributeChanges);
        }

        /// <summary>
        /// Adds the child location created changes.
        /// </summary>
        /// <param name="childLocNode">The child location identifier.</param>
        /// <param name="childLoc">The child location model.</param>
        /// <returns>
        /// The task.
        /// </returns>
        public async Task AddChildLocationCreatedChangesAsync(string childLocNode, JObject childLoc)
        {
            var changes = new List<AttributeChangeModel>
            {
                new AttributeChangeModel
                {
                    AttributeName = "childLocation",
                    NewValue = childLocNode
                },
                new AttributeChangeModel
                {
                    AttributeName = "locationID",
                    NewValue = childLoc.Value<string>("locationID")
                },
                new AttributeChangeModel
                {
                    AttributeName = "locationName",
                    NewValue = childLoc.Value<string>("locationName")
                },
                new AttributeChangeModel
                {
                    AttributeName = "locationType",
                    NewValue = childLoc.Value<string>("locationType")
                }
            };

            await AddObjectChangesAndUpdateSummary(ChildLocationObjectType, childLocNode, childLoc, changes);
        }

        /// <summary>
        /// Adds the child location deleted changes.
        /// </summary>
        /// <param name="childLocNode">The child location identifier.</param>
        /// <param name="childLoc">The child location model.</param>
        /// <returns>The task.</returns>
        public async Task AddChildLocationDeletedChangesAsync(string childLocNode, JObject childLoc)
        {
            var changes = new List<AttributeChangeModel>
            {
                new AttributeChangeModel
                {
                    AttributeName = "childLocation",
                    OldValue = childLocNode
                }
            };

            var regionId = childLoc.Value<string>("regionNodeId");
            await AddObjectChangesAndUpdateSummary(RegionObjectType, regionId, childLoc, changes);
        }

        #region Child Location - Associates change history.

        /// <summary>
        /// Adds the Associates history for Child Location.
        /// </summary>
        /// <param name="childLocation">The child location.</param>
        /// <param name="oldAssociates">The old associates.</param>
        /// <param name="newAssociates">The new associates.</param>
        public async Task AddChildLocationAssociatesChangesAsync(JObject childLocation, IList<AssociateModel> oldAssociates, IList<AssociateModel> newAssociates)
        {
            var changes = GetAssociatesChanges(oldAssociates, newAssociates);

            string node = childLocation.Value<string>("node");
            await AddObjectChangesAndUpdateSummary(ChildLocationObjectType, node, childLocation, changes);
        }

        #endregion

        #region Child Location - Events change history.

        /// <summary>
        /// Adds the Events history for Location.
        /// </summary>
        /// <param name="nodeType">Type of the node.</param>
        /// <param name="location">The location object.</param>
        /// <param name="addedEvents">The added events.</param>
        /// <param name="removedEvents">The removed events.</param>
        /// <param name="changedEvents">The changed events.</param>
        public async Task AddLocationEventsChangesAsync(NodeType nodeType, JObject location, IList<JObject> addedEvents = null, IList<JObject> removedEvents = null, IList<ChangedObject> changedEvents = null)
        {
            var attributesToSkip = new string[]
            {
                "eventType",
                "locationNode",
                "eventGuid",
                "parentGuid",
                "eventStartDateTime",
                "eventEndDateTime",
                "isFullDay",
                "eventDuration",
                "displaySequence"
            };

            var changes = new List<AttributeChangeModel>();

            // handle Removed events (Removed events should go first in the order, then Added events)
            if (removedEvents != null)
            {
                foreach (var item in removedEvents)
                {
                    var itemChanges = GetDeletedItemAttributeChanges(item);
                    changes.AddRange(itemChanges);
                }
            }

            // handle Added events
            if (addedEvents != null)
            {
                foreach (var item in addedEvents)
                {
                    var itemChanges = GetCreatedItemAttributeChanges(item);
                    changes.AddRange(itemChanges);
                }
            }

            // handle Changed events
            if (changedEvents != null)
            {
                foreach (var item in changedEvents)
                {
                    var itemChanges = GetAttributeChanges(item.NewObject, item.OldObject);
                    changes.AddRange(itemChanges);
                }
            }

            // remove skipped attributes
            changes = changes.Where(x => !attributesToSkip.Contains(x.AttributeName)).ToList();

            string node = location.Value<string>("node");
            string objectType = ToObjectType(nodeType);
            await AddObjectChangesAndUpdateSummary(objectType, node, location, changes);
        }

        public async Task<dynamic> AddHistoryForChildLocBulkEventAsync(IList<JObject> childLocs, JObject addedEvent)
        {
            var attributesToSkip = new string[]
            {
                "eventType",
                "locationNode",
                "eventGuid",
                "parentGuid",
                "eventStartDateTime",
                "eventEndDateTime",
                "isFullDay",
                "eventDuration",
                "displaySequence"
            };

            // get attribute changes
            var changes = GetCreatedItemAttributeChanges(addedEvent)
                // remove skipped attributes
                .Where(x => !attributesToSkip.Contains(x.AttributeName))
                // take only changed values
                .Where(x => x.OldValue != x.NewValue)
                .ToList();

            // get common props
            var now = DateTime.UtcNow;
            var editedby = _appContextProvider.GetCurrentUserFullName();
            string objectType = ToObjectType(NodeType.ChildLoc);

            // add changes for all child locations
            var historyWatch = Stopwatch.StartNew();
            var historyBatch = new UnlimitedTransactionalBatch(() => CreateTransactionalBatch(CosmosConfig.LocationsContainerName, CosmosConfig.ChangeHistoryPartitionKey));
            foreach (var item in childLocs)
            {
                string node = item.Value<string>("node");
                string locationName = item.Value<string>("locationName");

                var address = item["address"];
                string state = address?.Value<string>("state");
                string cityName = address?.Value<string>("cityName");

                // add history record for each change
                foreach (AttributeChangeModel change in changes)
                {
                    var changeModel = new ObjectChangeModel
                    {
                        LocationName = locationName,
                        ObjectId = node,
                        ObjectType = objectType,
                        State = state,
                        CityName = cityName,
                        ChangedOn = now,
                        EditedBy = editedby,
                        ChangedAttribute = change
                    };

                    // NOTE: Maybe no need to set Rec attributes? Don't see a reason for it, except maybe date
                    var changeModelObj = PrepareNewObjectProperties(changeModel, CosmosConfig.ChangeHistoryPartitionKey);

                    // add to batch
                    historyBatch.CreateItem(changeModelObj);
                }
            }
            await historyBatch.ExecuteAsync();
            historyWatch.Stop();


            // get existing summaries for all child locations
            var loadSummaryWatch = Stopwatch.StartNew();
            var allNodes = childLocs
                .Select(x => x.Value<string>("node"))
                .ToList();

            var whereQuerySb = new StringBuilder($"where c.partition_key='{CosmosConfig.ChangeSummaryPartitionKey}'")
                .AppendEqualsCondition("objectType", objectType)
                .AppendContainsCondition("locationId", allNodes);

            string sql = $"select * from c {whereQuerySb}";
            var existingSummaries = await GetAllItemsAsync<JObject>(CosmosConfig.LocationsContainerName, sql);
            var existingSummaryLookup = existingSummaries.ToDictionary(x => x.Value<string>("objectId"));
            loadSummaryWatch.Stop();

            // add summary for all child locations
            var summaryWatch = Stopwatch.StartNew();
            var summaryBatch = new UnlimitedTransactionalBatch(() => CreateTransactionalBatch(CosmosConfig.LocationsContainerName, CosmosConfig.ChangeSummaryPartitionKey));
            foreach (var item in childLocs)
            {
                string node = item.Value<string>("node");

                // try get existing summary
                if (!existingSummaryLookup.TryGetValue(node, out JObject summaryObj))
                {
                    // create change summary
                    var summary = new LocationChangeSummary
                    {
                        LocationId = node
                    };
                    summaryObj = PrepareNewObjectProperties(summary, CosmosConfig.ChangeSummaryPartitionKey, setRecFields: false);
                }

                // set object info updated fields
                summaryObj["locationName"] = item.Value<string>("locationName");
                summaryObj["locationNodeType"] = NodeType.ChildLoc.ToString();
                summaryObj["address"] = item["address"];
                summaryObj["region"] = item.Value<string>("regionName");
                summaryObj["editedBy"] = editedby;

                // set last change details
                summaryObj["lastChangedDate"] = DateTime.UtcNow;

                // create or update summary
                summaryBatch.UpsertItem(summaryObj);
            }
            await summaryBatch.ExecuteAsync();
            summaryWatch.Stop();

            // return timing
            return new
            {
                addHistorySec = Math.Round(historyWatch.Elapsed.TotalSeconds, 3),
                loadSummarySec = Math.Round(loadSummaryWatch.Elapsed.TotalSeconds, 3),
                addSummarySec = Math.Round(summaryWatch.Elapsed.TotalSeconds, 3)
            };
        }

        /// <summary>
        /// Loads the calendar events.
        /// </summary>
        /// <param name="eventIds">The calendar event ids.</param>
        /// <returns>The calendar events.</returns>
        private async Task<IList<JObject>> LoadCalendarEventsAsync(IList<string> eventIds)
        {
            if (eventIds?.Count > 0)
            {
                var eventIdsCsv = string.Join("','", eventIds);

                // load Events
                string[] propsToRetrieve =
                    {
                    "c.eventId",
                    "c.eventName",
                    "c.eventStartDay",
                    "c.eventEndDay",
                    "c.eventStartTime",
                    "c.eventEndTime"
                };

                string propList = string.Join(", ", propsToRetrieve);
                string sql = $"select {propList} from c where c.partition_key='{CosmosConfig.CalendarEventPartitionKey}' and c.eventId IN('{eventIdsCsv}') {ActiveOrDeletedRecordFilter}";
                var events = await GetAllItemsAsync<JObject>(CosmosConfig.SecondaryContainerName, sql);
                return events;
            }

            return new List<JObject>();
        }

        #endregion

        #region Child Location - Operationg Hours change history.

        /// <summary>
        /// Adds the Operating Hours history for Child Location.
        /// </summary>
        /// <param name="childLocation">The child location.</param>
        /// <param name="newOperatingHours">The new operating hours.</param>
        public async Task AddChildLocationOperatingHoursChangesAsync(JObject childLocation, IList<OperatingHoursModel> newOperatingHours)
        {
            // f) Operating hours - Create operating hour - we show null for old and "<Day of the week> <Open Hour> - <Close Hour>" for new
            // g) Operating hours - Change operating hour - we show  "<Day of the week> <Open Hour> - <Close Hour>"  for old and "<Day of the week> <New Open Hour> - <New Close Hour>" for new

            var oldOperatingHoursArray = (childLocation["operatingHours"] as JArray) ?? new JArray();

            // construct models for old values
            var oldOperatingHours = new List<OperatingHoursModel>();
            foreach (var item in oldOperatingHoursArray)
            {
                string dayOfWeek = item.Value<string>("day");

                var model = new OperatingHoursModel
                {
                    DayOfWeek = (int)Enum.Parse<DayOfWeek>(dayOfWeek),
                    DayOfWeekName = item.Value<string>("day"),
                    OpenHour = item.Value<string>("openHour"),
                    CloseHour = item.Value<string>("closeHour"),
                    OpenAfterHour = item.Value<string>("openAfterHour"),
                    CloseAfterHour = item.Value<string>("closeAfterHour"),
                    OpenReceivingHour = item.Value<string>("openReceivingHour"),
                    CloseReceivingHour = item.Value<string>("closeReceivingHour"),
                    OpenCloseFlag = item.Value<string>("openCloseFlag")
                };
                oldOperatingHours.Add(model);
            }

            var changes = new List<AttributeChangeModel>();

            // handle new/updated items
            foreach (var newItem in newOperatingHours)
            {
                var oldItem = oldOperatingHours.FirstOrDefault(x => x.DayOfWeek == newItem.DayOfWeek);

                if (oldItem != null &&
                    IsTimeSame(oldItem.OpenHour, newItem.OpenHour) &&
                    IsTimeSame(oldItem.CloseHour, newItem.CloseHour) &&
                    IsTimeSame(oldItem.OpenAfterHour, newItem.OpenAfterHour) &&
                    IsTimeSame(oldItem.CloseAfterHour, newItem.CloseAfterHour) &&
                    IsTimeSame(oldItem.OpenReceivingHour, newItem.OpenReceivingHour) &&
                    IsTimeSame(oldItem.CloseReceivingHour, newItem.CloseReceivingHour) &&
                    oldItem.OpenCloseFlag == newItem.OpenCloseFlag)
                {
                    // ignore if none of 'required' fields are changed
                    continue;
                }

                // changed for open hour
                if (!IsTimeSame(oldItem.OpenHour, newItem.OpenHour) ||
                    !IsTimeSame(oldItem.CloseHour, newItem.CloseHour) ||
                    oldItem.OpenCloseFlag != newItem.OpenCloseFlag)
                {
                    var oldItemDescr = oldItem.OpenCloseFlag == "Open"
                            ? $"{oldItem.DayOfWeekName} {oldItem.OpenHour} - {oldItem.CloseHour}"
                            : $"{oldItem.DayOfWeekName} Closed";

                    var newItemDescr = newItem.OpenCloseFlag == "Open"
                        ? $"{newItem.DayOfWeekName} {newItem.OpenHour} - {newItem.CloseHour}"
                        : $"{newItem.DayOfWeekName} Closed";

                    var change = new AttributeChangeModel
                    {
                        AttributeName = "operatingHours",
                        OldValue = oldItemDescr,
                        NewValue = newItemDescr
                    };
                    changes.Add(change);
                }
                if (!IsTimeSame(oldItem.OpenAfterHour, newItem.OpenAfterHour) ||
                    !IsTimeSame(oldItem.CloseAfterHour, newItem.CloseAfterHour))
                {
                    var oldItemDescr = oldItem.OpenAfterHour != null
                            ? $"{oldItem.DayOfWeekName} {oldItem.OpenAfterHour} - {oldItem.CloseAfterHour}"
                            : null;

                    var newItemDescr = newItem.OpenAfterHour != null
                        ? $"{newItem.DayOfWeekName} {newItem.OpenAfterHour} - {newItem.CloseAfterHour}"
                        : null;

                    var change = new AttributeChangeModel
                    {
                        AttributeName = "operatingHours.after",
                        OldValue = oldItemDescr,
                        NewValue = newItemDescr
                    };
                    changes.Add(change);
                }
                if (!IsTimeSame(oldItem.OpenReceivingHour, newItem.OpenReceivingHour) ||
                    !IsTimeSame(oldItem.CloseReceivingHour, newItem.CloseReceivingHour))
                {
                    var oldItemDescr = oldItem.OpenAfterHour != null
                            ? $"{oldItem.DayOfWeekName} {oldItem.OpenReceivingHour} - {oldItem.CloseReceivingHour}"
                            : null;

                    var newItemDescr = newItem.OpenAfterHour != null
                        ? $"{newItem.DayOfWeekName} {newItem.OpenReceivingHour} - {newItem.CloseReceivingHour}"
                        : null;

                    var change = new AttributeChangeModel
                    {
                        AttributeName = "operatingHours.receiving",
                        OldValue = oldItemDescr,
                        NewValue = newItemDescr
                    };
                    changes.Add(change);
                }
            }

            string node = childLocation.Value<string>("node");
            await AddObjectChangesAndUpdateSummary(ChildLocationObjectType, node, childLocation, changes);
        }

        #endregion

        #region Child Location - Professional Associates change history.

        /// <summary>
        /// Adds the Professional Associates history for Child Location.
        /// </summary>
        /// <param name="childLocObj">The child location.</param>
        /// <param name="oldItems">The old Professional Associates.</param>
        /// <param name="newItems">The new Professional Associates.</param>
        public async Task AddChildLocationProfessionalAssociatesChangesAsync(JObject childLocObj, IList<ProfessionalAssociationModel> oldItems, IList<ProfessionalAssociationModel> newItems)
        {
            const string attributePrefix = "professionalAssociation";
            await AddArrayItemChanges(attributePrefix, childLocObj, oldItems, newItems);
        }

        #endregion

        #region Child Location - Merchandising Banners change history.

        /// <summary>
        /// Adds the Merchandising Banners history for Child Location.
        /// </summary>
        /// <param name="childLocObj">The child location.</param>
        /// <param name="oldItems">The old Merchandising Banners.</param>
        /// <param name="newItems">The new Merchandising Banners.</param>
        public async Task AddChildLocationMerchandisingBannersChangesAsync(JObject childLocObj, IList<MerchandisingBannerModel> oldItems, IList<MerchandisingBannerModel> newItems)
        {
            const string attributePrefix = "merchandisingBanner";
            await AddArrayItemChanges(attributePrefix, childLocObj, oldItems, newItems);
        }

        #endregion

        #region Child Location - Calendar Event Mass Update changes history.

        /// <summary>
        /// Adds the Calendar Event Mass Update history for Child Location.
        /// </summary>
        /// <param name="childLocObj">The child location.</param>
        /// <param name="removedEventIds">The old Merchandising Banners.</param>
        /// <param name="addedEventIds">The new Merchandising Banners.</param>
        public async Task AddChildLocationCalendarEventMassUpdateChangesAsync(JObject childLocObj, IList<string> removedEventIds, IList<string> addedEventIds)
        {
            string removedEventIdsStr = removedEventIds?.StringJoin(Environment.NewLine);
            string addedEventIdsStr = addedEventIds?.StringJoin(Environment.NewLine);

            var childLocChanges = new List<AttributeChangeModel>
            {
                new AttributeChangeModel
                {
                    AttributeName = "calendarEventMassUpdate",
                    OldValue = removedEventIdsStr,
                    NewValue = addedEventIdsStr
                }
            };

            // save history
            await AddObjectChangesAndUpdateSummary(ChildLocationObjectType, childLocObj.Node(), childLocObj, childLocChanges);
        }

        #endregion

        #endregion


        #region Pricing Region change history

        /// <summary>
        /// Adds the pricing region created changes.
        /// </summary>
        /// <param name="model">The pricing region mapping.</param>
        public async Task AddPricingRegionCreatedChangesAsync(PricingRegionMappingModel model)
        {
            var changes = new List<AttributeChangeModel>
            {
                new AttributeChangeModel
                {
                    AttributeName = "pricingRegionId",
                    NewValue = model.PricingRegionId
                },
                new AttributeChangeModel
                {
                    AttributeName = "trilogieLogon",
                    NewValue = model.TrilogieLogon
                },
                new AttributeChangeModel
                {
                    AttributeName = "pricingRegion",
                    NewValue = model.PricingRegion
                },
            };

            await AddNonLocationObjectChangesAsync(PricingRegionMappingObjectType, model.PricingRegionId, changes);
        }

        /// <summary>
        /// Adds the pricing region deleted changes.
        /// </summary>
        /// <param name="model">The pricing region mapping.</param>
        public async Task AddPricingRegionDeletedChangesAsync(PricingRegionMappingModel model)
        {
            var changes = new List<AttributeChangeModel>
            {
                new AttributeChangeModel
                {
                    AttributeName = "pricingRegionId",
                    OldValue = model.PricingRegionId
                },
                new AttributeChangeModel
                {
                    AttributeName = "trilogieLogon",
                    OldValue = model.TrilogieLogon
                },
                new AttributeChangeModel
                {
                    AttributeName = "pricingRegion",
                    OldValue = model.PricingRegion
                },
            };

            await AddNonLocationObjectChangesAsync(PricingRegionMappingObjectType, model.PricingRegionId, changes);
        }

        /// <summary>
        /// Adds the pricing region updated changes.
        /// </summary>
        /// <param name="oldModel">The old pricing region mapping.</param>
        /// <param name="newModel">The new pricing region mapping.</param>
        public async Task AddPricingRegionUpdatedChangesAsync(PricingRegionMappingModel oldModel, PricingRegionMappingModel newModel)
        {
            var changes = new List<AttributeChangeModel>
            {
                // NOTE: update of Trilogie Logon is disabled for now
                //new AttributeChangeModel
                //{
                //    AttributeName = "trilogieLogon",
                //    OldValue = oldModel.TrilogieLogon,
                //    NewValue = newModel.TrilogieLogon
                //},
                new AttributeChangeModel
                {
                    AttributeName = "pricingRegion",
                    OldValue = oldModel.PricingRegion,
                    NewValue = newModel.PricingRegion
                },
            };

            await AddNonLocationObjectChangesAsync(PricingRegionMappingObjectType, newModel.PricingRegionId, changes);
        }

        #endregion


        #region Calendar Event Mass Update

        /// <summary>
        /// Adds the Calendar Events Mass Update created changes.
        /// </summary>
        /// <param name="massUpdate">The mass update data.</param>
        /// <param name="childLocs">The affcted child locations.</param>
        public async Task AddCalendarEventMassUpdateCreatedChangesAsync(CalendarEventMassUpdate massUpdate, IList<JObject> childLocs)
        {
            // Step 1: Add Mass Update history
            var changes = new List<AttributeChangeModel>
            {
                new AttributeChangeModel
                {
                    AttributeName = "massUpdate.id",
                    NewValue = massUpdate.MassUpdateId
                },
                new AttributeChangeModel
                {
                    AttributeName = "massUpdate.title",
                    NewValue = massUpdate.Title
                },
                new AttributeChangeModel
                {
                    AttributeName = "massUpdate.description",
                    NewValue = massUpdate.Description
                },
                new AttributeChangeModel
                {
                    AttributeName = "massUpdate.filter",
                    NewValue = Util.GetCalendarEventMassUpdateFilterDescription(massUpdate.Filter)
                }
            };
            await AddNonLocationObjectChangesAsync(CalendarEventMassUpdateObjectType, massUpdate.MassUpdateId, changes);

            // Step 2: Add Child Location history
            string massUpdateEventIds = massUpdate.CalendarEvents
                .Select(x => x.EventId)
                .StringJoin(Environment.NewLine);

            foreach (var childLoc in childLocs)
            {
                var childLocChanges = new List<AttributeChangeModel>
                {
                    new AttributeChangeModel
                    {
                        AttributeName = "calendarEventMassUpdate",
                        NewValue = massUpdateEventIds
                    }
                };

                // save history
                await AddObjectChangesAndUpdateSummary(ChildLocationObjectType, childLoc.Node(), childLoc, childLocChanges);
            }
        }

        /// <summary>
        /// Adds the Calendar Events Mass Update deleted changes.
        /// </summary>
        /// <param name="massUpdate">The deleted mass update data.</param>
        /// <param name="childLocs">The affcted child locations.</param>
        public async Task AddCalendarEventMassUpdateDeletedChangesAsync(CalendarEventMassUpdate massUpdate, IList<JObject> childLocs)
        {
            // Step 1: Add Mass Update history
            var changes = new List<AttributeChangeModel>
            {
                new AttributeChangeModel
                {
                    AttributeName = "massUpdate.id",
                    OldValue = massUpdate.MassUpdateId
                },
                new AttributeChangeModel
                {
                    AttributeName = "massUpdate.title",
                    OldValue = massUpdate.Title
                },
                new AttributeChangeModel
                {
                    AttributeName = "massUpdate.description",
                    OldValue = massUpdate.Description
                },
                new AttributeChangeModel
                {
                    AttributeName = "massUpdate.filter",
                    OldValue = Util.GetCalendarEventMassUpdateFilterDescription(massUpdate.Filter)
                }
            };
            await AddNonLocationObjectChangesAsync(CalendarEventMassUpdateObjectType, massUpdate.MassUpdateId, changes);
        }

        /// <summary>
        /// Adds the Calendar Events Mass Update updated changes.
        /// </summary>
        /// <param name="oldMassUpdate">The old mass update data.</param>
        /// <param name="newMassUpdate">The new mass update data.</param>
        public async Task AddCalendarEventMassUpdateUpdatedChangesAsync(CalendarEventMassUpdate oldMassUpdate, CalendarEventMassUpdate newMassUpdate)
        {
            // Step 1: Add Mass Update history
            var changes = new List<AttributeChangeModel>
            {
                new AttributeChangeModel
                {
                    AttributeName = "massUpdate.title",
                    OldValue = oldMassUpdate.Title,
                    NewValue = newMassUpdate.Title
                },
                new AttributeChangeModel
                {
                    AttributeName = "massUpdate.description",
                    OldValue = oldMassUpdate.Description,
                    NewValue = newMassUpdate.Description
                }
            };
            await AddNonLocationObjectChangesAsync(CalendarEventMassUpdateObjectType, oldMassUpdate.MassUpdateId, changes);
        }

        #endregion


        /// <summary>
        /// Searches change history matching given criteria.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        /// <returns>
        /// Change history matching given criteria.
        /// </returns>
        public async Task<SearchResult<ObjectChangeModel>> SearchChangeHistoryAsync(ChangeHistorySearchCriteria criteria)
        {
            if (!string.IsNullOrEmpty(criteria.CampusId) && criteria.Hierarchy == true)
            {
                // get child Region Ids
                criteria.RegionNodeIds = await GetRegionIdsAsync(criteria.CampusId);

                // get Child Location Ids
                criteria.ChildLocationIds = await GetChildLocationIdsAsync(criteria.RegionNodeIds);
            }

            if (!string.IsNullOrEmpty(criteria.RegionNodeId))
            {
                criteria.RegionNodeIds = new List<string> { criteria.RegionNodeId };

                if (criteria.Hierarchy == true)
                {
                    // get Child Location Ids
                    criteria.ChildLocationIds = await GetChildLocationIdsAsync(criteria.RegionNodeIds);
                }
            }

            if (!string.IsNullOrEmpty(criteria.RegionId))
            {
                // get child Region Ids
                criteria.RegionNodeIds = await GetRegionIdsAsync(criteria.RegionId, criteria.State, criteria.CityName);

                if (criteria.Hierarchy == true)
                {
                    // get Child Location Ids
                    criteria.ChildLocationIds = await GetChildLocationIdsAsync(criteria.RegionNodeIds);
                }
            }

            if (!string.IsNullOrEmpty(criteria.ChildLocationId))
            {
                criteria.ChildLocationIds = new List<string> { criteria.ChildLocationId };
            }

            var result = await SearchObjectChangesAsync(criteria);

            return result;
        }

        /// <summary>
        /// Searches the change history summary matching given criteria.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        /// <returns>
        /// Change summary for the given criteria.
        /// </returns>
        public async Task<SearchResult<LocationChangeSummary>> SearchChangeSummariesAsync(ChangeSummarySearchCriteria criteria)
        {
            if (!string.IsNullOrEmpty(criteria.CampusId) && criteria.Hierarchy == true)
            {
                // get child Region Ids
                criteria.RegionNodeIds = await GetRegionIdsAsync(criteria.CampusId);

                // get Child Location Ids
                criteria.ChildLocationIds = await GetChildLocationIdsAsync(criteria.RegionNodeIds);
            }

            if (!string.IsNullOrEmpty(criteria.RegionId))
            {
                criteria.RegionNodeIds = new List<string> { criteria.RegionId };

                if (criteria.Hierarchy == true)
                {
                    // get Child Location Ids
                    criteria.ChildLocationIds = await GetChildLocationIdsAsync(criteria.RegionNodeIds);
                }
            }

            if (!string.IsNullOrEmpty(criteria.ChildLocationId))
            {
                criteria.ChildLocationIds = new List<string> { criteria.ChildLocationId };
            }

            var result = await SearchObjectChangeSummariesAsync(criteria);

            return result;
        }


        #region helper methods

        /// <summary>
        /// Determines whether given time values are same.
        /// </summary>
        /// <param name="time1">The first time.</param>
        /// <param name="time2">The second time.</param>
        /// <returns>
        ///   <c>true</c> if given time values are same; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsTimeSame(string time1, string time2)
        {
            if (string.IsNullOrEmpty(time1) && string.IsNullOrEmpty(time2))
            {
                return true;
            }

            if (string.IsNullOrEmpty(time1) || string.IsNullOrEmpty(time2))
            {
                return false;
            }

            if (time1 == time2)
            {
                return true;
            }

            return time1.TrimStart('0') == time2.TrimStart('0');
        }

        /// <summary>
        /// Gets the change history between two objects.
        /// </summary>
        /// <param name="newObj">The new object.</param>
        /// <param name="oldObj">The old object.</param>
        /// <param name="path">The property path within the object.</param>
        /// <returns>The change history of given objects.</returns>
        private IEnumerable<AttributeChangeModel> GetAttributeChanges(JObject newObj, JToken oldObj, string path = "")
        {
            var newProps = newObj.Properties();
            foreach (var newProp in newProps)
            {
                string propName = newProp.Name;
                if (ChangedPropsToIgnore.Contains(propName))
                {
                    // ignore internal properties
                    continue;
                }

                string propPath = path.Length > 0 ? $"{path}.{propName}" : propName;
                var oldPropValue = oldObj?[propName];

                var newValue = newProp.Value;
                if (newValue?.Type == JTokenType.Array)
                {
                    if (propName == "financialData")
                    {
                        var changes = GetFinancialDataChanges(oldPropValue, newValue);
                        foreach (var change in changes)
                        {
                            yield return change;
                        }
                    }
                    else if (propName == "visibletoWebsitesFlag")
                    {
                        var oldVisibletoWebsitesFlag = oldPropValue as JArray;
                        var newVisibletoWebsitesFlag = newValue as JArray;
                        if (oldVisibletoWebsitesFlag.Count == 0 ||
                            newVisibletoWebsitesFlag.Count == 0 ||
                            oldVisibletoWebsitesFlag[0]["visible"].ToString() != newVisibletoWebsitesFlag[0]["visible"].ToString())
                            yield return new AttributeChangeModel
                            {
                                AttributeName = propPath,
                                NewValue = newVisibletoWebsitesFlag.Count == 0 ? null : newVisibletoWebsitesFlag[0]["visible"].ToString(),
                                OldValue = oldVisibletoWebsitesFlag.Count == 0 ? null : oldVisibletoWebsitesFlag[0]["visible"].ToString()
                            };
                    }
                    else if (
                        propName == "locationPhoneNumber" ||
                        propPath == "branchAdditionalContent.galleryImageNames" ||
                        propPath == "branchAdditionalContent.businessGroupImageNames")
                    {
                        var newArray = newValue as JArray;
                        var oldArray = oldPropValue as JArray;

                        // convert to array
                        string[] newValues = newArray?.ToObject<string[]>() ?? Array.Empty<string>();
                        string[] oldValues = oldArray?.ToObject<string[]>() ?? Array.Empty<string>();

                        // remove same items in both arrays
                        var addedValues = newValues.Except(oldValues).ToList();
                        var removedValues = oldValues.Except(newValues).ToList();

                        // check for changes
                        for (var i = 0; i < Math.Max(addedValues.Count, removedValues.Count); i++)
                        {
                            var tempNewValue = string.Empty;
                            var tempOldValue = string.Empty;
                            if (i < Math.Min(addedValues.Count, removedValues.Count))
                            {
                                // handle updated
                                tempNewValue = addedValues[i];
                                tempOldValue = removedValues[i];
                            }
                            else if (removedValues.Count > addedValues.Count)
                            {
                                // handle removed values
                                tempOldValue = removedValues[i];
                            }
                            else if (removedValues.Count < addedValues.Count)
                            {
                                // handle removed values
                                tempNewValue = addedValues[i];
                            }
                            var change = new AttributeChangeModel
                            {
                                AttributeName = propPath,
                                OldValue = tempOldValue,
                                NewValue = tempNewValue
                            };
                            yield return change;
                        }
                    }
                    else if (SimpleStringArrayProps.Contains(propName))
                    {
                        var oldArray = oldPropValue as JArray;
                        var newArray = newValue as JArray;
                        string[] oldValues = oldArray?.ToObject<string[]>() ?? Array.Empty<string>();
                        string[] newValues = newArray?.ToObject<string[]>() ?? Array.Empty<string>();
                        var removedValues = oldValues.Except(newValues);
                        var addedValues = newValues.Except(oldValues);

                        // handle removed values
                        foreach (var value in removedValues)
                        {
                            yield return new AttributeChangeModel
                            {
                                AttributeName = propPath,
                                OldValue = value,
                                NewValue = string.Empty
                            };
                        }

                        // handle added values
                        foreach (var value in addedValues)
                        {
                            yield return new AttributeChangeModel
                            {
                                AttributeName = propPath,
                                OldValue = string.Empty,
                                NewValue = value
                            };
                        }
                    }

                    // ignore other arrays, they are handled separately
                    continue;
                }

                // for Objects - go one level deeper
                if (newValue?.Type == JTokenType.Object)
                {
                    var newPropValueObj = newValue as JObject;
                    var changeProps = GetAttributeChanges(newPropValueObj, oldPropValue as JObject, propPath);
                    foreach (var changeProp in changeProps)
                    {
                        yield return changeProp;
                    }

                    continue;
                }

                // for primitive types - compare string representation
                string newValueStr = newValue?.ToString();
                string oldValueStr = oldPropValue?.ToString();

                // if both values are null/empty, ignore change
                if (string.IsNullOrWhiteSpace(newValueStr) && string.IsNullOrWhiteSpace(oldValueStr))
                {
                    continue;
                }

                // ignore boolean changes from Null/Empty to 'N'
                if (BooleanFields.Contains(propName) && string.IsNullOrWhiteSpace(oldValueStr) && newValueStr == "N")
                {
                    continue;
                }

                if (newValueStr != oldValueStr)
                {
                    if (FinancialDataDates.Contains(propName) && !string.IsNullOrWhiteSpace(newValue.ToString()))
                    {
                        newValueStr = newValue.Value<DateTime>().ToString("MM/dd/yyyy");
                    }
                    if (FinancialDataDates.Contains(propName) && !string.IsNullOrWhiteSpace(oldPropValue.ToString()))
                    {
                        oldValueStr = oldPropValue.Value<DateTime>().ToString("MM/dd/yyyy");
                    }
                    yield return new AttributeChangeModel
                    {
                        AttributeName = propPath,
                        NewValue = newValueStr,
                        OldValue = oldValueStr
                    };
                }
            }
        }

        /// <summary>
        /// Gets the change history attributes for Created object.
        /// </summary>
        /// <param name="createdObj">The created object.</param>
        /// <param name="attributeNamePrefix">The attribute name prefix.</param>
        /// <returns>
        /// The change history of given objects.
        /// </returns>
        private static IEnumerable<AttributeChangeModel> GetCreatedItemAttributeChanges(JObject createdObj, string attributeNamePrefix = "")
        {
            if (!string.IsNullOrEmpty(attributeNamePrefix))
            {
                attributeNamePrefix += ".";
            }

            var props = createdObj.Properties();
            foreach (var prop in props)
            {
                string propName = attributeNamePrefix + prop.Name;
                if (ChangedPropsToIgnore.Contains(propName))
                {
                    // ignore internal properties
                    continue;
                }

                // for primitive types - compare string representation
                string valueStr = prop.Value?.ToString();

                // if value is null/empty, ignore change
                if (string.IsNullOrWhiteSpace(valueStr))
                {
                    continue;
                }

                yield return new AttributeChangeModel
                {
                    AttributeName = propName,
                    NewValue = valueStr,
                    OldValue = null
                };
            }
        }

        /// <summary>
        /// Gets the change history attributes for Deleted object.
        /// </summary>
        /// <param name="deletedObj">The deleted object.</param>
        /// <param name="attributeNamePrefix">The attribute name prefix.</param>
        /// <returns>
        /// The change history of given object.
        /// </returns>
        private static IEnumerable<AttributeChangeModel> GetDeletedItemAttributeChanges(JObject deletedObj, string attributeNamePrefix = "")
        {
            if (!string.IsNullOrEmpty(attributeNamePrefix))
            {
                attributeNamePrefix += ".";
            }

            var props = deletedObj.Properties();
            foreach (var prop in props)
            {
                string propName = attributeNamePrefix + prop.Name;
                if (ChangedPropsToIgnore.Contains(propName))
                {
                    // ignore internal properties
                    continue;
                }

                // for primitive types - compare string representation
                string valueStr = prop.Value?.ToString();

                // if value is null/empty, ignore change
                if (string.IsNullOrWhiteSpace(valueStr))
                {
                    continue;
                }

                yield return new AttributeChangeModel
                {
                    AttributeName = propName,
                    NewValue = null,
                    OldValue = valueStr
                };
            }
        }

        /// <summary>
        /// Gets the financial data changes.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        /// <returns>The financial data changes.</returns>
        private IList<AttributeChangeModel> GetFinancialDataChanges(JToken oldValue, JToken newValue)
        {
            var camelCaseSerializer = JsonSerializer.Create(Util.SerializerSettings);

            var result = new List<AttributeChangeModel>();

            // convert to list
            var newItems = newValue.ToObject<IList<FinancialDataItem>>();
            var oldItems = oldValue?.ToObject<IList<FinancialDataItem>>() ?? new List<FinancialDataItem>();

            // remove same items in both lists
            var addedItems = newItems.Where(n => !oldItems.Any(o => n.CostCenterId == o.CostCenterId)).ToList();
            var removedItems = oldItems.Where(o => !newItems.Any(n => n.CostCenterId == o.CostCenterId)).ToList();

            for (var i = 0; i < Math.Max(addedItems.Count, removedItems.Count); i++)
            {
                if (i < Math.Min(addedItems.Count, removedItems.Count))
                {
                    // handle updated values
                    var changes = GetAttributeChanges(JObject.FromObject(addedItems[i], camelCaseSerializer),
                        JObject.FromObject(removedItems[i], camelCaseSerializer), path: "financialData").ToList();
                    result.AddRange(changes);
                }
                else if (removedItems.Count > addedItems.Count)
                {
                    // handle removed values
                    var changes = GetAttributeChanges(JObject.FromObject(new FinancialDataItem(), camelCaseSerializer),
                        JObject.FromObject(removedItems[i], camelCaseSerializer), path: "financialData").ToList();
                    result.AddRange(changes);
                }
                else if (removedItems.Count < addedItems.Count)
                {
                    // handle added values
                    var changes = GetAttributeChanges(JObject.FromObject(addedItems[i], camelCaseSerializer),
                        JObject.FromObject(new FinancialDataItem(), camelCaseSerializer), path: "financialData").ToList();
                    result.AddRange(changes);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the change summary of the given object.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="objectId">The Object Id.</param>
        /// <returns>
        /// The given object's change summary.
        /// </returns>
        private async Task<JObject> GetObjectChangeSummaryAsync(string objectType, string objectId)
        {
            // TODO: 'objectId' is not present in Summary. locationId must be used instead
            string sql = $"SELECT * FROM c WHERE c.partition_key='{CosmosConfig.ChangeSummaryPartitionKey}'" +
                $" AND c.objectType='{objectType}' AND c.objectId='{objectId}'";

            var summaryObj = await GetFirstOrDefaultAsync<JObject>(CosmosConfig.LocationsContainerName, sql);
            return summaryObj;
        }

        /// <summary>
        /// Adds the change history for the array items changes.
        /// </summary>
        /// <typeparam name="T">Type of the models</typeparam>
        /// <param name="attributePrefix">The attribute prefix.</param>
        /// <param name="childLocObj">The child location object.</param>
        /// <param name="oldItems">The old items.</param>
        /// <param name="newItems">The new items.</param>
        private async Task AddArrayItemChanges<T>(string attributePrefix, JObject childLocObj, IList<T> oldItems, IList<T> newItems)
            where T : UniqueModel
        {
            var attributesToSkip = new string[]
            {
                $"{attributePrefix}.itemGuid",
            };

            var deletedItems = oldItems.Where(x => !newItems.Any(y => y.ItemGuid == x.ItemGuid)).ToList();
            var createdItems = newItems.Where(x => !oldItems.Any(y => y.ItemGuid == x.ItemGuid)).ToList();
            var updatedItems = newItems.Where(x => oldItems.Any(y => y.ItemGuid == x.ItemGuid)).ToList();

            var changes = new List<AttributeChangeModel>();

            // handle deleted items
            foreach (var item in deletedItems)
            {
                var obj = Util.ToJObject(item);
                var itemChanges = GetDeletedItemAttributeChanges(obj, attributePrefix);
                changes.AddRange(itemChanges);
            }

            // handle new items
            foreach (var item in createdItems)
            {
                var obj = Util.ToJObject(item);
                var itemChanges = GetCreatedItemAttributeChanges(obj, attributePrefix);
                changes.AddRange(itemChanges);
            }

            // handle updated items
            foreach (var item in updatedItems)
            {
                var oldItem = oldItems.First(x => x.ItemGuid == item.ItemGuid);
                var oldObj = Util.ToJObject(oldItem);
                var newObj = Util.ToJObject(item);
                var itemChanges = GetAttributeChanges(newObj, oldObj, attributePrefix);
                changes.AddRange(itemChanges);
            }

            // remove skipped attributes
            changes = changes.Where(x => !attributesToSkip.Contains(x.AttributeName)).ToList();

            string node = childLocObj.Value<string>("node");
            await AddObjectChangesAndUpdateSummary(ChildLocationObjectType, node, childLocObj, changes);
        }

        /// <summary>
        /// Adds the object changes and updates change summary.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="objectId">The object Id.</param>
        /// <param name="model">The objetc model.</param>
        /// <param name="changes">The attribute changes.</param>
        private async Task AddObjectChangesAndUpdateSummary(string objectType, string objectId, JObject model, IList<AttributeChangeModel> changes)
        {
            if (changes.Count == 0)
            {
                // no need to save change history if nothing was changed
                return;
            }

            // add changes to history
            await AddObjectChangesAsync(objectType, objectId, model, changes);

            // update change summary
            await UpdateObjectChangeSummaryAsync(objectType, objectId, model);
        }

        /// <summary>
        /// Adds the object changes.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="objectId">The object Id.</param>
        /// <param name="model">The objetc model.</param>
        /// <param name="changes">The attributes changes.</param>
        private async Task AddObjectChangesAsync(string objectType, string objectId, JObject model, IList<AttributeChangeModel> changes)
        {
            string locationName = model.Value<string>("locationName");

            var address = model["address"];
            string state = address?.Value<string>("state");
            string cityName = address?.Value<string>("cityName");

            // add new history record
            var now = DateTime.UtcNow;
            var items = new List<ObjectChangeModel>();
            foreach (AttributeChangeModel change in changes)
            {
                if (change.OldValue == change.NewValue)
                {
                    // ignore unchanged values
                    continue;
                }

                var objectChange = new ObjectChangeModel
                {
                    LocationName = locationName,
                    ObjectId = objectId,
                    ObjectType = objectType,
                    State = state,
                    CityName = cityName,
                    ChangedOn = now,
                    EditedBy = _appContextProvider.GetCurrentUserFullName(),
                    ChangedAttribute = change
                };
                items.Add(objectChange);
            }

            if (items.Count > 0)
            {
                await CreateItemFromListAsync<ObjectChangeModel>(CosmosConfig.LocationsContainerName, CosmosConfig.ChangeHistoryPartitionKey, items);
            }
        }

        /// <summary>
        /// Adds the history changes for non-Location object.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="objectId">The object Id.</param>
        /// <param name="changes">The attributes changes.</param>
        private async Task AddNonLocationObjectChangesAsync(string objectType, string objectId, IList<AttributeChangeModel> changes)
        {
            // add new history record
            var now = DateTime.UtcNow;
            var items = new List<ObjectChangeModel>();
            foreach (AttributeChangeModel change in changes)
            {
                if (change.OldValue == change.NewValue)
                {
                    // ignore unchanged values
                    continue;
                }

                var objectChange = new ObjectChangeModel
                {
                    ObjectId = objectId,
                    ObjectType = objectType,
                    ChangedOn = now,
                    EditedBy = _appContextProvider.GetCurrentUserFullName(),
                    ChangedAttribute = change
                };
                items.Add(objectChange);
            }

            await CreateItemFromListAsync<ObjectChangeModel>(CosmosConfig.LocationsContainerName, CosmosConfig.ChangeHistoryPartitionKey, items);
        }

        /// <summary>
        /// Updates the object changes summary.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="objectId">The object Id.</param>
        /// <param name="model">The object model.</param>
        private async Task UpdateObjectChangeSummaryAsync(string objectType, string objectId, JObject model)
        {
            // try get existing summary
            JObject summaryObj = await GetObjectChangeSummaryAsync(objectType, objectId);
            if (summaryObj == null)
            {
                // create change summary
                var summary = new LocationChangeSummary
                {
                    LocationId = objectId
                };
                summaryObj = PrepareNewObjectProperties(summary, CosmosConfig.ChangeSummaryPartitionKey, setRecFields: false);
            }

            string locationName = model.Value<string>("locationName");

            // set object info updated fields
            summaryObj["locationName"] = locationName;
            summaryObj["locationNodeType"] = ToNodeType(objectType).ToString();
            summaryObj["address"] = model["address"];
            summaryObj["region"] = model.Value<string>("regionName");
            summaryObj["editedBy"] = _appContextProvider.GetCurrentUserFullName();

            // set last change details
            summaryObj["lastChangedDate"] = DateTime.UtcNow;

            // create or update summary
            await UpsertItemAsync(CosmosConfig.LocationsContainerName, CosmosConfig.ChangeSummaryPartitionKey, summaryObj);
        }

        /// <summary>
        /// Searches object's change history matching given criteria.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        /// <returns>
        /// Campus change history.
        /// </returns>
        private async Task<SearchResult<ObjectChangeModel>> SearchObjectChangesAsync(ChangeHistorySearchCriteria criteria)
        {
            // prepare where conditions for Ids
            var conditions = new List<string>();
            if (!string.IsNullOrEmpty(criteria.CampusId))
            {
                conditions.Add($"(c.objectType='{CampusObjectType}' AND c.objectId='{criteria.CampusId}')");
            }
            if (criteria.RegionNodeIds?.Count > 0)
            {
                var regionIdsCsv = string.Join("','", criteria.RegionNodeIds);
                conditions.Add($"(c.objectType='{RegionObjectType}' AND c.objectId in('{regionIdsCsv}'))");
            }
            if (criteria.ChildLocationIds?.Count > 0)
            {
                var childLocationIdsCsv = string.Join("','", criteria.ChildLocationIds);
                conditions.Add($"(c.objectType='{ChildLocationObjectType}' AND c.objectId in('{childLocationIdsCsv}'))");
            }
            string idFilter = conditions.Count > 0
                ? $" AND ({string.Join(" OR ", conditions)})"
                : string.Empty;

            // construct where statement
            var whereQuery = new StringBuilder($"WHERE c.partition_key='{CosmosConfig.ChangeHistoryPartitionKey}'")
                .AppendEqualsCondition("objectType", criteria.ObjectType)
                .AppendEqualsCondition("state", criteria.State)
                .AppendEqualsCondition("cityName", criteria.CityName)
                .Append(idFilter)
                .AppendGreaterThanOrEqualToCondition("changedOn", criteria.StartDate?.Date)
                .AppendLessThanOrEqualToCondition("changedOn", criteria.EndDate.EOD());

            // condition for attribute names
            if (criteria.AttributeNames?.Count > 0)
            {
                var attributeNamesCsv = string.Join("','", criteria.AttributeNames);
                whereQuery.Append($" AND c.changedAttribute.attributeName IN('{attributeNamesCsv}')");
            }

            int offset = (criteria.PageNum - 1) * criteria.PageSize;
            string sql = $"SELECT * FROM c" +
                $" {whereQuery}" +
                $" ORDER BY c.{criteria.SortBy} {criteria.SortOrder}" +
                $" OFFSET {offset} LIMIT {criteria.PageSize}";

            var summaries = await GetAllItemsAsync<ObjectChangeModel>(CosmosConfig.LocationsContainerName, sql);

            var result = new SearchResult<ObjectChangeModel>(summaries);

            // get total Count
            string totalCountSql = $"select value count(1) from c {whereQuery}";
            result.TotalCount = await GetValueAsync<int>(CosmosConfig.LocationsContainerName, totalCountSql);

            return result;
        }

        /// <summary>
        /// Searches the change history summary matching given criteria.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        /// <returns>
        /// Change summary for the given criteria.
        /// </returns>
        private async Task<SearchResult<LocationChangeSummary>> SearchObjectChangeSummariesAsync(ChangeSummarySearchCriteria criteria)
        {
            // prepare where conditions for Ids
            var conditions = new List<string>();
            if (!string.IsNullOrEmpty(criteria.CampusId))
            {
                conditions.Add($"(c.objectType='{CampusObjectType}' AND c.objectId='{criteria.CampusId}')");
            }
            if (criteria.RegionNodeIds?.Count > 0)
            {
                var regionIdsCsv = string.Join("','", criteria.RegionNodeIds);
                conditions.Add($"(c.objectType='{RegionObjectType}' AND c.objectId in('{regionIdsCsv}'))");
            }
            if (criteria.ChildLocationIds?.Count > 0)
            {
                var childLocationIdsCsv = string.Join("','", criteria.ChildLocationIds);
                conditions.Add($"(c.objectType='{HierarchyNodeType.childLoc}' AND c.objectId in('{childLocationIdsCsv}'))");
            }
            string idFilter = conditions.Count > 0
                ? $" AND ({string.Join(" OR ", conditions)})"
                : string.Empty;

            // construct where statement
            var whereQuery = new StringBuilder($"WHERE c.partition_key='{CosmosConfig.ChangeSummaryPartitionKey}'")
                .AppendEqualsCondition("objectType", criteria.ObjectType)
                .Append(idFilter)
                .AppendGreaterThanOrEqualToCondition("lastChangedDate", criteria.StartDate?.Date)
                .AppendLessThanOrEqualToCondition("lastChangedDate", criteria.EndDate.EOD());

            int offset = (criteria.PageNum - 1) * criteria.PageSize;
            string sql = $"SELECT * FROM c" +
                $" {whereQuery}" +
                $" ORDER BY c.{criteria.SortBy} {criteria.SortOrder}" +
                $" OFFSET {offset} LIMIT {criteria.PageSize}";

            var summaries = await GetAllItemsAsync<LocationChangeSummary>(CosmosConfig.LocationsContainerName, sql);
            var result = new SearchResult<LocationChangeSummary>(summaries);

            // get total Count
            string totalCountSql = $"select value count(1) from c {whereQuery}";
            result.TotalCount = await GetValueAsync<int>(CosmosConfig.LocationsContainerName, totalCountSql);

            return result;
        }

        /// <summary>
        /// Gets the associates changes.
        /// </summary>
        /// <param name="oldAssociates">The old associates.</param>
        /// <param name="newAssociates">The new associates.</param>
        /// <returns>The associates changes.</returns>
        private IList<AttributeChangeModel> GetAssociatesChanges(IList<AssociateModel> oldAssociates, IList<AssociateModel> newAssociates)
        {
            var changes = new List<AttributeChangeModel>();

            // get removed/added associates
            var removedAssociates = oldAssociates.Except(newAssociates);
            var addedAssociates = newAssociates.Except(oldAssociates);

            var changedAssociates = removedAssociates.Concat(addedAssociates).ToList();
            if (changedAssociates.Count == 0)
            {
                // no changes
                return changes;
            }

            foreach (var associate in changedAssociates)
            {
                var change = new AttributeChangeModel
                {
                    AttributeName = "contacts"
                };

                var associateDescription = $"{associate.FirstName} {associate.LastName} - {associate.Title}";
                if (removedAssociates.Contains(associate))
                {
                    change.OldValue = associateDescription;
                    change.NewValue = null;
                }
                else
                {
                    change.OldValue = null;
                    change.NewValue = associateDescription;
                }
                changes.Add(change);
            }

            return changes;
        }

        /// <summary>
        /// Gets the child region Ids.
        /// </summary>
        /// <param name="regionId">The region identifier.</param>
        /// <param name="state">The region state.</param>
        /// <param name="cityName">The region city name.</param>
        /// <returns>
        /// The child region Ids.
        /// </returns>
        private async Task<IList<string>> GetRegionIdsAsync(string regionId, string state, string cityName)
        {
            // load Ids of Campus Regions
            string sql = $"SELECT VALUE c.node FROM c WHERE c.partition_key='{CosmosConfig.RegionPartitionKey}' AND c.regionID='{regionId}' {ActiveRecordFilter}";
            if (state != null)
            {
                sql += $" AND c.address.state = '{state}'";
            }
            if (cityName != null)
            {
                sql += $" AND c.address.cityName = '{cityName}'";
            }
            var regionIds = await GetAllItemsAsync<string>(CosmosConfig.LocationsContainerName, sql);
            return regionIds;
        }

        /// <summary>
        /// Converts to nodetype.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns></returns>
        private static HierarchyNodeType ToNodeType(string objectType)
        {
            return objectType switch
            {
                CampusObjectType => HierarchyNodeType.campus,
                RegionObjectType => HierarchyNodeType.region,
                ChildLocationObjectType => HierarchyNodeType.childLoc,
                _ => HierarchyNodeType.none,
            };
        }

        /// <summary>
        /// Converts location node type to object type which is used for change history.
        /// </summary>
        /// <param name="nodeType">Type of the location node.</param>
        /// <returns></returns>
        private static string ToObjectType(NodeType nodeType)
        {
            return nodeType switch
            {
                NodeType.Campus => CampusObjectType,
                NodeType.Region => RegionObjectType,
                NodeType.ChildLoc => ChildLocationObjectType,
                _ => throw new NotSupportedException("")
            };
        }

        #endregion
    }
}
