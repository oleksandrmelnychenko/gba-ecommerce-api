using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Documents;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface IPaymentDeliveryDocumentRepository {
    void Add(IEnumerable<PaymentDeliveryDocument> paymentDeliveryDocuments);

    void Update(IEnumerable<PaymentDeliveryDocument> paymentDeliveryDocuments);

    void Remove(Guid netId);
}