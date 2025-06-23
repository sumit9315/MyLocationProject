using System.Runtime.Serialization;
using CosmosIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Child Location Business Info model.
    /// </summary>
    public class ChildLocBusinessInfoModel : BusinessInfoModel
    {
        /// <summary>
        /// Indicates whether the location is open for purchase.
        /// </summary>
        public bool? OpenForPurchase { get; set; }

        /// <summary>
        /// Indicates whether the location is open for shipping.
        /// </summary>
        public bool? OpenForShipping { get; set; }

        /// <summary>
        /// Indicates whether the location is open for receiving.
        /// </summary>
        public bool? OpenForReceiving { get; set; }

        /// <summary>
        /// Indicates whether the location is pro-pick-up.
        /// </summary>
        public bool? ProPickup { get; set; }

        /// <summary>
        /// Indicates whether the location supports self checkout.
        /// </summary>
        public bool? SelfCheckout { get; set; }

        /// <summary>
        /// Indicates whether the location is staff-pro-pick-up.
        /// </summary>
        public bool? StaffPropickup { get; set; }

        /// <summary>
        /// Indicates whether the location is visible to the website.
        /// </summary>
        public bool? VisibleToWebsite { get; set; }

        /// <summary>
        /// Gets or sets a BOPIS flag.
        /// </summary>
        public bool? Bopis { get; set; }

        /// <summary>
        /// Gets or sets the Text to Counter flag.
        /// </summary>
        public bool? TextToCounter { get; set; }

        /// <summary>
        /// Gets or sets the Buy Online flag.
        /// </summary>
        public bool? BuyOnline { get; set; }

        /// <summary>
        /// Gets or sets the Available to Storefront flag.
        /// </summary>
        public bool? AvailableToStorefront { get; set; }

        /// <summary>
        /// Gets or sets the text to counter phone number.
        /// </summary>
        public string TextToCounterPhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the Locker PU.
        /// </summary>
        public string LockerPU { get; set; }

        /// <summary>
        /// Gets or sets the Locker PU display name.
        /// </summary>
        [CosmosIgnore]
        public string LockerPUDisplayName { get; set; }

        /// <summary>
        /// Gets or sets the Cages value (whether there are cages and what type of cages).
        /// </summary>
        public string Cages { get; set; }

        /// <summary>
        /// Gets or sets the Cages display name.
        /// </summary>
        [CosmosIgnore]
        public string CagesDisplayName { get; set; }
    }
}
