using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Messages.Sales.Offers;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.Sales;

public sealed class SaleOffersActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public SaleOffersActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;

        Receive<AddNewSaleOfferMessage>(ProcessAddNewSaleOfferMessage);

        Receive<UpdateSaleOfferValidityPeriodMessage>(ProcessUpdateSaleOfferValidityPeriodMessage);

        Receive<ProcessSaleOfferMessage>(ProcessProcessSaleOfferMessage);

        Receive<GetAllSaleOffersFilteredMessage>(ProcessGetAllSaleOffersFilteredMessage);
    }

    private void ProcessAddNewSaleOfferMessage(AddNewSaleOfferMessage message) {
        if (message.ClientShoppingCart == null) {
            Sender.Tell(new Tuple<ClientShoppingCart, string>(null, "ClientShoppingCart entity can not be empty"));
        } else if (message.ClientShoppingCart.ClientAgreement == null || message.ClientShoppingCart.ClientAgreement.IsNew()) {
            Sender.Tell(new Tuple<ClientShoppingCart, string>(null, "ClientAgreement need to be specified"));
        } else if (!message.ClientShoppingCart.OrderItems.Any()) {
            Sender.Tell(new Tuple<ClientShoppingCart, string>(null, "You need to add at least one product to offer"));
        } else if (!message.ClientShoppingCart.OrderItems.Any(i => i.IsNew() && i.Product != null && !i.Product.IsNew() && i.Qty > 0)) {
            Sender.Tell(new Tuple<ClientShoppingCart, string>(null, "You need to add at least one product to offer"));
        } else if (!message.ClientShoppingCart.IsNew()) {
            Sender.Tell(new Tuple<ClientShoppingCart, string>(null, "Existing offer is not valid input for current request"));
        } else {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

            ClientAgreement clientAgreement =
                _clientRepositoriesFactory.NewClientAgreementRepository(connection).GetByIdWithAgreementAndOrganization(message.ClientShoppingCart.ClientAgreement.Id);

            if (clientAgreement == null) {
                Sender.Tell(new Tuple<ClientShoppingCart, string>(null, "Specified ClientAgreement does not exists"));
            } else {
                IClientShoppingCartRepository clientShoppingCartRepository = _clientRepositoriesFactory.NewClientShoppingCartRepository(connection);

                message.ClientShoppingCart.ValidUntil =
                    DateTime.Now.Date.AddDays(
                        message.ValidDays > 0 ? message.ValidDays : 2
                    );

                message.ClientShoppingCart.IsVatCart = clientAgreement.Agreement.WithVATAccounting;

                message.ClientShoppingCart.ClientAgreementId = clientAgreement.Id;

                message.ClientShoppingCart.CreatedById = user.Id;

                ClientShoppingCart lastOfferByCulture =
                    clientShoppingCartRepository.GetLastOfferByCulture(clientAgreement.Agreement.Organization.Culture);

                if (clientAgreement.Agreement.Organization.Culture.Equals("pl"))
                    message.ClientShoppingCart.Number = lastOfferByCulture != null
                        ? string.Format("P{0:D10}", Convert.ToInt32(lastOfferByCulture.Number.Substring(1)) + 1)
                        : string.Format("P{0:D10}", 1);
                else
                    message.ClientShoppingCart.Number = lastOfferByCulture != null
                        ? string.Format("{0:D10}", Convert.ToInt32(lastOfferByCulture.Number) + 1)
                        : string.Format("{0:D10}", 1);

                message.ClientShoppingCart.Id = clientShoppingCartRepository.AddAsOffer(message.ClientShoppingCart);

                _saleRepositoriesFactory
                    .NewOrderItemRepository(connection)
                    .Add(
                        message
                            .ClientShoppingCart
                            .OrderItems
                            .Where(i => i.IsNew() && i.Product != null && !i.Product.IsNew() && i.Qty > 0)
                            .Select(item => {
                                item.ProductId = item.Product.Id;
                                item.ClientShoppingCartId = message.ClientShoppingCart.Id;
                                item.UserId = user.Id;

                                return item;
                            })
                    );

                Sender.Tell(new Tuple<ClientShoppingCart, string>(clientShoppingCartRepository.GetById(message.ClientShoppingCart.Id), string.Empty));
            }
        }
    }

    private void ProcessUpdateSaleOfferValidityPeriodMessage(UpdateSaleOfferValidityPeriodMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IClientShoppingCartRepository shoppingCartRepository = _clientRepositoriesFactory.NewClientShoppingCartRepository(connection);

        ClientShoppingCart offer = shoppingCartRepository.GetByNetId(message.NetId);

        if (offer != null && message.ValidDays > 0) {
            offer.ValidUntil = DateTime.Now.Date.AddDays(message.ValidDays);

            shoppingCartRepository.UpdateValidUntilDate(offer);
        }

        Sender.Tell(offer);
    }

    private void ProcessProcessSaleOfferMessage(ProcessSaleOfferMessage message) {
        if (message.Offer != null) {
            if (!message.Offer.IsNew()) {
                using IDbConnection connection = _connectionFactory.NewSqlConnection();
                IClientShoppingCartRepository clientShoppingCartRepository = _clientRepositoriesFactory.NewClientShoppingCartRepository(connection);

                ClientShoppingCart offerFromDb = clientShoppingCartRepository.GetByNetId(message.Offer.NetUid);

                if (offerFromDb != null) {
                    if (message.Offer.Deleted) {
                        clientShoppingCartRepository.Remove(message.Offer.NetUid);

                        Sender.Tell(new Tuple<ClientShoppingCart, string>(clientShoppingCartRepository.GetByNetId(message.Offer.NetUid), string.Empty));
                    } else {
                        List<OrderItem> changedItems = new();

                        User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

                        foreach (OrderItem orderItem in message.Offer.OrderItems.Where(i => !i.OfferProcessingStatus.Equals(OfferProcessingStatus.FullyProcessed)))
                            if (!string.IsNullOrEmpty(orderItem.Comment) && !string.IsNullOrEmpty(orderItem.Comment.Trim())
                                                                         && !orderItem.Comment.Equals(offerFromDb.OrderItems.First(i => i.Id.Equals(orderItem.Id))
                                                                             .Comment)) {
                                orderItem.OfferProcessingStatusChangedById = user.Id;
                                orderItem.OfferProcessingStatus = OfferProcessingStatus.FullyProcessed;

                                changedItems.Add(orderItem);
                            }

                        _saleRepositoriesFactory.NewOrderItemRepository(connection).SetOfferProcessingStatuses(changedItems);

                        if (!string.IsNullOrEmpty(message.Offer.Comment) && !string.IsNullOrEmpty(message.Offer.Comment.Trim()) &&
                            !message.Offer.Comment.Equals(offerFromDb.Comment)) {
                            message.Offer.OfferProcessingStatusChangedById = user.Id;

                            clientShoppingCartRepository.UpdateProcessingStatus(message.Offer);
                        }

                        if (!offerFromDb.OfferProcessingStatus.Equals(OfferProcessingStatus.FullyProcessed) &&
                            message.Offer.OrderItems.All(i => i.OfferProcessingStatus.Equals(OfferProcessingStatus.FullyProcessed))) {
                            message.Offer.OfferProcessingStatus = OfferProcessingStatus.FullyProcessed;

                            clientShoppingCartRepository.UpdateProcessingStatus(message.Offer);
                        }

                        Sender.Tell(new Tuple<ClientShoppingCart, string>(clientShoppingCartRepository.GetByNetId(message.Offer.NetUid), string.Empty));
                    }
                } else {
                    Sender.Tell(new Tuple<ClientShoppingCart, string>(null, "Such offer does not exists in database"));
                }
            } else {
                Sender.Tell(new Tuple<ClientShoppingCart, string>(null, "New offer is not valid input for current request"));
            }
        } else {
            Sender.Tell(new Tuple<ClientShoppingCart, string>(null, "Offer entity can not be empty"));
        }
    }

    private void ProcessGetAllSaleOffersFilteredMessage(GetAllSaleOffersFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IClientShoppingCartRepository clientShoppingCartRepository = _clientRepositoriesFactory.NewClientShoppingCartRepository(connection);

        List<ClientShoppingCart> offers =
            clientShoppingCartRepository
                .GetAllOffersFiltered(
                    message.From,
                    message.To
                );

        foreach (ClientShoppingCart offer in offers) {
            foreach (OrderItem orderItem in offer.OrderItems) {
                orderItem.TotalAmount = decimal.Round(orderItem.Product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountLocal = orderItem.Product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty);

                orderItem.Product.CurrentLocalPrice = decimal.Round(orderItem.Product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                orderItem.TotalAmount = decimal.Round(orderItem.TotalAmount, 2, MidpointRounding.AwayFromZero);
                orderItem.TotalAmountLocal = decimal.Round(orderItem.TotalAmountLocal, 2, MidpointRounding.AwayFromZero);
            }

            offer.TotalAmount = decimal.Round(offer.OrderItems.Sum(o => o.TotalAmount), 2, MidpointRounding.AwayFromZero);
            offer.TotalLocalAmount = decimal.Round(offer.OrderItems.Sum(o => o.TotalAmountLocal), 2, MidpointRounding.AwayFromZero);

            if (!offer.IsOfferProcessed)
                offer.OfferProcessingStatus = offer.ValidUntil < DateTime.Now.Date ? OfferProcessingStatus.NotProcessed : OfferProcessingStatus.PartiallyProcessed;
            else
                offer.OfferProcessingStatus = OfferProcessingStatus.FullyProcessed;
        }

        Sender.Tell(offers);
    }
}