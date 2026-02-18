using GBA.Domain.Entities;

namespace GBA.Domain.Messages.Countries;

public sealed class UpdateCountryMessage {
    public UpdateCountryMessage(Country country) {
        Country = country;
    }

    public Country Country { get; }
}