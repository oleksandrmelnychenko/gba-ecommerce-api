using System.Threading.Tasks;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Repositories.Identities.Contracts;
using GBA.Services.Services.UserManagement.Contracts;

namespace GBA.Services.Services.UserManagement;

public sealed class EmailAvailabilityService : IEmailAvailabilityService {
    private readonly IIdentityRepositoriesFactory _identityRepositoriesFactory;

    public EmailAvailabilityService(IIdentityRepositoriesFactory identityRepositoriesFactory) {
        _identityRepositoriesFactory = identityRepositoriesFactory;
    }

    public Task<IdentityResponse> IsEmailAvailableAsync(string email) {
        return _identityRepositoriesFactory
            .NewIdentityRepository()
            .IsEmailAvailableAsync(email);
    }
}
