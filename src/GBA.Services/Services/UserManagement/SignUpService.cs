using System;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using GBA.Common.IdentityConfiguration.Roles;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.EntityHelpers;
using GBA.Domain.IdentityEntities;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Ecommerce.Contracts;
using GBA.Domain.Repositories.Identities.Contracts;
using GBA.Domain.Repositories.Regions.Contracts;
using GBA.Services.Services.Clients.Contracts;
using GBA.Services.Services.UserManagement.Contracts;

namespace GBA.Services.Services.UserManagement;

public sealed class SignUpService : ISignUpService {
    private readonly IClientAgreementService _clientAgreementService;

    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IEcommerceAdminPanelRepositoriesFactory _ecommerceRepositoriesFactory;

    private readonly IIdentityRepositoriesFactory _identityRepositoriesFactory;

    private readonly IRegionRepositoriesFactory _regionRepositoriesFactory;

    public SignUpService(
        IDbConnectionFactory connectionFactory,
        IIdentityRepositoriesFactory identityRepositoriesFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IRegionRepositoriesFactory regionRepositoriesFactory,
        IEcommerceAdminPanelRepositoriesFactory ecommerceRepositoriesFactory,
        IClientAgreementService clientAgreementService
    ) {
        _connectionFactory = connectionFactory;

        _identityRepositoriesFactory = identityRepositoriesFactory;

        _clientRepositoriesFactory = clientRepositoriesFactory;

        _regionRepositoriesFactory = regionRepositoriesFactory;
        _ecommerceRepositoriesFactory = ecommerceRepositoriesFactory;
        _clientAgreementService = clientAgreementService;
    }

    public async Task<Tuple<IdentityResponse, Client>> SignUp(
        Client client,
        string password,
        Guid ecommerceRegionNetId) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IIdentityRepository identityRepository = _identityRepositoriesFactory.NewIdentityRepository();
            IClientRepository clientRepository = _clientRepositoriesFactory.NewClientRepository(connection);
            IRegionCodeRepository regionCodeRepository = _regionRepositoriesFactory.NewRegionCodeRepository(connection);
            IRegionRepository regionRepository = _regionRepositoriesFactory.NewRegionRepository(connection);
            var ecommerceRegion = _ecommerceRepositoriesFactory
                .NewEcommerceRegionRepository(connection)
                .GetByNetId(ecommerceRegionNetId);
            if (ecommerceRegion == null)
                throw new ArgumentException("A valid ecommerce region is required.");

            //client.RegionCode.RegionId = regionRepository.GetLastRecord().Id;
            //client.RegionCodeId = regionCodeRepository.Add(client.RegionCode);

            //client.IsTemporaryClient = true;
            client.IsFromECommerce = true;
            client.OrderExpireDays = 3;

            client.Id = clientRepository.Add(client);

            if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower().Equals("uk"))
                client.ClientInRole = new ClientInRole {
                    ClientTypeId = 2,
                    ClientTypeRoleId = 1,
                    ClientId = client.Id
                };
            else
                client.ClientInRole = new ClientInRole {
                    ClientTypeId = 2,
                    ClientTypeRoleId = 2,
                    ClientId = client.Id
                };

            _clientRepositoriesFactory.NewClientInRoleRepository(connection).Add(client.ClientInRole);

            client = clientRepository.GetById(client.Id);

            //UpdateAbbreviation
            if (!string.IsNullOrEmpty(client.LastName) || !string.IsNullOrEmpty(client.FirstName)) {
                if (!string.IsNullOrEmpty(client.LastName)) client.Abbreviation += client.LastName.ToCharArray()[0];
                if (!string.IsNullOrEmpty(client.FirstName)) client.Abbreviation += client.FirstName.ToCharArray()[0];

                clientRepository.UpdateAbbreviation(client);
            }

            UserIdentity user = new() {
                Email = client.EmailAddress,
                UserName = !string.IsNullOrEmpty(client.MobileNumber)
                    ? client.MobileNumber
                    : client.EmailAddress,
                PhoneNumber = client.MobileNumber,
                NetId = client.NetUid,
                Region = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                UserType = IdentityUserType.Client
            };

            IdentityResponse response = await identityRepository.CreateUser(user, password, false);

            if (response.Succeeded) {
                await identityRepository.AddUserRoleAndClaims(user, IdentityRoles.ClientUa);
                await _clientAgreementService.AddDefaultAgreementForClient(client, ecommerceRegion.IsLocalPayment);
            } else {
                clientRepository.Remove(client.Id);
            }

            return new Tuple<IdentityResponse, Client>(response, client);
    }
}
