using System.Data;
using GBA.Domain.Repositories.ConsignmentNoteSettings.Contracts;

namespace GBA.Domain.Repositories.ConsignmentNoteSettings;

public sealed class ConsignmentNoteSettingRepositoriesFactory : IConsignmentNoteSettingRepositoriesFactory {
    public IConsignmentNoteSettingRepository NewConsignmentNoteSettingRepository(IDbConnection connection) {
        return new ConsignmentNoteSettingRepository(connection);
    }
}