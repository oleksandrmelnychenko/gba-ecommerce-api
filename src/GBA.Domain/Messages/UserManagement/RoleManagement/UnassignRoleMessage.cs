namespace GBA.Domain.Messages.UserManagement.RoleManagement;

public sealed class UnassignRoleMessage {
    public UnassignRoleMessage(string userName, string role) {
        UserName = userName;

        Role = role;
    }

    public string UserName { get; set; }

    public string Role { get; set; }
}