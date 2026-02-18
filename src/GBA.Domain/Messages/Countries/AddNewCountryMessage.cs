using GBA.Domain.Entities;

namespace GBA.Domain.Messages.Countries;

public sealed class AddNewCountryMessage {
    public AddNewCountryMessage(Country country) {
        Country = country;
    }

    public Country Country { get; }
}