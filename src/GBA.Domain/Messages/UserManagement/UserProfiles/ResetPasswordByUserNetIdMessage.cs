using System;

namespace GBA.Domain.Messages.UserManagement.UserProfiles;

public sealed class ResetPasswordByUserNetIdMessage {
    public ResetPasswordByUserNetIdMessage(Guid netId, string password) {
        NetId = netId;

        Password = password;
    }

    public Guid NetId { get; }

    public string Password { get; }
}