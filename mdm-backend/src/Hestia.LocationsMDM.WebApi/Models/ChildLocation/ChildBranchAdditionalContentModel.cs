using System.Collections.Generic;

namespace Hestia.LocationsMDM.WebApi.Models
{
    public class ChildBranchAdditionalContentModel
    {
        public string AboutUs { get; set; }
        public string UrlPrefix { get; set; }
        public string UrlSuffix { get; set; }
        public string AppointmentUrl { get; set; }
        public string BranchPageUrl { get; set; }
        public string BranchImage { get; set; }
        public IList<string> GalleryImageNames { get; set; }
        public IList<string> BusinessGroupImageNames { get; set; }
    }
}