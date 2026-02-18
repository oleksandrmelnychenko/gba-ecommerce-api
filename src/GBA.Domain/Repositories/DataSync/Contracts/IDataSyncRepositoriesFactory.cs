using System.Data;

namespace GBA.Domain.Repositories.DataSync.Contracts;

public interface IDataSyncRepositoriesFactory {
    IMeasureUnitsSyncRepository NewMeasureUnitsSyncRepository(IDbConnection oneCConnection, IDbConnection remoteSyncConnection, IDbConnection amgSyncConnection);

    IProductGroupsSyncRepository NewProductGroupsSyncRepository(IDbConnection oneCConnection, IDbConnection remoteSyncConnection, IDbConnection amgSyncConnection);

    IProductsSyncRepository NewProductsSyncRepository(IDbConnection oneCConnection, IDbConnection remoteSyncConnection, IDbConnection amgSyncConnection);

    IRegionsSyncRepository NewRegionsSyncRepository(IDbConnection oneCConnection, IDbConnection remoteSyncConnection, IDbConnection amgSyncConnection);

    IClientsSyncRepository NewClientsSyncRepository(IDbConnection oneCConnection, IDbConnection remoteSyncConnection, IDbConnection amgSyncConnection);

    IPaymentRegistersSyncRepository NewPaymentRegistersSyncRepository(IDbConnection oneCConnection, IDbConnection remoteSyncConnection, IDbConnection amgSyncConnection);

    IAccountingSyncRepository NewAccountingSyncRepository(IDbConnection oneCConnection, IDbConnection remoteSyncConnection, IDbConnection amgSyncConnection);

    IConsignmentsSyncRepository NewConsignmentsSyncRepository(IDbConnection oneCConnection, IDbConnection amgOneCConnection, IDbConnection remoteSyncConnection);

    IDataSyncOperationRepository NewDataSyncOperationRepository(IDbConnection connection);

    IIncomedOrdersSyncRepository NewIncomedOrdersSyncRepository(IDbConnection oneCConnection, IDbConnection amgOneCConnection, IDbConnection remoteSyncConnection);

    IOutcomeOrdersSyncRepository NewOutcomeOrdersSyncRepository(IDbConnection oneCConnection, IDbConnection remoteSyncConnection, IDbConnection amgSyncConnection);

    IDocumentsAfterSyncRepository NewDocumentsAfterSyncRepository(IDbConnection connection);

    IGbaDataExportRepository NewGbaDataExportRepository(IDbConnection connection);
}