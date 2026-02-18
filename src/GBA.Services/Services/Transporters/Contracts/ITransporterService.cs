using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GBA.Domain.Entities.Transporters;

namespace GBA.Services.Services.Transporters.Contracts;

public interface ITransporterService {
    Task<List<TransporterType>> GetAllTransporterTypes();

    Task<List<Transporter>> GetAllTransportersByTransporterTypeNetId(Guid netId);
}