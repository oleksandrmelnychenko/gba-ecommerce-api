using System.Data;
using GBA.Domain.Repositories.Supplies.ActProvidingServices.Contracts;
using GBA.Domain.Repositories.Supplies.DeliveryProductProtocols.Contracts;
using GBA.Domain.Repositories.Supplies.Documents.Contracts;
using GBA.Domain.Repositories.Supplies.HelperServices.Contracts;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyRepositoriesFactory {
    ISupplyOrderItemRepository NewSupplyOrderItemRepository(IDbConnection connection);

    IPlaneDeliveryServiceRepository NewPlaneDeliveryServiceRepository(IDbConnection connection);

    ICustomAgencyServiceRepository NewCustomAgencyServiceRepository(IDbConnection connection);

    IPortWorkServiceRepository NewPortWorkServiceRepository(IDbConnection connection);

    ICustomServiceRepository NewCustomServiceRepository(IDbConnection connection);

    ITransportationServiceRepository NewTransportationServiceRepository(IDbConnection connection);

    IBillOfLadingDocumentRepository NewBillOfLadingDocumentRepository(IDbConnection connection);

    IContainerServiceRepository NewContainerServiceRepository(IDbConnection connection);

    ISupplyOrderDeliveryDocumentRepository NewSupplyOrderDeliveryDocumentRepository(IDbConnection connection);

    ISupplyDeliveryDocumentRepository NewSupplyDeliveryDocumentRepository(IDbConnection connection);

    IPaymentDeliveryDocumentRepository NewPaymentDeliveryDocumentRepository(IDbConnection connection);

    ISupplyInformationDeliveryProtocolRepository NewSupplyInformationDeliveryProtocolRepository(IDbConnection connection);

    ISupplyInformationDeliveryProtocolKeyRepository NewSupplyInformationDeliveryProtocolKeyRepository(IDbConnection connection);

    ISupplyPaymentTaskRepository NewSupplyPaymentTaskRepository(IDbConnection connection);

    ISupplyOrderPaymentDeliveryProtocolKeyRepository NewSupplyOrderPaymentDeliveryProtocolKeyRepository(IDbConnection connection);

    ISupplyOrderPaymentDeliveryProtocolRepository NewSupplyOrderPaymentDeliveryProtocolRepository(IDbConnection connection);

    IInvoiceDocumentRepository NewInvoiceDocumentRepository(IDbConnection connection);

    ISupplyInvoiceRepository NewSupplyInvoiceRepository(IDbConnection connection);

    IProFormDocumentRepository NewProFormDocumentRepository(IDbConnection connection);

    ISupplyProFormRepository NewSupplyProFormRepository(IDbConnection connection);

    IPackingListDocumentRepository NewPackingListDocumentRepository(IDbConnection connection);

    IResponsibilityDeliveryProtocolRepository NewResponsibilityDeliveryProtocolRepository(IDbConnection connection);

    ISupplyOrderNumberRepository NewSupplyOrderNumberRepository(IDbConnection connection);

    IPortCustomAgencyServiceRepository NewPortCustomAgencyServiceRepository(IDbConnection connection);

    IVehicleDeliveryServiceRepository NewVehicleDeliveryServiceRepository(IDbConnection connection);

    ICreditNoteDocumentRepository NewCreditNoteDocumentRepository(IDbConnection connection);

    ISupplyOrderPolandPaymentDeliveryProtocolRepository NewSupplyOrderPolandPaymentDeliveryProtocolRepository(IDbConnection connection);

    IServiceDetailItemRepository NewServiceDetailItemRepository(IDbConnection connection);

    IServiceDetailItemKeyRepository NewServiceDetailItemKeyRepository(IDbConnection connection);

    ISupplyInformationDeliveryProtocolKeyTranslationRepository NewSupplyInformationDeliveryProtocolKeyTranslationRepository(IDbConnection connection);

    IPackingListRepository NewPackingListRepository(IDbConnection connection);

    IPackingListPackageRepository NewPackingListPackageRepository(IDbConnection connection);

    ISupplyInvoiceOrderItemRepository NewSupplyInvoiceOrderItemRepository(IDbConnection connection);

    IPackingListPackageOrderItemRepository NewPackingListPackageOrderItemRepository(IDbConnection connection);

    ISupplyOrderContainerServiceRepository NewSupplyOrderContainerServiceRepository(IDbConnection connection);

    ISupplyServicesSearchRepository NewSupplyServicesSearchRepository(IDbConnection connection);

    IGroupedPaymentTasksRepository NewGroupedPaymentTasksRepository(IDbConnection connection, IDbConnection currencyExchangeConnection);

    ISupplyPaymentTaskDocumentRepository NewSupplyPaymentTaskDocumentRepository(IDbConnection connection);

    ISupplyOrganizationRepository NewSupplyOrganizationRepository(IDbConnection connection);

    ISupplyOrganizationDocumentRepository NewSupplyOrganizationDocumentRepository(IDbConnection connection);

    ISupplyOrganizationCashFlowRepository NewSupplyOrganizationCashFlowRepository(IDbConnection connection);

    ISupplyServiceNumberRepository NewSupplyServiceNumberRepository(IDbConnection connection);

    ISupplyOrganizationAgreementRepository NewSupplyOrganizationAgreementRepository(IDbConnection connection);

    ISupplyReturnRepository NewSupplyReturnRepository(IDbConnection connection);

    ISupplyReturnItemRepository NewSupplyReturnItemRepository(IDbConnection connection);

    IMergedServiceRepository NewMergedServiceRepository(IDbConnection connection);

    IOrderProductSpecificationRepository NewOrderProductSpecificationRepository(IDbConnection connection);

    IVehicleServiceRepository NewVehicleServiceRepository(IDbConnection connection);

    ISupplyOrderVehicleServiceRepository NewSupplyOrderVehicleServiceRepository(IDbConnection connection);

    IDeliveryProductProtocolRepository NewDeliveryProductProtocolRepository(IDbConnection connection);

    IBillOfLadingServiceRepository NewBillOfLadingServiceRepository(IDbConnection connection);

    IDeliveryProductProtocolDocumentRepository NewDeliveryProductProtocolDocumentRepository(IDbConnection connection);

    IDeliveryProductProtocolNumberRepository NewDeliveryProductProtocolNumberRepository(IDbConnection connection);

    ISupplyInvoiceBillOfLadingServiceRepository NewSupplyInvoiceBillOfLadingServiceRepository(IDbConnection connection);

    ISupplyInvoiceMergedServiceRepository NewSupplyInvoiceMergedServiceRepository(IDbConnection connection);

    ISupplyInvoiceDeliveryDocumentRepository NewSupplyInvoiceDeliveryDocumentRepository(IDbConnection connection);

    ISupplyInformationTaskRepository NewSupplyInformationTaskRepository(IDbConnection connection);

    IPackingListPackageOrderItemSupplyServiceRepository NewPackingListPackageOrderItemSupplyServiceRepository(IDbConnection connection);

    IActProvidingServiceRepository NewActProvidingServiceRepository(IDbConnection connection);

    IActProvidingServiceDocumentRepository NewActProvidingServiceDocumentRepository(IDbConnection connection);

    ISupplyServiceAccountDocumentRepository NewSupplyServiceAccountDocumentRepository(IDbConnection connection);
}