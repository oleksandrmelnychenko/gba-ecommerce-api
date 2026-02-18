using System.Collections.Generic;
using GBA.Common.Helpers;

namespace GBA.Domain.Messages.UserManagement.UserProfiles;

public sealed class GetAllUserProfileByRoleTypesMessage {
    public GetAllUserProfileByRoleTypesMessage(IEnumerable<UserRoleType> userRoleTypes) {
        UserRoleTypes = userRoleTypes;
    }

    public IEnumerable<UserRoleType> UserRoleTypes { get; set; }
}