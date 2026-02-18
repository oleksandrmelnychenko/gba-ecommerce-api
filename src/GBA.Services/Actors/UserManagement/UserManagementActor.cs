using Akka.Actor;
using Akka.DI.Core;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.UserManagement;

public sealed class UserManagementActor : ReceiveActor {
    public UserManagementActor() {
        ActorReferenceManager.Instance.Add(
            UserActorNames.SIGN_UP_ACTOR,
            Context.ActorOf(Context.DI().Props<SignUpActor>(), UserActorNames.SIGN_UP_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            UserActorNames.EMAIL_AVAILABILITY_ACTOR,
            Context.ActorOf(Context.DI().Props<EmailAvailabilityActor>(), UserActorNames.EMAIL_AVAILABILITY_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            UserActorNames.USER_NAME_AVAILABILITY_ACTOR,
            Context.ActorOf(Context.DI().Props<UserNameAvailabilityActor>(), UserActorNames.USER_NAME_AVAILABILITY_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            UserActorNames.REQUEST_TOKEN_ACTOR,
            Context.ActorOf(Context.DI().Props<RequestTokenActor>(), UserActorNames.REQUEST_TOKEN_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            UserActorNames.ROLE_MANAGEMENT_ACTOR,
            Context.ActorOf(Context.DI().Props<RoleManagementActor>(), UserActorNames.ROLE_MANAGEMENT_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            UserActorNames.USER_PROFILES_ACTOR,
            Context.ActorOf(Context.DI().Props<UserProfilesActor>(), UserActorNames.USER_PROFILES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            UserActorNames.USER_PROFILE_ROLES_ACTOR,
            Context.ActorOf(Context.DI().Props<UserProfileRolesActor>(), UserActorNames.USER_PROFILE_ROLES_ACTOR)
        );

        ActorReferenceManager.Instance.Add(
            UserActorNames.USER_PERMISSIONS_ACTOR,
            Context.ActorOf(Context.DI().Props<UserPermissionsActor>(), UserActorNames.USER_PERMISSIONS_ACTOR));
    }
}