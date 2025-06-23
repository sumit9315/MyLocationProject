using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The defaults for Branch Additional Content LOV model.
    /// </summary>
    public class BranchAdditionalContentDefaultsLovModel
    {
        public string Kob { get; set; }

        public IList<string> BusinessGroupImageNames { get; set; }
    }
}
