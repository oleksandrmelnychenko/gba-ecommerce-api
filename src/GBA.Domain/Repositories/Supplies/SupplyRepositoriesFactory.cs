using System.Data;
using GBA.Domain.Repositories.Supplies.ActProvidingServices;
using GBA.Domain.Repositories.Supplies.ActProvidingServices.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.Repositories.Supplies.DeliveryProductProtocols;
using GBA.Domain.Repositories.Supplies.DeliveryProductProtocols.Contracts;
using GBA.Domain.Repositories.Supplies.Documents;
using GBA.Domain.Repositories.Supplies.Documents.Contracts;
using GBA.Domain.Repositories.Supplies.HelperServices;
using GBA.Domain.Repositories.Supplies.HelperServices.Contracts;
using GBA.Domain.Repositories.Supplies.PackingLists;
using GBA.Domain.Repositories.Supplies.Protocols;

namespace GBA.Domain.Repositories.Supplies;

public sealed class SupplyRepositoriesFactory : ISupplyRepositoriesFactory {
    public IInvoiceDocumentRepository NewInvoiceDocumentRepository(IDbConnection connection) {
        return new InvoiceDocumentRepository(connection);
    }

    public IPackingListDocumentRepository NewPackingListDocumentRepository(IDbConnection connection) {
        return new PackingListDocumentRepository(connection);
    }

    public IProFormDocumentRepository NewProFormDocumentRepository(IDbConnection connection) {
        return new ProFormDocumentRepository(connection);
    }

    public IResponsibilityDeliveryProtocolRepository NewResponsibilityDeliveryProtocolRepository(IDbConnection connection) {
        return new ResponsibilityDeliveryProtocolRepository(connection);
    }

    public ISupplyInformationDeliveryProtocolRepository NewSupplyInformationDeliveryProtocolRepository(IDbConnection connection) {
        return new SupplyInformationDeliveryProtocolRepository(connection);
    }

    public ISupplyInvoiceRepository NewSupplyInvoiceRepository(IDbConnection connection) {
        return new SupplyInvoiceRepository(connection);
    }

    public ISupplyInformationDeliveryProtocolKeyRepository NewSupplyInformationDeliveryProtocolKeyRepository(IDbConnection connection) {
        return new SupplyInformationDeliveryProtocolKeyRepository(connection);
    }

    public ISupplyOrderNumberRepository NewSupplyOrderNumberRepository(IDbConnection connection) {
        return new SupplyOrderNumberRepository(connection);
    }

    public ISupplyOrderPaymentDeliveryProtocolKeyRepository NewSupplyOrderPaymentDeliveryProtocolKeyRepository(IDbConnection connection) {
        return new SupplyOrderPaymentDeliveryProtocolKeyRepository(connection);
    }

    public ISupplyOrderPaymentDeliveryProtocolRepository NewSupplyOrderPaymentDeliveryProtocolRepository(IDbConnection connection) {
        return new SupplyOrderPaymentDeliveryProtocolRepository(connection);
    }

    public ISupplyOrderRepository NewSupplyOrderRepository(IDbConnection connection) {
        return new SupplyOrderRepository(connection);
    }

    public ISupplyPaymentTaskRepository NewSupplyPaymentTaskRepository(IDbConnection connection) {
        return new SupplyPaymentTaskRepository(connection);
    }

    public ISupplyProFormRepository NewSupplyProFormRepository(IDbConnection connection) {
        return new SupplyProFormRepository(connection);
    }

    public IPaymentDeliveryDocumentRepository NewPaymentDeliveryDocumentRepository(IDbConnection connection) {
        return new PaymentDeliveryDocumentRepository(connection);
    }

    public ISupplyDeliveryDocumentRepository NewSupplyDeliveryDocumentRepository(IDbConnection connection) {
        return new SupplyDeliveryDocumentRepository(connection);
    }

    public ISupplyOrderDeliveryDocumentRepository NewSupplyOrderDeliveryDocumentRepository(IDbConnection connection) {
        return new SupplyOrderDeliveryDocumentRepository(connection);
    }

    public IContainerServiceRepository NewContainerServiceRepository(IDbConnection connection) {
        return new ContainerServiceRepository(connection);
    }

