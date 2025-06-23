using Hestia.LocationsMDM.WebApi.Common.Constants;
using Hestia.LocationsMDM.WebApi.Models;
using Newtonsoft.Json.Linq;

namespace MDM.Tools.Common
{
    // NOTE: In future this can be moved to shared package that will be used by both MDM backend and MDM Tools
    public static class JObjectModelExtensions
    {
        public static string Node(this JToken item)
        {
            var value = item.Value<string>("node");
            return value;
        }

        public static string CampusNodeId(this JToken item)
        {
            var value = item.Value<string>("campusNodeId");
            return value;
        }

        public static string RegionNodeId(this JToken item)
        {
            var value = item.Value<string>("regionNodeId");
            return value;
        }

        public static string LocationID(this JToken item)
        {
            var value = item.Value<string>("locationID");
            return value;
        }

        public static string LocationName(this JToken item)
        {
            var value = item.Value<string>("locationName");
            return value;
        }

        public static string LocationType(this JToken item)
        {
            var value = item.Value<string>("locationType");
            return value;
        }

        public static string RegionID(this JToken item)
        {
            var value = item.Value<string>("regionID");
            return value;
        }

        public static string RegionName(this JToken item)
        {
            var value = item.Value<string>("regionName");
            return value;
        }

        public static string KOB(this JToken item)
        {
            var value = item.Value<string>("kob");
            return value;
        }

        public static JArray FinancialData(this JToken item)
        {
            var value = item["financialData"] as JArray;
            return value;
        }

        public static JObject BranchAdditionalContent(this JToken item)
        {
            var value = item["branchAdditionalContent"] as JObject;
            return value;
        }

        public static JObject Address(this JToken item)
        {
            var value = item["address"] as JObject;
            return value;
        }

        public static string AddressLine1(this JToken item)
        {
            var value = item.Value<string>("addressLine1");
            return value;
        }

        public static string City(this JToken item)
        {
            var value = item.Value<string>("cityName");
            return value;
        }

        public static string State(this JToken item)
        {
            var value = item.Value<string>("state");
            return value;
        }

        public static NodeAddress AddressModel(this JToken item)
        {
            var value = item.Value<NodeAddress>("address");
            return value;
        }

        public static bool IsCustomerFacingLocation(this JObject childLoc)
        {
            string locType = childLoc.LocationType();
            bool result = LocationTypes.CustomerFacingLocationTypes.Contains(locType);
            return result;
        }

    }
}
