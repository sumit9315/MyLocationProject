using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hestia.LocationsMDM.WebApi.Common.Constants
{
    public static class KOBs
    {
        public const string Showroom = "Showroom";
        public const string SelectionCenter = "Selection Center";
        public const string Plumbing = "Plumbing/PVF";
        public const string HVAC = "HVAC";
        public const string Waterworks = "Waterworks";
        public const string MechanicalIndustrial = "Mechanical/Industrial";
        public const string FireAndFabrication = "Fire & Fabrication";

        public static readonly IList<string> ShowroomKOBs = new List<string>
        {
            Showroom,
            SelectionCenter
        };

        public static readonly IList<string> CounterKOBs = new List<string>
        {
            Plumbing,
            HVAC,
            Waterworks,
            MechanicalIndustrial,
            FireAndFabrication
        };

        public static bool IsValidLocationTypeKOB(string locationType, string kob)
        {
            if (locationType == LocationTypes.Showroom)
            {
                return ShowroomKOBs.Contains(kob);
            }
            
            if (locationType == LocationTypes.Counter)
            {
                return CounterKOBs.Contains(kob);
            }

            bool hasKob = kob != null;
            bool requireKOB = LocationTypes.CustomerFacingLocationTypes.Contains(locationType);
            return hasKob == requireKOB;
        }
    }
}
