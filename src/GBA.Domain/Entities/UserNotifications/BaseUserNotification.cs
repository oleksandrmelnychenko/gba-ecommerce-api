namespace GBA.Domain.Entities.UserNotifications;

public abstract class BaseUserNotification : EntityBase {
    public UserNotificationType UserNotificationType { get; set; }

    public bool Locked { get; set; }

    public bool Processed { get; set; }

    public long CreatedById { get; set; }

    public long? LockedById { get; set; }

    public long? LastViewedById { get; set; }

    public long? ProcessedById { get; set; }

    public virtual User CreatedBy { get; set; }

    public virtual User LockedBy { get; set; }

    public virtual User LastViewedBy { get; set; }

    public virtual User ProcessedBy { get; set; }
}