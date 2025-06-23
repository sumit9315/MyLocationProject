using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Hestia.LocationsMDM.WebApi.Config
{
    /// <summary>
    /// The Cosmos DB configuration.
    /// </summary>
    public class CosmosConfig
    {
        /// <summary>
        /// Gets or sets the name of the database.
        /// </summary>
        /// <value>
        /// The name of the database.
        /// </value>
        public string DbName { get; set; }

        /// <summary>
        /// Gets or sets the name of the Application container.
        /// </summary>
        /// <value>
        /// The name of the Application container.
        /// </value>
        public string ApplicationContainerName { get; set; }

        /// <summary>
        /// Gets or sets the name of the locations container.
        /// </summary>
        /// <value>
        /// The name of the locations container.
        /// </value>
        public string LocationsContainerName { get; set; }

        /// <summary>
        /// Gets or sets the name of the secondary container.
        /// </summary>
        /// <value>
        /// The name of the secondary container.
        /// </value>
        public string SecondaryContainerName { get; set; }

        /// <summary>
        /// Gets or sets the Campus partition key.
        /// </summary>
        /// <value>
        /// The Campus partition key.
        /// </value>
        public string CampusPartitionKey { get; set; }

        /// <summary>
        /// Gets or sets the region partition key.
        /// </summary>
        /// <value>
        /// The region partition key.
        /// </value>
        public string RegionPartitionKey { get; set; }

        /// <summary>
        /// Gets or sets the child location partition key.
        /// </summary>
        /// <value>
        /// The child location partition key.
        /// </value>
        public string ChildLocationPartitionKey { get; set; }

        /// <summary>
        /// Gets or sets the associate partition key.
        /// </summary>
        /// <value>
        /// The associate partition key.
        /// </value>
        public string AssociatePartitionKey { get; set; }

        /// <summary>
        /// Gets or sets the campus role partition key.
        /// </summary>
        /// <value>
        /// The campus role partition key.
        /// </value>
        public string CampusRolePartitionKey { get; set; }

        /// <summary>
        /// Gets or sets the calendar event partition key.
        /// </summary>
        /// <value>
        /// The calendar event partition key.
        /// </value>
        public string CalendarEventPartitionKey { get; set; }

        /// <summary>
        /// Gets or sets the calendar event Mass Update partition key.
        /// </summary>
        /// <value>
        /// The calendar event Mass Update partition key.
        /// </value>
        public string CalendarEventMassUpdatePartitionKey { get; set; }

        /// <summary>
        /// Gets or sets the change history partition key.
        /// </summary>
        /// <value>
        /// The change history partition key.
        /// </value>
        public string ChangeHistoryPartitionKey { get; set; }

        /// <summary>
        /// Gets or sets the change summary partition key.
        /// </summary>
        /// <value>
        /// The change summary partition key.
        /// </value>
        public string ChangeSummaryPartitionKey { get; set; }

        /// <summary>
        /// Gets or sets the EDMCS Master partition key.
        /// </summary>
        /// <value>
        /// The EDMCS Master partition key.
        /// </value>
        public string EdmcsMasterPartitionKey { get; set; }

        /// <summary>
        /// Gets or sets the List of Values partition key.
        /// </summary>
        /// <value>
        /// The List of Values partition key.
        /// </value>
        public string LovPartitionKey { get; set; }

        /// <summary>
        /// The pricing region mapping partition key.
        /// </summary>
        public string PricingRegionMappingPartitionKey { get; set; }
    }
}
