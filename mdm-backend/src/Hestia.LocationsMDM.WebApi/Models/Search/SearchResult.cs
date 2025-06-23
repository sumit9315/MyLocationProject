using System.Collections.Generic;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// An model that represents search result.
    /// </summary>
    ///
    /// <typeparam name="T">The type of the items in the search result.</typeparam>
    public class SearchResult<T>
    {
        /// <summary>
        /// The total records count.
        /// </summary>
        /// <example>124</example>
        public int TotalCount { get; set; }

        /// <summary>
        /// The items.
        /// </summary>
        public IList<T> Items { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchResult{T}"/> class.
        /// </summary>
        public SearchResult()
        {
            Items = new List<T>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchResult{T}"/> class.
        /// </summary>
        /// <param name="items">The items.</param>
        public SearchResult(IList<T> items)
        {
            Items = items;
        }
    }
}
