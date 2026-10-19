using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using GBA.Common.Configuration;
using GBA.Common.IdentityConfiguration.Policies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace GBA.Ecommerce.Auth;

public static class ShopAuthenticationSchemes {
    public const string Customer = JwtBearerDefaults.AuthenticationScheme;
    public const string GbaInternal = "GbaInternal";
    public const string CustomerAuthenticationType = "ShopCustomer";
}

public static class ShopIdentityTypes {
    public const string ClaimType = "type";
    public const string Client = "Client";
    public const string Workplace = "Workplace";
    public const string User = "User";

    public static bool IsCustomer(ClaimsIdentity? identity) =>
        identity != null &&
        (identity.HasClaim(ClaimType, Client) || identity.HasClaim(ClaimType, Workplace)) &&
        !identity.HasClaim(ClaimType, User);

    public static bool IsInternalUser(ClaimsIdentity? identity) =>
        identity != null && identity.HasClaim(ClaimType, User);
}

public sealed class InternalAdministratorRequirement : IAuthorizationRequirement {
    public InternalAdministratorRequirement(IEnumerable<string> roles) {
        Roles = roles
            .Select(role => role.Trim())
            .Where(role => role.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyCollection<string> Roles { get; }
}

public sealed class InternalAdministratorAuthorizationHandler : AuthorizationHandler<InternalAdministratorRequirement> {
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        InternalAdministratorRequirement requirement) {
        bool allowed = context.User.Identities.Any(identity =>
            identity.IsAuthenticated &&
            ShopIdentityTypes.IsInternalUser(identity) &&
            requirement.Roles.Any(role => identity.HasClaim(identity.RoleClaimType, role)));

        if (allowed) context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

public static class ShopAuthentication {
    public const string LegacySharedKeyId = "gba-legacy-shared";

    public static IServiceCollection AddShopAuthentication(
        this IServiceCollection services,
        SecuritySettings security,
        GbaServerAuthSettings gbaServer) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(security);
        ArgumentNullException.ThrowIfNull(gbaServer);

        TokenValidationParameters customerParameters = BuildCustomerValidationParameters(security, gbaServer);
        TokenValidationParameters? gbaInternalParameters = BuildGbaInternalValidationParameters(gbaServer);

        var authentication = services.AddAuthentication(options => {
            options.DefaultScheme = ShopAuthenticationSchemes.Customer;
            options.DefaultAuthenticateScheme = ShopAuthenticationSchemes.Customer;
            options.DefaultChallengeScheme = ShopAuthenticationSchemes.Customer;
        }).AddJwtBearer(ShopAuthenticationSchemes.Customer, options => {
            options.TokenValidationParameters = customerParameters;
            options.Events = new JwtBearerEvents {
                OnTokenValidated = context => {
                    if (IsLegacySharedToken(context.SecurityToken) &&
                        !ShopIdentityTypes.IsCustomer(context.Principal?.Identity as ClaimsIdentity)) {
                        context.Fail("Tokens signed with the former shared key are accepted only for shop customers.");
                    }

                    return Task.CompletedTask;
                }
            };
        });

        List<string> administrativeSchemes = new() { ShopAuthenticationSchemes.Customer };
        if (gbaInternalParameters != null) {
            authentication.AddJwtBearer(ShopAuthenticationSchemes.GbaInternal, options => {
                options.TokenValidationParameters = gbaInternalParameters;
                options.Events = new JwtBearerEvents {
                    OnTokenValidated = context => {
                        if (!ShopIdentityTypes.IsInternalUser(context.Principal?.Identity as ClaimsIdentity)) {
                            context.Fail("Only internal gba-server users are accepted by this scheme.");
                        }

                        return Task.CompletedTask;
                    }
                };
            });
            administrativeSchemes.Add(ShopAuthenticationSchemes.GbaInternal);
        }

        services.AddAuthorization(options => {
            options.DefaultPolicy = new AuthorizationPolicyBuilder(ShopAuthenticationSchemes.Customer)
                .RequireAuthenticatedUser()
                .Build();
            options.AddPolicy(
                DefaultPolicies.ShopAdministrativePolicy,
                BuildAdministrativePolicy(administrativeSchemes, DefaultPolicies.AdministrativeRolesPolicy.Split(',')));
            options.AddPolicy(
                DefaultPolicies.ShopAdministratorPolicy,
                BuildAdministrativePolicy(administrativeSchemes, new[] { "Administrator" }));
        });

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IAuthorizationHandler, InternalAdministratorAuthorizationHandler>());

        return services;
    }

    public static AuthorizationPolicy BuildAdministrativePolicy(IEnumerable<string> schemes, IEnumerable<string> roles) =>
        new AuthorizationPolicyBuilder(schemes.ToArray())
            .RequireAuthenticatedUser()
            .AddRequirements(new InternalAdministratorRequirement(roles))
            .Build();

    public static TokenValidationParameters BuildCustomerValidationParameters(
        SecuritySettings security,
        GbaServerAuthSettings gbaServer) {
        List<SecurityKey> keys = new() { CreateKey(security.JwtKey, null) };
        List<string> issuers = new() { security.JwtIssuer.Trim() };
        List<string> audiences = new() { security.JwtAudience.Trim() };

        if (security.AcceptLegacySharedTokens) {
            if (!gbaServer.IsConfigured)
                throw new InvalidOperationException(
                    $"Security:AcceptLegacySharedTokens requires {GbaServerAuthSettings.SectionName} to name the former shared key");

            keys.Add(CreateKey(gbaServer.JwtKey, LegacySharedKeyId));
            issuers.Add(gbaServer.JwtIssuer);
            audiences.Add(gbaServer.JwtAudience);
        }

        return CreateParameters(keys, issuers, audiences, ShopAuthenticationSchemes.CustomerAuthenticationType);
    }

    public static TokenValidationParameters? BuildGbaInternalValidationParameters(GbaServerAuthSettings gbaServer) {
        if (!gbaServer.IsConfigured) return null;

        return CreateParameters(
            new List<SecurityKey> { CreateKey(gbaServer.JwtKey, null) },
            new List<string> { gbaServer.JwtIssuer },
            new List<string> { gbaServer.JwtAudience },
            ShopAuthenticationSchemes.GbaInternal);
    }

    public static bool IsLegacySharedToken(SecurityToken? token) =>
        string.Equals(token?.SigningKey?.KeyId, LegacySharedKeyId, StringComparison.Ordinal);

    private static SymmetricSecurityKey CreateKey(string value, string? keyId) {
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(value.Trim()));
        if (keyId != null) key.KeyId = keyId;
        return key;
    }

    private static TokenValidationParameters CreateParameters(
        List<SecurityKey> keys,
        List<string> issuers,
        List<string> audiences,
        string authenticationType) =>
        new() {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = keys,
            TryAllIssuerSigningKeys = true,
            ValidateIssuer = true,
            ValidIssuers = issuers.Distinct(StringComparer.Ordinal).ToArray(),
            ValidateAudience = true,
            ValidAudiences = audiences.Distinct(StringComparer.Ordinal).ToArray(),
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
            ClockSkew = TimeSpan.Zero,
            AuthenticationType = authenticationType
        };
}