    public IBillOfLadingDocumentRepository NewBillOfLadingDocumentRepository(IDbConnection connection) {
        return new BillOfLadingDocumentRepository(connection);
    }

    public ITransportationServiceRepository NewTransportationServiceRepository(IDbConnection connection) {
        return new TransportationServiceRepository(connection);
    }

    public ICustomServiceRepository NewCustomServiceRepository(IDbConnection connection) {
        return new CustomServiceRepository(connection);
    }

    public IPortWorkServiceRepository NewPortWorkServiceRepository(IDbConnection connection) {
        return new PortWorkServiceRepository(connection);
    }

    public ICustomAgencyServiceRepository NewCustomAgencyServiceRepository(IDbConnection connection) {
        return new CustomAgencyServiceRepository(connection);
    }

    public IPortCustomAgencyServiceRepository NewPortCustomAgencyServiceRepository(IDbConnection connection) {
        return new PortCustomAgencyServiceRepository(connection);
    }

    public IVehicleDeliveryServiceRepository NewVehicleDeliveryServiceRepository(IDbConnection connection) {
        return new VehicleDeliveryServiceRepository(connection);
    }

    public IPlaneDeliveryServiceRepository NewPlaneDeliveryServiceRepository(IDbConnection connection) {
        return new PlaneDeliveryServiceRepository(connection);
    }

    public ISupplyOrderItemRepository NewSupplyOrderItemRepository(IDbConnection connection) {
        return new SupplyOrderItemRepository(connection);
    }

    public ICreditNoteDocumentRepository NewCreditNoteDocumentRepository(IDbConnection connection) {
        return new CreditNoteDocumentRepository(connection);
    }

    public ISupplyOrderPolandPaymentDeliveryProtocolRepository NewSupplyOrderPolandPaymentDeliveryProtocolRepository(IDbConnection connection) {
        return new SupplyOrderPolandPaymentDeliveryProtocolRepository(connection);
    }

    public IServiceDetailItemRepository NewServiceDetailItemRepository(IDbConnection connection) {
        return new ServiceDetailItemRepository(connection);
    }

    public ISupplyInformationDeliveryProtocolKeyTranslationRepository NewSupplyInformationDeliveryProtocolKeyTranslationRepository(IDbConnection connection) {
        return new SupplyInformationDeliveryProtocolKeyTranslationRepository(connection);
    }

    public IServiceDetailItemKeyRepository NewServiceDetailItemKeyRepository(IDbConnection connection) {
        return new ServiceDetailItemKeyRepository(connection);
    }

    public IPackingListRepository NewPackingListRepository(IDbConnection connection) {
        return new PackingListRepository(connection);
    }

    public IPackingListPackageRepository NewPackingListPackageRepository(IDbConnection connection) {
        return new PackingListPackageRepository(connection);
    }

    public ISupplyInvoiceOrderItemRepository NewSupplyInvoiceOrderItemRepository(IDbConnection connection) {
        return new SupplyInvoiceOrderItemRepository(connection);
    }

    public IPackingListPackageOrderItemRepository NewPackingListPackageOrderItemRepository(IDbConnection connection) {
        return new PackingListPackageOrderItemRepository(connection);
    }

    public ISupplyOrderContainerServiceRepository NewSupplyOrderContainerServiceRepository(IDbConnection connection) {
        return new SupplyOrderContainerServiceRepository(connection);
    }

    public ISupplyServicesSearchRepository NewSupplyServicesSearchRepository(IDbConnection connection) {
        return new SupplyServicesSearchRepository(connection);
    }

    public IGroupedPaymentTasksRepository NewGroupedPaymentTasksRepository(IDbConnection connection, IDbConnection currencyExchangeConnection) {
        return new GroupedPaymentTasksRepository(connection, currencyExchangeConnection);
    }

    public ISupplyPaymentTaskDocumentRepository NewSupplyPaymentTaskDocumentRepository(IDbConnection connection) {
        return new SupplyPaymentTaskDocumentRepository(connection);
    }

    public ISupplyOrganizationRepository NewSupplyOrganizationRepository(IDbConnection connection) {
        return new SupplyOrganizationRepository(connection);
    }

