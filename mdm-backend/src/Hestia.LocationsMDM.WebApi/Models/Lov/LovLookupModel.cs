using System;
using System.Collections.Generic;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The LOV lookup model.
    /// </summary>
    public class LovLookupModel : IComparable
    {
        public string Value { get; set; }

        public string DisplayName { get; set; }

        public int CompareTo(object obj)
        {
            var otherItem = obj as LovLookupModel;
            if (DisplayName == null)
            {
                return -1;
            }
            return DisplayName.CompareTo(otherItem?.DisplayName);
        }
    }
}
