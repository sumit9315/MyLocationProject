using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Professional Association LOV model.
    /// </summary>
    public class ProfessionalAssociationLovModel : IComparable
    {
        public string Name { get; set; }

        public string LogoSource { get; set; }

        public string Url { get; set; }

        public int CompareTo(object obj)
        {
            var otherItem = obj as ProfessionalAssociationLovModel;
            if (Name == null)
            {
                return -1;
            }
            return Name.CompareTo(otherItem?.Name);
        }
    }
}
