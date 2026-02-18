using System.Collections.Generic;
using System.Data;
using Akka.Actor;
using GBA.Common;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Ecommerce;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Messages.Ecommerce;
using GBA.Domain.Messages.Ecommerce.SEO;
using GBA.Domain.Messages.Ecommerce.SEO.EcommerceContactInfos;
using GBA.Domain.Messages.Ecommerce.SEO.EcommerceContactsMessages;
using GBA.Domain.Messages.Ecommerce.SEO.EcommercePages;
using GBA.Domain.Repositories.Ecommerce.Contracts;

namespace GBA.Services.Actors.Ecommerce;

public sealed class EcommerceAdminPanelActor : ReceiveActor {
    private readonly IEcommerceAdminPanelRepositoriesFactory _adminPanelRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    public EcommerceAdminPanelActor(
        IEcommerceAdminPanelRepositoriesFactory adminPanelRepositoriesFactory,
        IDbConnectionFactory connectionFactory) {
        _adminPanelRepositoriesFactory = adminPanelRepositoriesFactory;
        _connectionFactory = connectionFactory;

        Receive<AddEcommerceContactInfoMessage>(ProcessAddEcommerceContactInfoMessage);

        Receive<UpdateEcommerceContactInfoMessage>(ProcessUpdateEcommerceContactInfoMessage);

        Receive<AddEcommerceContactsMessage>(ProcessAddEcommerceContactsMessage);

        Receive<GetAllEcommerceContactsMessage>(ProcessGetAllEcommerceContactsMessage);

        Receive<GetEcommerceContactsByIdMessage>(ProcessGetEcommerceContactsByIdMessage);

        Receive<GetEcommerceContactsByNetIdMessage>(ProcessGetEcommerceContactsByNetIdMessage);

        Receive<RemoveEcommerceContactsByNetIdMessage>(ProcessRemoveEcommerceContactsByNetIdMessage);

        Receive<UpdateEcommerceContactsMessage>(ProcessUpdateEcommerceContactsMessage);

        Receive<AddEcommercePageMessage>(ProcessAddEcommercePageMessage);

        Receive<GetAllEcommercePageMessage>(ProcessGetAllEcommercePageMessage);

        Receive<RemoveEcommercePageByNetIdMessage>(ProcessRemoveEcommercePageByNetIdMessage);

        Receive<UpdateEcommercePageMessage>(ProcessUpdateEcommercePageMessage);

        Receive<GetAllSeoInfoMessage>(ProcessGetAllSeoInfoMessage);

        Receive<GetEcommercePageByNetIdMessage>(ProcessGetEcommercePageByNetIdMessage);

        Receive<GetEcommercePageByIdMessage>(ProcessGetEcommercePageByIdMessage);

        Receive<GetAllSeoInfoPageWithLocaleMessage>(ProcessGetAllSeoInfoPageWithLocaleMessage);

        Receive<UpdateSeoPageWithLocaleMessage>(ProcessUpdateSeoPageWithLocaleMessage);

        Receive<UpdateRetailPaymentTypeTranslateMessage>(ProcessUpdateRetailPaymentTypeTranslateMessage);

        Receive<GetAllEcommerceRegionsMessage>(ProcessGetAllEcommerceRegionsByLocaleMessage);

        Receive<UpdateEcommerceRegionMessage>(ProcessUpdateEcommerceRegionMessage);
    }

    private void ProcessUpdateEcommerceRegionMessage(UpdateEcommerceRegionMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IEcommerceRegionRepository ecommerceRegionRepository = _adminPanelRepositoriesFactory.NewEcommerceRegionRepository(connection);

        if (message.EcommerceRegion.IsNew())
            ecommerceRegionRepository.Add(message.EcommerceRegion);
        else
            ecommerceRegionRepository.Update(message.EcommerceRegion);

        Sender.Tell(ecommerceRegionRepository.GetAll());
    }

    private void ProcessGetAllEcommerceRegionsByLocaleMessage(GetAllEcommerceRegionsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IEcommerceRegionRepository ecommerceRegionRepository = _adminPanelRepositoriesFactory.NewEcommerceRegionRepository(connection);

        Sender.Tell(ecommerceRegionRepository.GetAll());
    }

