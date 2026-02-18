using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Akka.Actor;
using GBA.Common.IdentityConfiguration.Roles;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Users;
using GBA.Domain.EntityHelpers;
using GBA.Domain.IdentityEntities;
using GBA.Domain.Messages.UserManagement;
using GBA.Domain.Repositories.Agreements.Contracts;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Identities.Contracts;
using GBA.Domain.Repositories.Regions.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Domain.Repositories.UserRoles.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.UserManagement;

public sealed class SignUpActor : ReceiveActor {
    private readonly IAgreementRepositoriesFactory _agreementRepositoriesFactory;
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IIdentityRepositoriesFactory _identityRepositoriesFactory;
    private readonly IRegionRepositoriesFactory _regionRepositoriesFactory;
    private readonly IStorageRepositoryFactory _storageRepositoryFactory;
    private readonly IUserRepositoriesFactory _userRepositoryFactory;
    private readonly IUserRoleRepositoriesFactory _userRoleRepositoriesFactory;

    public SignUpActor(
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoryFactory,
        IIdentityRepositoriesFactory identityRepositoriesFactory,
        IUserRoleRepositoriesFactory userRoleRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IRegionRepositoriesFactory regionRepositoriesFactory,
        IAgreementRepositoriesFactory agreementRepositoriesFactory,
        IStorageRepositoryFactory storageRepositoryFactory
    ) {
        _connectionFactory = connectionFactory;
        _userRepositoryFactory = userRepositoryFactory;
        _identityRepositoriesFactory = identityRepositoriesFactory;
        _userRoleRepositoriesFactory = userRoleRepositoriesFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _regionRepositoriesFactory = regionRepositoriesFactory;
        _agreementRepositoriesFactory = agreementRepositoriesFactory;
        _storageRepositoryFactory = storageRepositoryFactory;
        ReceiveAsync<OldUsersMessage>(async message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IIdentityRepository identityRepository = _identityRepositoriesFactory.NewIdentityRepository();
            IClientRepository clientRepository = _clientRepositoriesFactory.NewClientRepository(connection);
            IRegionCodeRepository regionCodeRepository = _regionRepositoriesFactory.NewRegionCodeRepository(connection);
            IRegionRepository regionRepository = _regionRepositoriesFactory.NewRegionRepository(connection);
            List<OldUserShop> haveUsers = new();
            List<Client> clients = new();
            foreach (OldUserShop item in message.OldUsers)
                if (item.Password != null && item.Email != "" && item.Username != null) {
                    UserIdentity user = await identityRepository.GetUserName(item.Email);
                    Client clientEmail = clientRepository.GetEmailDeleted(item.Email);
                    if (clientEmail != null) {
                        Client ClientRegionCode = clientRepository.GetOriginalRegionCode(item.Username);
                        if (ClientRegionCode?.MainClientId != null) clientEmail = clientRepository.GetById((long)ClientRegionCode.MainClientId);
                    }

                    if (user != null && clientEmail != null) {
                        await identityRepository.DeleteUserByNetId(user.NetId.ToString());
                        user.NetId = clientEmail.NetUid;
                        user.UserType = IdentityUserType.Client;

                        IdentityResponse response = await identityRepository.CreateUser(user, item.Password, false);
                        if (response.Succeeded) await identityRepository.AddUserRoleAndClaims(user, IdentityRoles.ClientUa);
                    } else if (clientEmail != null) {
                        //clientEmail = clientRepository.GetById(clientEmail.Id);
                        UserIdentity userIdentity = new() {
                            Email = clientEmail.EmailAddress,
                            UserName = clientEmail.EmailAddress,
                            PhoneNumber = clientEmail.MobileNumber,
                            NetId = clientEmail.NetUid,
                            Region = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                            UserType = IdentityUserType.Client
                        };

                        IdentityResponse response = await identityRepository.CreateUser(userIdentity, item.Password, false);

                        if (response.Succeeded) await identityRepository.AddUserRoleAndClaims(userIdentity, IdentityRoles.ClientUa);
                    }
                }

            foreach (OldUserShop item in message.OldUsers)
                if (item.Password != null && item.Email != "" && item.Username != null) {
                    Client clientEmail = clientRepository.GetEmailDeleted(item.Email);
                    if (clientEmail != null) {
                        Client ClientRegionCode = clientRepository.GetOriginalRegionCode(item.Username);
                        if (ClientRegionCode?.MainClientId != null) clientEmail = clientRepository.GetById((long)ClientRegionCode.MainClientId);
                        if (!haveUsers.Any(x => x.Email == item.Email)) haveUsers.Add(item);
                    }
                }

            Sender.Tell(haveUsers);
        });

        ReceiveAsync<SignUpMessage>(async message => {
            try {
                using IDbConnection connection = _connectionFactory.NewSqlConnection();
                IIdentityRepository identityRepository = _identityRepositoriesFactory.NewIdentityRepository();
                IUserRepository userRepository = _userRepositoryFactory.NewUserRepository(connection);

                UserRole userProfileRole = _userRoleRepositoriesFactory.NewUserRoleRepository(connection).GetByNetIdWithoutTranslation(message.UserProfile.UserRole.NetUid);

                message.UserProfile.UserRoleId = userProfileRole.Id;

                if (!string.IsNullOrEmpty(message.UserProfile.LastName)) {
                    char[] chars = message.UserProfile.LastName.ToCharArray();

                    if (chars.Any()) message.UserProfile.Abbreviation += chars.First();
                }

                if (!string.IsNullOrEmpty(message.UserProfile.FirstName)) {
                    char[] chars = message.UserProfile.FirstName.ToCharArray();

                    if (chars.Any()) message.UserProfile.Abbreviation += chars.First();
                }

                long userId = userRepository.Add(message.UserProfile);

                message.UserProfile = userRepository.GetById(userId);

                UserIdentity user = new() {
                    Email = message.UserProfile.Email,
                    UserName = message.UserProfile.PhoneNumber,
                    PhoneNumber = message.UserProfile.PhoneNumber,
                    NetId = message.UserProfile.NetUid,
                    Region = message.UserProfile.Region,
                    UserType = IdentityUserType.User
                };

                IdentityResponse response = await identityRepository.CreateUser(user, message.Password);

                if (response.Succeeded)
                    await identityRepository.AddUserRoleAndClaims(user, userProfileRole.Name);
                else
                    userRepository.Remove(message.UserProfile.NetUid);

                Sender.Tell(response);
            } catch (Exception exc) {
                Sender.Tell(exc);
            }
        });
    }
}
