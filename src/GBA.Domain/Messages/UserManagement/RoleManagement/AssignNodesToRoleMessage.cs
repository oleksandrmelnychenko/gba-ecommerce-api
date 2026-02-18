using GBA.Domain.Entities;

namespace GBA.Domain.Messages.UserManagement.RoleManagement;

public sealed class AssignNodesToRoleMessage {
    public AssignNodesToRoleMessage(UserRole userRole) {
        UserRole = userRole;
    }

    public UserRole UserRole { get; }
}