    private void ProcessUpdateRetailPaymentTypeTranslateMessage(UpdateRetailPaymentTypeTranslateMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IEcommerceRetailPaymentTypeTranslateRepository paymentTypeRepository = _adminPanelRepositoriesFactory.NewEcommercePaymentTypeRepository(connection);
        IEcommercePageRepository pageRepository = _adminPanelRepositoriesFactory.NewEcommercePageRepository(connection);
        IEcommerceContactsRepository contactsRepository = _adminPanelRepositoriesFactory.NewEcommerceContactsRepository(connection);
        IEcommerceContactInfoRepository contactInfoRepository = _adminPanelRepositoriesFactory.NewEcommerceContactInfoRepository(connection);
        paymentTypeRepository.Update(message.RetailPaymentTypeTranslate);

        Sender.Tell(FillSeoPageModelFromDb(contactsRepository, pageRepository, contactInfoRepository, paymentTypeRepository));
    }

    private void ProcessUpdateSeoPageWithLocaleMessage(UpdateSeoPageWithLocaleMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IEcommercePageRepository pageRepository = _adminPanelRepositoriesFactory.NewEcommercePageRepository(connection);
        IEcommerceContactsRepository contactsRepository = _adminPanelRepositoriesFactory.NewEcommerceContactsRepository(connection);
        IEcommerceContactInfoRepository contactInfoRepository = _adminPanelRepositoriesFactory.NewEcommerceContactInfoRepository(connection);
        IEcommerceRetailPaymentTypeTranslateRepository paymentTypeRepository = _adminPanelRepositoriesFactory.NewEcommercePaymentTypeRepository(connection);

        pageRepository.Update(message.SeoPage);

        Sender.Tell(FillSeoPageModelFromDb(contactsRepository, pageRepository, contactInfoRepository, paymentTypeRepository));
    }

    private void ProcessGetAllSeoInfoPageWithLocaleMessage(GetAllSeoInfoPageWithLocaleMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IEcommercePageRepository pageRepository = _adminPanelRepositoriesFactory.NewEcommercePageRepository(connection);
        IEcommerceContactsRepository contactsRepository = _adminPanelRepositoriesFactory.NewEcommerceContactsRepository(connection);
        IEcommerceContactInfoRepository contactInfoRepository = _adminPanelRepositoriesFactory.NewEcommerceContactInfoRepository(connection);

        Sender.Tell(FillFullSeoPageModelFromDb(pageRepository, contactsRepository, contactInfoRepository, message.Locale));
    }

