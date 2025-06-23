using System.Collections.Generic;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// An model that represents base search criteria class.
    /// </summary>
    public class SearchCriteria
    {
        /// <summary>
        /// The page number.
        /// </summary>
        public int PageNum { get; set; } = 1;

        /// <summary>
        /// The page size.
        /// </summary>
        public int PageSize { get; set; } = 50;

        /// <summary>
        /// The sort by field.
        /// </summary>
        public string SortBy { get; set; }

        /// <summary>
        /// The sort order.
        /// </summary>
        public SortOrder SortOrder { get; set; }
    }
}
