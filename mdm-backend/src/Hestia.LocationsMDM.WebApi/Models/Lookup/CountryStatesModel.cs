using System.Collections.Generic;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Country with States model.
    /// </summary>
    public class CountryStatesModel
    {
        public string CountryName { get; set; }

        public IList<string> States { get; set; }
    }
}
