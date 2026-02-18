using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Repositories.Clients.RetailClients.Contracts;

public interface IRetailClientPaymentImageRepository {
    long Add(RetailClientPaymentImage paymentImage);

    void Update(RetailClientPaymentImage paymentImage);

    void Remove(long id);

    IEnumerable<RetailClientPaymentImage> GetAllByRetailClientId(long id);

    IEnumerable<RetailClientPaymentImage> GetAllRetailClientNetId(Guid netId);

    IEnumerable<RetailClientPaymentImage> GetAll();

    IEnumerable<RetailClientPaymentImage> GetAllFiltered(
        DateTime? saleDateFrom = null,
        DateTime? saleDateTo = null,
        string saleNumber = "",
        string clientName = "",
        string phoneNumber = "");

    RetailClientPaymentImage GetPaymentImageBySaleNetId(Guid netId);

    RetailClientPaymentImage GetById(long id);
}