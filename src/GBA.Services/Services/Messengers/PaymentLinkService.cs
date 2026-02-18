using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using GBA.Common.Helpers;
using GBA.Common.Models;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Services.Services.Messengers.Contracts;

namespace GBA.Services.Services.Messengers;

public sealed class PaymentLinkService : IPaymentLinkService {
    private readonly IDbConnectionFactory _connectionFactory;
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() {
        PropertyNameCaseInsensitive = true
    };

    public PaymentLinkService(IDbConnectionFactory connectionFactory) {
        _connectionFactory = connectionFactory;
    }

    public Task<string> GenerateSalePaymentInfoMessage(Guid retailClientNetId, Guid saleNetId) {
        // TODO Viber message sender when we have the API

        UriBuilder ecommerceClientUrl;

        if (File.Exists(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath())) {
            EcommerceCrmConfig data = JsonSerializer.Deserialize<EcommerceCrmConfig>(
                File.ReadAllText(NoltFolderManager.GetEcommerceCrmConfigJsonFilePath()),
                _jsonSerializerOptions);
#if DEBUG
            //TODO Make locale dynamic
            //ecommerceClientUrl = new UriBuilder($"http://localhost:7000/uk/upload/payment/{retailClientNetId}/{saleNetId}");
            ecommerceClientUrl = new UriBuilder($"{data?.EcommerceClientUrl}/uk/upload/payment/{retailClientNetId}/{saleNetId}");
#else
                    ecommerceClientUrl = new UriBuilder($"{data?.EcommerceClientUrlRelease}/uk/upload/payment/{retailClientNetId}/{saleNetId}");
#endif
        } else {
            ecommerceClientUrl = new UriBuilder($"http://93.183.224.42/api/v1/{CultureInfo.CurrentCulture}/sales/payment/save");
        }

        return Task.FromResult(ecommerceClientUrl.ToString());
    }
}
