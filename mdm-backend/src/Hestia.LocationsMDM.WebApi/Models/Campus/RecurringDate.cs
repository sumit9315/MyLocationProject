using System.Runtime.Serialization;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Recurring date details.
    /// </summary>
    public class RecurringDate
    {
        /// <summary>
        /// The month, if applicable. (0 is Janaury, December is 11)
        /// </summary>
        public int? Month { get; set; }

        /// <summary>
        /// The day of month (1-31).
        /// </summary>
        public int? DayOfMonth { get; set; }

        /// <summary>
        /// The day of week (0-6)
        /// </summary>
        public int? DayOfWeek { get; set; }

        /// <summary>
        /// The xth day of week. (0 is first, 1 is second, 2 is third, 3 is fourth, and 4 is the last)
        /// </summary>
        public int? OrdinalInMonth { get; set; }
    }
}
