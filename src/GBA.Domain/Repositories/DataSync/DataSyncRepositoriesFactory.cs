using System.Data;
using GBA.Domain.Repositories.DataSync.Contracts;

namespace GBA.Domain.Repositories.DataSync;

public sealed class DataSyncRepositoriesFactory : IDataSyncRepositoriesFactory {
    public IMeasureUnitsSyncRepository NewMeasureUnitsSyncRepository(IDbConnection oneCConnection, IDbConnection remoteSyncConnection, IDbConnection amgSyncConnection) {
        return new MeasureUnitsSyncRepository(oneCConnection, remoteSyncConnection, amgSyncConnection);
    }

    public IProductGroupsSyncRepository NewProductGroupsSyncRepository(IDbConnection oneCConnection, IDbConnection remoteSyncConnection, IDbConnection amgSyncConnection) {
        return new ProductGroupsSyncRepository(oneCConnection, remoteSyncConnection, amgSyncConnection);
    }

    public IProductsSyncRepository NewProductsSyncRepository(IDbConnection oneCConnection, IDbConnection remoteSyncConnection, IDbConnection amgSyncConnection) {
        return new ProductsSyncRepository(oneCConnection, remoteSyncConnection, amgSyncConnection);
    }

    public IRegionsSyncRepository NewRegionsSyncRepository(IDbConnection oneCConnection, IDbConnection remoteSyncConnection, IDbConnection amgSyncConnection) {
        return new RegionsSyncRepository(oneCConnection, remoteSyncConnection, amgSyncConnection);
    }

    public IClientsSyncRepository NewClientsSyncRepository(IDbConnection oneCConnection, IDbConnection remoteSyncConnection, IDbConnection amgSyncConnection) {
        return new ClientsSyncRepository(oneCConnection, remoteSyncConnection, amgSyncConnection);
    }

    public IPaymentRegistersSyncRepository NewPaymentRegistersSyncRepository(IDbConnection oneCConnection, IDbConnection remoteSyncConnection, IDbConnection amgSyncConnection) {
        return new PaymentRegistersSyncRepository(oneCConnection, remoteSyncConnection, amgSyncConnection);
    }

    public IAccountingSyncRepository NewAccountingSyncRepository(IDbConnection oneCConnection, IDbConnection remoteSyncConnection, IDbConnection amgSyncConnection) {
        return new AccountingSyncRepository(oneCConnection, remoteSyncConnection, amgSyncConnection);
    }

    public IConsignmentsSyncRepository NewConsignmentsSyncRepository(IDbConnection oneCConnection, IDbConnection amgOneCConnection, IDbConnection remoteSyncConnection) {
        return new ConsignmentsSyncRepository(oneCConnection, amgOneCConnection, remoteSyncConnection);
    }

    public IDataSyncOperationRepository NewDataSyncOperationRepository(IDbConnection connection) {
        return new DataSyncOperationRepository(connection);
    }

    public IIncomedOrdersSyncRepository NewIncomedOrdersSyncRepository(IDbConnection oneCConnection, IDbConnection amgOneCConnection, IDbConnection remoteSyncConnection) {
        return new IncomedOrdersSyncSyncRepository(oneCConnection, amgOneCConnection, remoteSyncConnection);
    }

    public IOutcomeOrdersSyncRepository NewOutcomeOrdersSyncRepository(IDbConnection oneCConnection, IDbConnection remoteSyncConnection, IDbConnection amgSyncConnection) {
        return new OutcomeOrdersSyncRepository(oneCConnection, remoteSyncConnection, amgSyncConnection);
    }

    public IDocumentsAfterSyncRepository NewDocumentsAfterSyncRepository(IDbConnection connection) {
        return new DocumentsAfterSyncRepository(connection);
    }

    public IGbaDataExportRepository NewGbaDataExportRepository(IDbConnection connection) {
        return new GbaDataExportRepository(connection);
    }
}