using System;
using System.Globalization;
using System.Threading.Tasks;
using GBA.Services.Services.Messengers.Contracts;

namespace GBA.Services.Services.Messengers;

public sealed class PaymentLinkService : IPaymentLinkService {
    public Task<string> GenerateSalePaymentInfoMessage(Guid retailClientNetId, Guid saleNetId) {
        // TODO Viber message sender when we have the API

        // The frontend owns the public origin. Returning a same-origin path avoids
        // leaking checkout capabilities to stale or plaintext hosts in server config.
        string locale = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        return Task.FromResult(
            $"/{locale}/upload/payment/{retailClientNetId}/{saleNetId}");
    }
}
