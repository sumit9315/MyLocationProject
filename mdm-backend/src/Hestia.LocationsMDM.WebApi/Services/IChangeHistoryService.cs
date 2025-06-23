using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hestia.LocationsMDM.WebApi.Models;
using Hestia.LocationsMDM.WebApi.Models.CalendarEvent;
using Newtonsoft.Json.Linq;

namespace Hestia.LocationsMDM.WebApi.Services
{
    /// <summary>
    /// The Change History service interface.
    /// </summary>
    public interface IChangeHistoryService
    {
        /// <summary>
        /// Adds the pricing region change for given Locations.
        /// </summary>
        /// <param name="changedLocationObjects">The changed location objects.</param>
        Task AddLocationsPricingRegionChangeAsync(IList<ChangedObject> changedLocationObjects);

        /// <summary>
        /// Changes the object Id ('node' for locations) in all change history/summary records.
        /// </summary>
        /// <param name="oldObjectId">The old object Id.</param>
        /// <param name="newObjectId">The new object Id.</param>
        Task ChangeObjectIdAsync(string oldObjectId, string newObjectId);

        /// <summary>
        /// Adds the Campus change history/summary.
        /// </summary>
        /// <param name="oldObj">The old object.</param>
        /// <param name="newObj">The new object.</param>
        Task AddCampusChangesAsync(JObject oldObj, JObject newObj);

        /// <summary>
        /// Adds the Associates history for Campus.
        /// </summary>
        /// <param name="campus">The Campus.</param>
        /// <param name="oldAssociates">The old associates.</param>
        /// <param name="newAssociates">The new associates.</param>
        Task AddCampusAssociatesChangesAsync(JObject campus, IList<AssociateModel> oldAssociates, IList<AssociateModel> newAssociates);


        /// <summary>
        /// Adds the Region change history/summary.
        /// </summary>
        /// <param name="oldObj">The old object.</param>
        /// <param name="newObj">The new object.</param>
        Task AddRegionChangesAsync(JObject oldObj, JObject newObj);

        /// <summary>
        /// Adds the Associates history for Region.
        /// </summary>
        /// <param name="region">The Region.</param>
        /// <param name="oldAssociates">The old associates.</param>
        /// <param name="newAssociates">The new associates.</param>
        Task AddRegionAssociatesChangesAsync(JObject region, IList<AssociateModel> oldAssociates, IList<AssociateModel> newAssociates);

        /// <summary>
        /// Adds the child location changes.
        /// </summary>
        /// <param name="oldObj">The old object.</param>
        /// <param name="newObj">The new object.</param>
        Task AddChildLocationChangesAsync(JObject oldObj, JObject newObj);

        /// <summary>
        /// Adds the child location created changes.
        /// </summary>
        /// <param name="childLocNode">The child location identifier.</param>
        /// <param name="childLoc">The child location model.</param>
        /// <returns>The task.</returns>
        Task AddChildLocationCreatedChangesAsync(string childLocNode, JObject childLoc);

        /// <summary>
        /// Adds the child location deleted changes.
        /// </summary>
        /// <param name="childLocNode">The child location identifier.</param>
        /// <param name="childLoc">The child location model.</param>
        /// <returns>The task.</returns>
        Task AddChildLocationDeletedChangesAsync(string childLocNode, JObject childLoc);

        /// <summary>
        /// Adds the Associates history for Child Location.
        /// </summary>
        /// <param name="childLocation">The child location.</param>
        /// <param name="oldAssociates">The old associates.</param>
        /// <param name="newAssociates">The new associates.</param>
        Task AddChildLocationAssociatesChangesAsync(JObject childLocation, IList<AssociateModel> oldAssociates, IList<AssociateModel> newAssociates);

        /// <summary>
        /// Adds the Events history for Location.
        /// </summary>
        /// <param name="nodeType">Type of the node.</param>
        /// <param name="location">The location object.</param>
        /// <param name="addedEvents">The added events.</param>
        /// <param name="removedEvents">The removed events.</param>
        /// <param name="changedEvents">The changed events.</param>
        /// <returns></returns>
        Task AddLocationEventsChangesAsync(NodeType nodeType, JObject location, IList<JObject> addedEvents = null, IList<JObject> removedEvents = null, IList<ChangedObject> changedEvents = null);

        Task<dynamic> AddHistoryForChildLocBulkEventAsync(IList<JObject> childLocs, JObject addedEvent);

