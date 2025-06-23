using System;
using System.Collections.Generic;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The List model.
    /// </summary>
    public class ListModel<T>
    {
        /// <summary>
        /// The Items.
        /// </summary>
        public IList<T> Items { get; set; }
    }
}
