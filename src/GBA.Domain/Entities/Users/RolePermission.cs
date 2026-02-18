namespace GBA.Domain.Entities;

public sealed class RolePermission : EntityBase {
    public long UserRoleId { get; set; }
    public long PermissionId { get; set; }
    public UserRole UserRole { get; set; }
    public Permission Permission { get; set; }
}