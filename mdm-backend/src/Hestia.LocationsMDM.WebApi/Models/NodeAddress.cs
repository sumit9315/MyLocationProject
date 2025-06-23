namespace Hestia.LocationsMDM.WebApi.Models
{
    public class NodeAddress : NodeMainAddress
    {
        public string AltShiptoAddressLine1 { get; set; }

        public string AltShiptoAddressLine2 { get; set; }

        public string AltShiptoAddressLine3 { get; set; }

        public string AltShiptoCityName { get; set; }

        public string AltShiptoState { get; set; }

        public string AltShiptoCountryName { get; set; }

        public string AltShiptoPostalCodePrimary { get; set; }

        public string AltShiptoPostalCodeExtn { get; set; }
    }
}
