using System;

namespace GBA.Domain.Messages.UserManagement.UserProfiles;

public sealed class AddScreenResolutionMessage {
    public AddScreenResolutionMessage(int width, int height, Guid userNetId) {
        Width = width;
        Height = height;
        UserNetId = userNetId;
    }

    public int Width { get; set; }

    public int Height { get; set; }

    public Guid UserNetId { get; set; }
}