        /// <summary>
        /// Adds the Operating Hours history for Child Location.
        /// </summary>
        /// <param name="childLocation">The child location.</param>
        /// <param name="newOperatingHours">The new operating hours.</param>
        Task AddChildLocationOperatingHoursChangesAsync(JObject childLocation, IList<OperatingHoursModel> newOperatingHours);

        /// <summary>
        /// Adds the Professional Associates history for Child Location.
        /// </summary>
        /// <param name="childLocObj">The child location.</param>
        /// <param name="oldItems">The old Professional Associates.</param>
        /// <param name="newItems">The new Professional Associates.</param>
        Task AddChildLocationProfessionalAssociatesChangesAsync(JObject childLocObj, IList<ProfessionalAssociationModel> oldItems, IList<ProfessionalAssociationModel> newItems);

        /// <summary>
        /// Adds the Merchandising Banners history for Child Location.
        /// </summary>
        /// <param name="childLocObj">The child location.</param>
        /// <param name="oldItems">The old Merchandising Banners.</param>
        /// <param name="newItems">The new Merchandising Banners.</param>
        Task AddChildLocationMerchandisingBannersChangesAsync(JObject childLocObj, IList<MerchandisingBannerModel> oldItems, IList<MerchandisingBannerModel> newItems);

        /// <summary>
        /// Adds the Merchandising Banners history for Child Location.
        /// </summary>
        /// <param name="childLocObj">The child location.</param>
        /// <param name="removedEventIds">The old Merchandising Banners.</param>
        /// <param name="addedEventIds">The new Merchandising Banners.</param>
        Task AddChildLocationCalendarEventMassUpdateChangesAsync(JObject childLocObj, IList<string> removedEventIds, IList<string> addedEventIds);

        /// <summary>
        /// Adds the pricing region created changes.
        /// </summary>
        /// <param name="model">The pricing region mapping.</param>
        Task AddPricingRegionCreatedChangesAsync(PricingRegionMappingModel model);

        /// <summary>
        /// Adds the pricing region deleted changes.
        /// </summary>
        /// <param name="model">The pricing region mapping.</param>
        Task AddPricingRegionDeletedChangesAsync(PricingRegionMappingModel model);

        /// <summary>
        /// Adds the pricing region updated changes.
        /// </summary>
        /// <param name="oldModel">The old pricing region mapping.</param>
        /// <param name="newModel">The new pricing region mapping.</param>
        Task AddPricingRegionUpdatedChangesAsync(PricingRegionMappingModel oldModel, PricingRegionMappingModel newModel);

        #region Calendar Event Mass Update

        /// <summary>
        /// Adds the Calendar Events Mass Update created changes.
        /// </summary>
        /// <param name="massUpdate">The mass update data.</param>
        /// <param name="childLocs">The affcted child locations.</param>
        Task AddCalendarEventMassUpdateCreatedChangesAsync(CalendarEventMassUpdate massUpdate, IList<JObject> childLocs);

        /// <summary>
        /// Adds the Calendar Events Mass Update deleted changes.
        /// </summary>
        /// <param name="massUpdate">The deleted mass update data.</param>
        /// <param name="childLocs">The affcted child locations.</param>
        Task AddCalendarEventMassUpdateDeletedChangesAsync(CalendarEventMassUpdate massUpdate, IList<JObject> childLocs);

        /// <summary>
        /// Adds the Calendar Events Mass Update updated changes.
        /// </summary>
        /// <param name="oldMassUpdate">The old mass update data.</param>
        /// <param name="newMassUpdate">The new mass update data.</param>
        Task AddCalendarEventMassUpdateUpdatedChangesAsync(CalendarEventMassUpdate oldMassUpdate, CalendarEventMassUpdate newMassUpdate);

        #endregion

        /// <summary>
        /// Searches change history matching given criteria.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        /// <returns>
        /// Change history matching given criteria.
        /// </returns>
        Task<SearchResult<ObjectChangeModel>> SearchChangeHistoryAsync(ChangeHistorySearchCriteria criteria);

        /// <summary>
        /// Searches the change history summary matching given criteria.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        /// <returns>
        /// Change summary for the given criteria.
        /// </returns>
        Task<SearchResult<LocationChangeSummary>> SearchChangeSummariesAsync(ChangeSummarySearchCriteria criteria);
    }
}
