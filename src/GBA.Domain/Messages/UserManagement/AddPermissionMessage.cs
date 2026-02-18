using GBA.Domain.Entities;

namespace GBA.Domain.Messages.UserManagement;

public sealed class AddPermissionMessage {
    public AddPermissionMessage(Permission permission) {
        Permission = permission;
    }

    public Permission Permission { get; }
}