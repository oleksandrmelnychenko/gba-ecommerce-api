using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Ecommerce;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Repositories.Ecommerce.Contracts;
using GBA.Domain.Repositories.PaymentOrders.Contracts;
using GBA.Domain.Repositories.Pricings.Contracts;
using GBA.Services.Services.Ecommerce;
using Moq;
using Xunit;

namespace GBA.Ecommerce.Api.Tests;

public sealed class SeoPageServiceTests {
    [Fact]
    public async Task Incomplete_locale_returns_available_pages_without_throwing() {
        const string locale = "uk";
        SeoPage homePage = new() { PageName = "Home" };
        SeoPage productsPage = new() { PageName = "Products" };

        Mock<IDbConnection> connection = new();
        Mock<IDbConnectionFactory> connectionFactory = new();
        connectionFactory
            .Setup(factory => factory.NewSqlConnection())
            .Returns(connection.Object);

        Mock<IEcommercePageRepository> pageRepository = new();
        pageRepository
            .Setup(repository => repository.GetAll(locale))
            .Returns(new List<SeoPage> { homePage, productsPage });

        Mock<IEcommerceAdminPanelRepositoriesFactory> ecommerceRepositoriesFactory = new();
        ecommerceRepositoriesFactory
            .Setup(factory => factory.NewEcommercePageRepository(connection.Object))
            .Returns(pageRepository.Object);
        ecommerceRepositoriesFactory
            .Setup(factory => factory.NewEcommerceContactsRepository(connection.Object))
            .Returns(Mock.Of<IEcommerceContactsRepository>());
        ecommerceRepositoriesFactory
            .Setup(factory => factory.NewEcommerceContactInfoRepository(connection.Object))
            .Returns(Mock.Of<IEcommerceContactInfoRepository>());
        ecommerceRepositoriesFactory
            .Setup(factory => factory.NewEcommercePaymentTypeRepository(connection.Object))
            .Returns(Mock.Of<IEcommerceRetailPaymentTypeTranslateRepository>());

        Mock<IPaymentOrderRepositoriesFactory> paymentRepositoriesFactory = new();
        paymentRepositoriesFactory
            .Setup(factory => factory.NewPaymentRegisterRepository(connection.Object))
            .Returns(Mock.Of<IPaymentRegisterRepository>());

        SeoPageService service = new(
            connectionFactory.Object,
            ecommerceRepositoriesFactory.Object,
            Mock.Of<IPricingRepositoriesFactory>(),
            paymentRepositoriesFactory.Object);

        FullSeoPageModel result = await service.GetAll(locale);

        Assert.Same(homePage, result.HomePage);
        Assert.Same(productsPage, result.ProductsPage);
        Assert.Null(result.AboutCompanyPage);
        Assert.Null(result.PhotoGalleryPage);
        Assert.Null(result.ContactsPage);
    }
}