    private void ProcessGetEcommercePageByIdMessage(GetEcommercePageByIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_adminPanelRepositoriesFactory.NewEcommercePageRepository(connection).GetById(message.Id));
    }

    private void ProcessGetEcommercePageByNetIdMessage(GetEcommercePageByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_adminPanelRepositoriesFactory.NewEcommercePageRepository(connection).GetByNetId(message.NetId));
    }

    private void ProcessGetAllSeoInfoMessage(GetAllSeoInfoMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IEcommerceContactsRepository ecommerceContactsRepository = _adminPanelRepositoriesFactory.NewEcommerceContactsRepository(connection);
        IEcommercePageRepository ecommercePageRepository = _adminPanelRepositoriesFactory.NewEcommercePageRepository(connection);
        IEcommerceContactInfoRepository ecommerceContactInfoRepository = _adminPanelRepositoriesFactory.NewEcommerceContactInfoRepository(connection);
        IEcommerceRetailPaymentTypeTranslateRepository paymentTypeRepository = _adminPanelRepositoriesFactory.NewEcommercePaymentTypeRepository(connection);

        Sender.Tell(FillSeoPageModelFromDb(ecommerceContactsRepository, ecommercePageRepository, ecommerceContactInfoRepository, paymentTypeRepository));
    }

    private void ProcessUpdateEcommercePageMessage(UpdateEcommercePageMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IEcommercePageRepository pageRepository = _adminPanelRepositoriesFactory.NewEcommercePageRepository(connection);
        IEcommerceContactsRepository contactsRepository = _adminPanelRepositoriesFactory.NewEcommerceContactsRepository(connection);
        IEcommerceContactInfoRepository contactInfoRepository = _adminPanelRepositoriesFactory.NewEcommerceContactInfoRepository(connection);
        IEcommerceRetailPaymentTypeTranslateRepository paymentTypeRepository = _adminPanelRepositoriesFactory.NewEcommercePaymentTypeRepository(connection);

        pageRepository.Update(message.EcommercePage);

        Sender.Tell(FillSeoPageModelFromDb(contactsRepository, pageRepository, contactInfoRepository, paymentTypeRepository));
    }

    private void ProcessRemoveEcommercePageByNetIdMessage(RemoveEcommercePageByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _adminPanelRepositoriesFactory.NewEcommercePageRepository(connection).Remove(message.NetId);
    }

    private void ProcessGetAllEcommercePageMessage(GetAllEcommercePageMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_adminPanelRepositoriesFactory.NewEcommercePageRepository(connection).GetAll());
    }

    private void ProcessAddEcommercePageMessage(AddEcommercePageMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_adminPanelRepositoriesFactory.NewEcommercePageRepository(connection).Add(message.EcommercePage));
    }

    private void ProcessUpdateEcommerceContactsMessage(UpdateEcommerceContactsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IEcommerceContactsRepository contactsRepository = _adminPanelRepositoriesFactory.NewEcommerceContactsRepository(connection);
        IEcommercePageRepository pageRepository = _adminPanelRepositoriesFactory.NewEcommercePageRepository(connection);
        IEcommerceContactInfoRepository contactInfoRepository = _adminPanelRepositoriesFactory.NewEcommerceContactInfoRepository(connection);
        IEcommerceRetailPaymentTypeTranslateRepository paymentTypeRepository = _adminPanelRepositoriesFactory.NewEcommercePaymentTypeRepository(connection);

        contactsRepository.Update(message.EcommerceContacts);

        Sender.Tell(FillSeoPageModelFromDb(contactsRepository, pageRepository, contactInfoRepository, paymentTypeRepository));
    }

    private void ProcessRemoveEcommerceContactsByNetIdMessage(RemoveEcommerceContactsByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IEcommerceRetailPaymentTypeTranslateRepository paymentTypeRepository = _adminPanelRepositoriesFactory.NewEcommercePaymentTypeRepository(connection);
        IEcommerceContactInfoRepository contactInfoRepository = _adminPanelRepositoriesFactory.NewEcommerceContactInfoRepository(connection);
        IEcommerceContactsRepository contactsRepository = _adminPanelRepositoriesFactory.NewEcommerceContactsRepository(connection);
        IEcommercePageRepository pageRepository = _adminPanelRepositoriesFactory.NewEcommercePageRepository(connection);

        contactsRepository.Remove(message.NetId);

        Sender.Tell(FillSeoPageModelFromDb(contactsRepository, pageRepository, contactInfoRepository, paymentTypeRepository));
    }

    private void ProcessGetEcommerceContactsByNetIdMessage(GetEcommerceContactsByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_adminPanelRepositoriesFactory.NewEcommerceContactsRepository(connection).GetByNetId(message.NetId));
    }

    private void ProcessGetEcommerceContactsByIdMessage(GetEcommerceContactsByIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_adminPanelRepositoriesFactory.NewEcommerceContactsRepository(connection).GetById(message.Id));
    }

    private void ProcessGetAllEcommerceContactsMessage(GetAllEcommerceContactsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_adminPanelRepositoriesFactory.NewEcommerceContactsRepository(connection).GetAll());
    }

    private void ProcessAddEcommerceContactsMessage(AddEcommerceContactsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IEcommerceContactsRepository contactsRepository = _adminPanelRepositoriesFactory.NewEcommerceContactsRepository(connection);
        IEcommercePageRepository pageRepository = _adminPanelRepositoriesFactory.NewEcommercePageRepository(connection);
        IEcommerceContactInfoRepository contactInfoRepository = _adminPanelRepositoriesFactory.NewEcommerceContactInfoRepository(connection);
        IEcommerceRetailPaymentTypeTranslateRepository paymentTypeRepository = _adminPanelRepositoriesFactory.NewEcommercePaymentTypeRepository(connection);

        contactsRepository.Add(message.EcommerceContacts);

        Sender.Tell(FillSeoPageModelFromDb(contactsRepository, pageRepository, contactInfoRepository, paymentTypeRepository));
    }

    private void ProcessUpdateEcommerceContactInfoMessage(UpdateEcommerceContactInfoMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IEcommerceRetailPaymentTypeTranslateRepository paymentTypeRepository = _adminPanelRepositoriesFactory.NewEcommercePaymentTypeRepository(connection);
        IEcommerceContactInfoRepository contactInfoRepository = _adminPanelRepositoriesFactory.NewEcommerceContactInfoRepository(connection);
        IEcommerceContactsRepository contactsRepository = _adminPanelRepositoriesFactory.NewEcommerceContactsRepository(connection);
        IEcommercePageRepository pageRepository = _adminPanelRepositoriesFactory.NewEcommercePageRepository(connection);

        contactInfoRepository.Update(message.EcommerceContactInfo);

        Sender.Tell(FillSeoPageModelFromDb(contactsRepository, pageRepository, contactInfoRepository, paymentTypeRepository));
    }

    private void ProcessAddEcommerceContactInfoMessage(AddEcommerceContactInfoMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_adminPanelRepositoriesFactory.NewEcommerceContactInfoRepository(connection).Add(message.EcommerceContactInfo));
    }

    private Dictionary<string, SeoPageModel> FillSeoPageModelFromDb(
        IEcommerceContactsRepository contactsRepository,
        IEcommercePageRepository pageRepository,
        IEcommerceContactInfoRepository contactInfoRepository,
        IEcommerceRetailPaymentTypeTranslateRepository paymentTypeTranslateRepository) {
        Dictionary<string, SeoPageModel> result = new();

        SeoPageModel pageModelUk = new();
        SeoPageModel pageModelRu = new();

        pageModelRu.EcommerceContactsList = contactsRepository.GetAll();
        pageModelRu.EcommercePages = pageRepository.GetAll(SharedStrings.RU);
        pageModelRu.EcommerceContactInfo = contactInfoRepository.GetLast(SharedStrings.RU);

        pageModelUk.EcommerceContactsList = contactsRepository.GetAll();
        pageModelUk.EcommercePages = pageRepository.GetAll(SharedStrings.UK);
        pageModelUk.EcommerceContactInfo = contactInfoRepository.GetLast(SharedStrings.UK);

        pageModelUk.RetailPaymentTypeTranslate = paymentTypeTranslateRepository.GetByCultureCode(SharedStrings.UK);
        pageModelRu.RetailPaymentTypeTranslate = paymentTypeTranslateRepository.GetByCultureCode(SharedStrings.RU);

        result.Add(SharedStrings.UK, pageModelUk);
        result.Add(SharedStrings.RU, pageModelRu);

        return result;
    }

    private FullSeoPageModel FillFullSeoPageModelFromDb(
        IEcommercePageRepository pageRepository,
        IEcommerceContactsRepository contactsRepository,
        IEcommerceContactInfoRepository contactInfoRepository,
        string locale) {
        FullSeoPageModel pageModel = new();

        List<SeoPage> seoPages = pageRepository.GetAll(locale);

        pageModel.HomePage = seoPages[0];
        pageModel.ProductsPage = seoPages[1];
        pageModel.AboutCompanyPage = seoPages[2];
        pageModel.PhotoGalleryPage = seoPages[3];
        pageModel.ContactsPage = seoPages[4];

        pageModel.EcommerceContactsList = contactsRepository.GetAll();
        pageModel.EcommerceContactInfo = contactInfoRepository.GetLast();

        return pageModel;
    }
}