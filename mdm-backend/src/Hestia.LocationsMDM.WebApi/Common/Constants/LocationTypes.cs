using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hestia.LocationsMDM.WebApi.Common.Constants
{
    public static class LocationTypes
    {
        public const string AuxiliaryStorage = "Auxiliary Storage";
        public const string Counter = "Counter";
        public const string DistributionCenter = "Distribution Center";
        public const string MarketDistributionCenter = "Market Distribution Center";
        public const string OfficeSpace = "Office Space";
        public const string OutsideStorageYard = "Outside Storage Yard";
        public const string ShipHubs = "ShipHubs";
        public const string Showroom = "Showroom";
        public const string SalesOffice = "Sales Office";
        public const string Warehouse = "Warehouse";

        public static readonly IList<string> CustomerFacingLocationTypes = new List<string>
        {
            Counter,
            SalesOffice,
            Showroom
        };
    }
}
