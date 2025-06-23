using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Hestia.LocationsMDM.WebApi.Models
{
    /// <summary>
    /// The Hierarchy Node.
    /// </summary>
    public class HierarchyNode : IdentifiableModel, IComparable<HierarchyNode>
    {
        /// <summary>
        /// The type of the location.
        /// </summary>
        public string LocationType { get; set; }

        /// <summary>
        /// The name of the node.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The inventory org.
        /// </summary>
        public string InventoryOrg { get; set; }

        /// <summary>
        /// The address of the node.
        /// </summary>
        public NodeAddress Address { get; set; }

        /// <summary>
        /// The partition key of the node.
        /// </summary>
        public string PartitionKey { get; set; }

        /// <summary>
        /// The location id of the node.
        /// </summary>
        public string LocationId { get; set; }

        /// <summary>
        /// Gets or Sets Children
        /// </summary>
        public IList<HierarchyNode> Children { get; set; }

        public HierarchyNodeType NodeType { get; set; }

        /// <summary>
        /// Gets or Sets Children
        /// </summary>
        public HierarchyNode ParentDoc { get; set; }

        /// <summary>
        /// Compares this instance with a specified string object and indicates whether
        /// this instance precedes, follows, or appears in the same position in the
        /// sort order as the specified string.
        /// </summary>
        /// 
        // <paramref name="other" /> is <see langword="null" />.</returns>
        public int CompareTo(HierarchyNode other)
        {
            return this.Name.CompareTo(other.Name);
        }
    }
}