    public ISupplyOrganizationDocumentRepository NewSupplyOrganizationDocumentRepository(IDbConnection connection) {
        return new SupplyOrganizationDocumentRepository(connection);
    }

    public ISupplyOrganizationCashFlowRepository NewSupplyOrganizationCashFlowRepository(IDbConnection connection) {
        return new SupplyOrganizationCashFlowRepository(connection);
    }

    public ISupplyServiceNumberRepository NewSupplyServiceNumberRepository(IDbConnection connection) {
        return new SupplyServiceNumberRepository(connection);
    }

    public ISupplyOrganizationAgreementRepository NewSupplyOrganizationAgreementRepository(IDbConnection connection) {
        return new SupplyOrganizationAgreementRepository(connection);
    }

    public ISupplyReturnRepository NewSupplyReturnRepository(IDbConnection connection) {
        return new SupplyReturnRepository(connection);
    }

    public ISupplyReturnItemRepository NewSupplyReturnItemRepository(IDbConnection connection) {
        return new SupplyReturnItemRepository(connection);
    }

    public IMergedServiceRepository NewMergedServiceRepository(IDbConnection connection) {
        return new MergedServiceRepository(connection);
    }

    public IOrderProductSpecificationRepository NewOrderProductSpecificationRepository(IDbConnection connection) {
        return new OrderProductSpecificationRepository(connection);
    }

    public IVehicleServiceRepository NewVehicleServiceRepository(IDbConnection connection) {
        return new VehicleServiceRepository(connection);
    }

    public ISupplyOrderVehicleServiceRepository NewSupplyOrderVehicleServiceRepository(IDbConnection connection) {
        return new SupplyOrderVehicleServiceRepository(connection);
    }

    public IDeliveryProductProtocolRepository NewDeliveryProductProtocolRepository(IDbConnection connection) {
        return new DeliveryProductProtocolRepository(connection);
    }

    public IBillOfLadingServiceRepository NewBillOfLadingServiceRepository(IDbConnection connection) {
        return new BillOfLadingServiceRepository(connection);
    }

    public IDeliveryProductProtocolDocumentRepository NewDeliveryProductProtocolDocumentRepository(IDbConnection connection) {
        return new DeliveryProductProtocolDocumentRepository(connection);
    }

    public IDeliveryProductProtocolNumberRepository NewDeliveryProductProtocolNumberRepository(IDbConnection connection) {
        return new DeliveryProductProtocolNumberRepository(connection);
    }

    public ISupplyInvoiceBillOfLadingServiceRepository NewSupplyInvoiceBillOfLadingServiceRepository(IDbConnection connection) {
        return new SupplyInvoiceBillOfLadingServiceRepository(connection);
    }

    public ISupplyInvoiceMergedServiceRepository NewSupplyInvoiceMergedServiceRepository(IDbConnection connection) {
        return new SupplyInvoiceMergedServiceRepository(connection);
    }

    public ISupplyInvoiceDeliveryDocumentRepository NewSupplyInvoiceDeliveryDocumentRepository(IDbConnection connection) {
        return new SupplyInvoiceDeliveryDocumentRepository(connection);
    }

    public ISupplyInformationTaskRepository NewSupplyInformationTaskRepository(IDbConnection connection) {
        return new SupplyInformationTaskRepository(connection);
    }

    public IPackingListPackageOrderItemSupplyServiceRepository NewPackingListPackageOrderItemSupplyServiceRepository(IDbConnection connection) {
        return new PackingListPackageOrderItemSupplyServiceRepository(connection);
    }

    public IActProvidingServiceRepository NewActProvidingServiceRepository(IDbConnection connection) {
        return new ActProvidingServiceRepository(connection);
    }

    public IActProvidingServiceDocumentRepository NewActProvidingServiceDocumentRepository(IDbConnection connection) {
        return new ActProvidingServiceDocumentRepository(connection);
    }

    public ISupplyServiceAccountDocumentRepository NewSupplyServiceAccountDocumentRepository(IDbConnection connection) {
        return new SupplyServiceAccountDocumentRepository(connection);
    }
}