using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Common.Helpers.RetailClients;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Messages.Clients.RetailClients;
using GBA.Domain.Messages.Sales.RetailSales;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Clients.RetailClients.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.VatRates.Contracts;

namespace GBA.Services.Actors.Clients;

public sealed class RetailClientActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IRetailClientRepositoriesFactory _retailClientRepositoriesFactory;
    private readonly IVatRateRepositoriesFactory _vatRateRepositoriesFactory;

    public RetailClientActor(
        IDbConnectionFactory connectionFactory,
        IRetailClientRepositoriesFactory retailClientRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IVatRateRepositoriesFactory vatRateRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _retailClientRepositoriesFactory = retailClientRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _vatRateRepositoriesFactory = vatRateRepositoriesFactory;

        Receive<SaveRetailClientPaymentImageMessage>(ProcessSaveRetailClientPaymentImage);

        Receive<UpdateRetailPaymentImageItemMessage>(ProcessUpdateRetailPaymentImageItemMessage);

        Receive<AddRetailPaymentImageItemMessage>(ProcessAddRetailPaymentImageItemMessage);

        Receive<GetRetailClientPayedAmountBySaleIdMessage>(ProcessGetRetailClientPayedAmountBySaleIdMessage);
    }

    private void ProcessGetRetailClientPayedAmountBySaleIdMessage(GetRetailClientPayedAmountBySaleIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_retailClientRepositoriesFactory.NewRetailPaymentStatusRepository(connection).GetBySaleId(message.SaleId));
    }

    private void ProcessAddRetailPaymentImageItemMessage(AddRetailPaymentImageItemMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            if (message.PaymentImageItem == null) throw new Exception("Item cannot be null");
            if (message.ImageUrl == null) throw new Exception("No image was sent");
            if (message.PaymentImageItem.Amount == decimal.Zero) throw new Exception("Amount cannot be zero");

            IRetailClientPaymentImageItemRepository retailClientPaymentImageItemRepository =
                _retailClientRepositoriesFactory.NewRetailClientPaymentImageItemRepository(connection);

            message.PaymentImageItem.ImgUrl = message.ImageUrl;
            message.PaymentImageItem.UserId = message.PaymentImageItem.User.Id;

            retailClientPaymentImageItemRepository.Add(message.PaymentImageItem);

            RetailClientPaymentImage retailClientPaymentImage =
                _retailClientRepositoriesFactory.NewRetailClientPaymentImageRepository(connection).GetById(message.PaymentImageItem.RetailClientPaymentImageId);

            List<RetailClientPaymentImageItem> retailClientPaymentImageItems =
                retailClientPaymentImageItemRepository.GetAllByRetailClientPaymentImageId(retailClientPaymentImage.Id).ToList();

            IRetailPaymentStatusRepository retailPaymentStatusRepository = _retailClientRepositoriesFactory.NewRetailPaymentStatusRepository(connection);
            RetailPaymentStatus retailPaymentStatus = retailPaymentStatusRepository.GetById(retailClientPaymentImage.RetailPaymentStatusId);
            retailPaymentStatus.Amount =
                decimal.Round(retailClientPaymentImageItems.Sum(e => e.Amount), 2, MidpointRounding.AwayFromZero);

            if (retailPaymentStatus.RetailPaymentStatusType.Equals(RetailPaymentStatusType.New)) retailPaymentStatus.RetailPaymentStatusType = RetailPaymentStatusType.Confirmed;

            if (retailPaymentStatus.RetailPaymentStatusType.Equals(RetailPaymentStatusType.PartialPaid))
                retailPaymentStatus.RetailPaymentStatusType = RetailPaymentStatusType.ChangedToInvoice;

            retailPaymentStatusRepository.Update(retailPaymentStatus);
            retailClientPaymentImage.RetailPaymentStatus = retailPaymentStatusRepository.GetById(retailPaymentStatus.Id);

            long vatRateId = retailClientPaymentImage.Sale.ClientAgreement.Agreement.Organization.VatRateId ?? 0;

            retailClientPaymentImage.Sale.ClientAgreement.Agreement.Organization.VatRate =
                _vatRateRepositoriesFactory.NewVatRateRepository(connection).GetById(vatRateId);

            decimal vatRate = Convert.ToDecimal(retailClientPaymentImage.Sale.ClientAgreement.Agreement.Organization.VatRate?.Value ?? 0) / 100;

            if (retailClientPaymentImage.Sale.IsVatSale)
                retailClientPaymentImage.Sale.Order.TotalVat = retailClientPaymentImage.Sale.Order.TotalAmountLocal * (vatRate / (vatRate + 1));

            // SaleActorsHelpers.CalculatePricingsForSaleWithDynamicPrices(
            //     retailClientPaymentImage.Sale,
            //     _exchangeRateRepositoriesFactory.NewExchangeRateRepository(connection),
            //     _currencyRepositoriesFactory.NewCurrencyRepository(connection));

            Sender.Tell(retailClientPaymentImage);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessUpdateRetailPaymentImageItemMessage(UpdateRetailPaymentImageItemMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            if (message.PaymentImageItem == null) throw new Exception("RetailClientPaymentImageItem cannot be null");
            if (message.PaymentImageItem.Amount == decimal.Zero) throw new Exception("Amount cannot be zero");

            if (message.PaymentImageItem.User != null)
                message.PaymentImageItem.UserId = message.PaymentImageItem.User.Id;

            IRetailClientPaymentImageItemRepository retailClientPaymentImageItemRepository = _retailClientRepositoriesFactory.NewRetailClientPaymentImageItemRepository(connection);

            retailClientPaymentImageItemRepository.Update(message.PaymentImageItem);

            RetailClientPaymentImage retailClientPaymentImage =
                _retailClientRepositoriesFactory.NewRetailClientPaymentImageRepository(connection).GetById(message.PaymentImageItem.RetailClientPaymentImageId);

            List<RetailClientPaymentImageItem> retailClientPaymentImageItems =
                retailClientPaymentImageItemRepository.GetAllByRetailClientPaymentImageId(retailClientPaymentImage.Id).ToList();

            IRetailPaymentStatusRepository retailPaymentStatusRepository = _retailClientRepositoriesFactory.NewRetailPaymentStatusRepository(connection);

            RetailPaymentStatus retailPaymentStatus = retailPaymentStatusRepository.GetById(retailClientPaymentImage.RetailPaymentStatusId);
            retailPaymentStatus.Amount = decimal.Round(retailClientPaymentImageItems.Sum(e => e.Amount), 2, MidpointRounding.AwayFromZero);

            if (retailPaymentStatus.RetailPaymentStatusType.Equals(RetailPaymentStatusType.New)) retailPaymentStatus.RetailPaymentStatusType = RetailPaymentStatusType.Confirmed;

            retailPaymentStatusRepository.Update(retailPaymentStatus);

            RetailClientPaymentImage clientPaymentImage =
                _retailClientRepositoriesFactory.NewRetailClientPaymentImageRepository(connection).GetById(message.PaymentImageItem.RetailClientPaymentImageId);

            decimal vatRate = Convert.ToDecimal(clientPaymentImage.Sale.ClientAgreement.Agreement.Organization.VatRate?.Value ?? 0) / 100;

            if (clientPaymentImage.Sale.IsVatSale)
                clientPaymentImage.Sale.Order.TotalVat = clientPaymentImage.Sale.Order.TotalAmountLocal * (vatRate / (vatRate + 1));

            Sender.Tell(clientPaymentImage);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessSaveRetailClientPaymentImage(SaveRetailClientPaymentImageMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            if (message.SaleNetId.Equals(Guid.Empty)) throw new Exception("Sale need to have existing NetUID");

            RetailClientPaymentImage paymentImage =
                _retailClientRepositoriesFactory.NewRetailClientPaymentImageRepository(connection).GetPaymentImageBySaleNetId(message.SaleNetId);

            if (paymentImage == null) throw new Exception("Such Sale does not exists in database");

            long id = _retailClientRepositoriesFactory.NewRetailClientPaymentImageItemRepository(connection).Add(new RetailClientPaymentImageItem {
                ImgUrl = message.ImgUrl,
                RetailClientPaymentImageId = paymentImage.Id,
                PaymentType = PaymentType.Prepayment
            });

            Sender.Tell(id);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }
}