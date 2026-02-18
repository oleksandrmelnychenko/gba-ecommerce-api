using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Transporters;
using GBA.Domain.Repositories.Transporters.Contracts;
using GBA.Services.Services.Transporters.Contracts;

namespace GBA.Services.Services.Transporters;

public sealed class TransporterService : ITransporterService {
    private readonly IDbConnectionFactory _connectionFactory;

    private readonly ITransporterRepositoriesFactory _transporterRepositoriesFactory;

    public TransporterService(
        IDbConnectionFactory connectionFactory,
        ITransporterRepositoriesFactory transporterRepositoriesFactory) {
        _connectionFactory = connectionFactory;

        _transporterRepositoriesFactory = transporterRepositoriesFactory;
    }

    public Task<List<TransporterType>> GetAllTransporterTypes() {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        return Task.FromResult(_transporterRepositoriesFactory.NewTransporterTypeRepository(connection).GetAll());
    }

    public Task<List<Transporter>> GetAllTransportersByTransporterTypeNetId(Guid netId) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        return Task.FromResult(_transporterRepositoriesFactory.NewTransporterRepository(connection).GetAllByTransporterTypeNetId(netId));
    }
}
