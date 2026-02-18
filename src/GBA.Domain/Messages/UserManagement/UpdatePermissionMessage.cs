using GBA.Domain.Entities;

namespace GBA.Domain.Messages.UserManagement;

public sealed class UpdatePermissionMessage {
    public UpdatePermissionMessage(Permission permission) {
        Permission = permission;
    }

    public Permission Permission { get; }
}