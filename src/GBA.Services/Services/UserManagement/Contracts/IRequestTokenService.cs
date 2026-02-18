using System;
using System.Threading.Tasks;
using GBA.Common.IdentityConfiguration.Entities;

namespace GBA.Services.Services.UserManagement.Contracts;

public interface IRequestTokenService {
    Task<Tuple<bool, string, CompleteAccessToken>> RequestToken(string userName, string password);

    Task<Tuple<bool, string, CompleteAccessToken>> RefreshToken(string refreshToken);
}