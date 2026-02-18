using System.Data;

namespace GBA.Domain.Repositories.ConsignmentNoteSettings.Contracts;

public interface IConsignmentNoteSettingRepositoriesFactory {
    IConsignmentNoteSettingRepository NewConsignmentNoteSettingRepository(IDbConnection connection);
}