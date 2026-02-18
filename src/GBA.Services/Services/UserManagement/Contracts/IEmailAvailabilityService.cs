using System.Threading.Tasks;
using GBA.Domain.EntityHelpers;

namespace GBA.Services.Services.UserManagement.Contracts;

public interface IEmailAvailabilityService {
    Task<IdentityResponse> IsEmailAvailableAsync(string email);
}