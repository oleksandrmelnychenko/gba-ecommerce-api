using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GBA.Common;
using GBA.Common.Helpers;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Ecommerce;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Repositories.Ecommerce.Contracts;
using GBA.Domain.Repositories.PaymentOrders.Contracts;
using GBA.Domain.Repositories.Pricings.Contracts;
using GBA.Services.Services.Ecommerce.Contracts;
using Newtonsoft.Json;

namespace GBA.Services.Services.Ecommerce;

public sealed class SeoPageService : ISeoPageService {
    private readonly IEcommerceAdminPanelRepositoriesFactory _adminPanelRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPaymentOrderRepositoriesFactory _paymentOrderRepositoriesFactory;
    private readonly IPricingRepositoriesFactory _pricingRepositoriesFactory;

    public SeoPageService(
        IDbConnectionFactory connectionFactory,
        IEcommerceAdminPanelRepositoriesFactory adminPanelRepositoriesFactory,
        IPricingRepositoriesFactory pricingRepositoriesFactory,
        IPaymentOrderRepositoriesFactory paymentOrderRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _adminPanelRepositoriesFactory = adminPanelRepositoriesFactory;
        _pricingRepositoriesFactory = pricingRepositoriesFactory;
        _paymentOrderRepositoriesFactory = paymentOrderRepositoriesFactory;
    }

    public Task FillDbIfNoData() {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IEcommercePageRepository pageRepository = _adminPanelRepositoriesFactory.NewEcommercePageRepository(connection);
            IEcommerceContactInfoRepository contactInfoRepository = _adminPanelRepositoriesFactory.NewEcommerceContactInfoRepository(connection);
            IEcommerceDefaultPricingRepository defaultPricingRepository = _adminPanelRepositoriesFactory.NewEcommerceDefaultPricingRepository(connection);
            IPricingRepository pricingRepository = _pricingRepositoriesFactory.NewPricingRepository(connection);
            IEcommerceRetailPaymentTypeTranslateRepository retailPaymentTypeTranslateRepository = _adminPanelRepositoriesFactory.NewEcommercePaymentTypeRepository(connection);

            if (!pageRepository.GetAll(SharedStrings.UK).Any()) {
                using StreamReader reader = new(NoltFolderManager.GetPagesLocalePath());
                string json = reader.ReadToEnd();
                List<SeoPage> seoPage = JsonConvert.DeserializeObject<List<SeoPage>>(json);
                pageRepository.AddList(seoPage);
            }

            if (contactInfoRepository.GetLast() == null) {
                using StreamReader reader = new(NoltFolderManager.GetContactInfoPath());
                string json = reader.ReadToEnd();
                List<EcommerceContactInfo> contactInfos = JsonConvert.DeserializeObject<List<EcommerceContactInfo>>(json);
                foreach (EcommerceContactInfo contactInfo in contactInfos) contactInfoRepository.Add(contactInfo);
            }

            if (!pageRepository.GetAll(SharedStrings.UK).Any()) {
                using StreamReader reader = new(NoltFolderManager.GetPagesLocalePath());
                string json = reader.ReadToEnd();
                List<SeoPage> ecommercePages = JsonConvert.DeserializeObject<List<SeoPage>>(json);
                pageRepository.AddList(ecommercePages);
            }

            if (retailPaymentTypeTranslateRepository.GetLast() == null) {
                retailPaymentTypeTranslateRepository.Add(new RetailPaymentTypeTranslate {
                    CultureCode = SharedStrings.UK
                });
                retailPaymentTypeTranslateRepository.Add(new RetailPaymentTypeTranslate {
                    CultureCode = SharedStrings.RU
                });
            }

            if (defaultPricingRepository.GetLast() == null) {
                Pricing pricing = pricingRepository.GetPricingByCurrentCultureWithHighestExtraCharge();

                if (pricing != null) {
                    long id = pricing.Id;

                    defaultPricingRepository.Add(new EcommerceDefaultPricing {
                        PricingId = id,
                        PromotionalPricingId = id
                    });
                }
            }

        return Task.CompletedTask;
    }

    public Task<SeoPageModel> GetAll() {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IEcommerceContactsRepository contactsRepository = _adminPanelRepositoriesFactory.NewEcommerceContactsRepository(connection);
            IEcommercePageRepository pageRepository = _adminPanelRepositoriesFactory.NewEcommercePageRepository(connection);
            IEcommerceContactInfoRepository contactInfoRepository = _adminPanelRepositoriesFactory.NewEcommerceContactInfoRepository(connection);

            SeoPageModel pageModel = new();
            //pageModel.EcommercePages = pageRepository.GetAll();
            pageModel.EcommerceContactsList = contactsRepository.GetAll();
            pageModel.EcommerceContactInfo = contactInfoRepository.GetLast();

            return Task.FromResult(pageModel);
    }

    public Task<FullSeoPageModel> GetAll(string locale) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IEcommerceContactsRepository contactsRepository = _adminPanelRepositoriesFactory.NewEcommerceContactsRepository(connection);
            IEcommercePageRepository pageRepository = _adminPanelRepositoriesFactory.NewEcommercePageRepository(connection);
            IEcommerceContactInfoRepository contactInfoRepository = _adminPanelRepositoriesFactory.NewEcommerceContactInfoRepository(connection);
            IEcommerceRetailPaymentTypeTranslateRepository paymentTypeTranslateRepository = _adminPanelRepositoriesFactory.NewEcommercePaymentTypeRepository(connection);
            IPaymentRegisterRepository paymentRegisterRepository = _paymentOrderRepositoriesFactory.NewPaymentRegisterRepository(connection);

            FullSeoPageModel pageModel = new();

            List<SeoPage> seoPages = pageRepository.GetAll(locale);

            pageModel.HomePage = seoPages[0];
            pageModel.ProductsPage = seoPages[1];
            pageModel.AboutCompanyPage = seoPages[2];
            pageModel.PhotoGalleryPage = seoPages[3];
            pageModel.ContactsPage = seoPages[4];

            pageModel.EcommerceContactsList = contactsRepository.GetAll();
            pageModel.EcommerceContactInfo = contactInfoRepository.GetLast(locale);
            pageModel.RetailPaymentTypeTranslate = paymentTypeTranslateRepository.GetByCultureCode(locale);

            PaymentRegister paymentRegister = paymentRegisterRepository.GetIsSelected();

            if (paymentRegister == null) return Task.FromResult(pageModel);

            pageModel.PaymentCard = new PaymentCard {
                NetUid = paymentRegister.NetUid,
                Id = paymentRegister.Id,
                Number = paymentRegister.AccountNumber
            };

            return Task.FromResult(pageModel);
    }
}
