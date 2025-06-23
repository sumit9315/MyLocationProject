using System;
using System.Runtime.Serialization;
using ApiIgnore = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Hestia.LocationsMDM.WebApi.Models
{
    public class FinancialDataItem
    {
        #region Primary Props

        /// <summary>
        /// The child location cost center id
        /// </summary>
        public string CostCenterId { get; set; }

        /// <summary>
        /// The lob cc.
        /// </summary>
        public string LobCc { get; set; }

        /// <summary>
        /// The lob cc name.
        /// </summary>
        public string LobCcName { get; set; }

        /// <summary>
        /// The lob cc effective date.
        /// </summary>
        public string LobCcEffDate { get; set; }

        /// <summary>
        /// The lob cc close date.
        /// </summary>
        public string LobCcCloseDate { get; set; }

        /// <summary>
        /// The lob cc status.
        /// </summary>
        public string LobCcStatus { get; set; }

        /// <summary>
        /// The CC Type.
        /// </summary>
        public string CcType { get; set; }

        /// <summary>
        /// The oracle company id.
        /// </summary>
        public string OracleCompanyID { get; set; }

        /// <summary>
        /// The oracle company name.
        /// </summary>
        public string OracleCompanyName { get; set; }

        /// <summary>
        /// The EIN Federal name.
        /// </summary>
        public string EinFederal { get; set; }

        /// <summary>
        /// The inventory org.
        /// </summary>
        public string InventoryOrg { get; set; }

        /// <summary>
        /// The sales system.
        /// </summary>
        public string SalesSystem { get; set; }

        /// <summary>
        /// The pricing region.
        /// </summary>
        public string PricingRegion { get; set; }

        #endregion


        #region Secondary Props (ignored in API)

        [ApiIgnore]
        public string EdmcsNode { get; set; }

        [ApiIgnore]
        public string AreaID { get; set; }

        [ApiIgnore]
        public string AreaName { get; set; }

        [ApiIgnore]
        public string Bmi { get; set; }

        [ApiIgnore]
        public string CcoEmplId { get; set; }

        [ApiIgnore]
        public string CostOrg { get; set; }

        [ApiIgnore]
        public string Counter { get; set; }

        [ApiIgnore]
        public string CpbuID { get; set; }

        [ApiIgnore]
        public string CpbuDescription { get; set; }

        [ApiIgnore]
        public string Currency { get; set; }

        [ApiIgnore]
        public string DistrictID { get; set; }

        [ApiIgnore]
        public string DistrictName { get; set; }

        [ApiIgnore]
        public string GlbuID { get; set; }

        [ApiIgnore]
        public string GlbuName { get; set; }

        [ApiIgnore]
        public string InspyrusOU { get; set; }

        [ApiIgnore]
        public string IntercoSales { get; set; }

        [ApiIgnore]
        public string LegacyDistrictID { get; set; }

        [ApiIgnore]
        public string LegacyDistrictName { get; set; }

        [ApiIgnore]
        public string LobId { get; set; }

        [ApiIgnore]
        public string LobCcOwnerName { get; set; }

        [ApiIgnore]
        public string LobDescription { get; set; }

        [ApiIgnore]
        public string Notes { get; set; }

        [ApiIgnore]
        public string Showroom { get; set; }

        [ApiIgnore]
        public string TrilogieLogon { get; set; }

        [ApiIgnore]
        public string TrilogieAlias { get; set; }

        [ApiIgnore]
        public string WorkdayCompanyID { get; set; }

        #endregion
    }
}
