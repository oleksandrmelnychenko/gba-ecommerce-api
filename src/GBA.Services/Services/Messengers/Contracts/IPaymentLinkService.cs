using System;
using System.Threading.Tasks;

namespace GBA.Services.Services.Messengers.Contracts;

public interface IPaymentLinkService {
    Task<string> GenerateSalePaymentInfoMessage(Guid retailClientNetId, Guid saleNetId);
}