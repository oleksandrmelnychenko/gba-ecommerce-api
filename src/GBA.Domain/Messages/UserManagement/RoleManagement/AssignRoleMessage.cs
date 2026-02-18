namespace GBA.Domain.Messages.UserManagement.RoleManagement;

public sealed class AssignRoleMessage {
    public AssignRoleMessage(string userName, string role) {
        UserName = userName;

        Role = role;
    }

    public string UserName { get; set; }

    public string Role { get; set; }
}