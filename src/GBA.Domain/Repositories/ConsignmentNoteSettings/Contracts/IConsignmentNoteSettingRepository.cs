using System;
using System.Collections.Generic;
using GBA.Domain.Entities.ConsignmentNoteSettings;

namespace GBA.Domain.Repositories.ConsignmentNoteSettings.Contracts;

public interface IConsignmentNoteSettingRepository {
    void Add(ConsignmentNoteSetting consignmentNoteSetting);

    void Update(ConsignmentNoteSetting consignmentNoteSetting);

    void Remove(Guid netId);

    ConsignmentNoteSetting GetByNetId(Guid netId);

    List<ConsignmentNoteSetting> GetAll(bool forReSale);
}