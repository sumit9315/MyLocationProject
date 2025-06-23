namespace Hestia.LocationsMDM.WebApi.Models
{
    public class NodeMainAddress : NodePrimaryAddress
    {
        public string AddressLine2 { get; set; }

        public string AddressLine3 { get; set; }

        public string PostalCodeExtn { get; set; }
    }
}
