using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text.Json;
using GBA.Common;
using GBA.Common.Cultures;
using GBA.Common.Exceptions.GlobalHandler;
using GBA.Common.Exceptions.GlobalHandler.Contracts;
using GBA.Common.Helpers;
using GBA.Common.IdentityConfiguration;
using GBA.Common.Middleware;
using GBA.Common.ResponseBuilder;
using GBA.Common.ResponseBuilder.Contracts;
using GBA.Data;
using GBA.Domain.DataSourceAdapters.SQL;
using GBA.Domain.DataSourceAdapters.SQL.Contracts;
using GBA.Domain.DbConnectionFactory;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.IdentityEntities;
using GBA.Domain.Repositories.Accounting;
using GBA.Domain.Repositories.Accounting.Contracts;
using GBA.Domain.Repositories.Agreements;
using GBA.Domain.Repositories.Agreements.Contracts;
using GBA.Domain.Repositories.Clients;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Currencies;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.Delivery;
using GBA.Domain.Repositories.Delivery.Contracts;
using GBA.Domain.Repositories.Ecommerce;
using GBA.Domain.Repositories.Ecommerce.Contracts;
using GBA.Domain.Repositories.ExchangeRates;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Identities;
using GBA.Domain.Repositories.Identities.Contracts;
using GBA.Domain.Repositories.Organizations;
using GBA.Domain.Repositories.Organizations.Contracts;
using GBA.Domain.Repositories.PaymentOrders;
using GBA.Domain.Repositories.PaymentOrders.Contracts;
using GBA.Domain.Repositories.Pricings;
using GBA.Domain.Repositories.Pricings.Contracts;
using GBA.Domain.Repositories.Products;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Regions;
using GBA.Domain.Repositories.Regions.Contracts;
using GBA.Domain.Repositories.ReSales;
using GBA.Domain.Repositories.ReSales.Contracts;
using GBA.Domain.Repositories.Sales;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Storages;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Domain.Repositories.Transporters;
using GBA.Domain.Repositories.Transporters.Contracts;
using GBA.Domain.Repositories.UserRoles;
using GBA.Domain.Repositories.UserRoles.Contracts;
using GBA.Domain.Repositories.Users;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Services.Services.Clients;
using GBA.Services.Services.Clients.Contracts;
using GBA.Services.Services.DeliveryRecipients;
using GBA.Services.Services.DeliveryRecipients.Contracts;
using GBA.Services.Services.Ecommerce;
using GBA.Services.Services.Ecommerce.Contracts;
using GBA.Services.Services.EcommerceRegions;
using GBA.Services.Services.EcommerceRegions.Contracts;
using GBA.Services.Services.ExchangeRates;
using GBA.Services.Services.ExchangeRates.Contracts;
using GBA.Services.Services.GeoLocations;
using GBA.Services.Services.GeoLocations.Contracts;
using GBA.Services.Services.Messengers;
using GBA.Services.Services.Messengers.Contracts;
using GBA.Services.Services.Offers;
using GBA.Services.Services.Offers.Contracts;
using GBA.Services.Services.Orders;
using GBA.Services.Services.Orders.Contracts;
using GBA.Services.Services.Products;
using GBA.Services.Services.Products.Contracts;
using GBA.Services.Services.Recommendations;
using GBA.Services.Services.Recommendations.Contracts;
using GBA.Services.Services.Regions;
using GBA.Services.Services.Regions.Contracts;
using GBA.Services.Services.Transporters;
using GBA.Services.Services.Transporters.Contracts;
using GBA.Services.Services.UserManagement;
using GBA.Services.Services.UserManagement.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.Text.Json.Serialization;
using GBA.Common.Configuration;
using GBA.Search.Extensions;
using Microsoft.Data.SqlClient;
using ConfigurationManager = GBA.Common.Helpers.ConfigurationManager;

namespace GBA.Ecommerce;

public class Startup {
    private const string _corsPolicy = "CorsPolicy";
    private const int _maxCachedSearchLimit = 100;
    private const int _maxCachedSearchOffset = 5000;
    private const int _maxCachedSearchTermLength = 128;

    private readonly IWebHostEnvironment _environment;

    public Startup(IWebHostEnvironment env) {
        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(env.ContentRootPath)
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .AddKeyPerFile("/run/secrets", optional: true);

        Configuration = builder.Build();

        _environment = env;

        NoltFolderManager.InitializeFolderManager(env.ContentRootPath);

        ConfigurationManager.SetAppSettingsProperties(Configuration);
        ConfigurationManager.SetAppEnvironmentRootPath(env.ContentRootPath);

        SecuritySettings securitySettings = Configuration.GetSection("Security").Get<SecuritySettings>() ?? new SecuritySettings();
        SecuritySettings.Initialize(securitySettings);
    }

    public IConfiguration Configuration { get; set; }

    public void ConfigureServices(IServiceCollection services) {
        ConfigureDbContext(services);

        services.AddResponseCompression(options => {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat([
                "application/json",
                "application/javascript",
                "text/css",
                "text/html",
                "text/json",
                "text/plain",
                "text/xml",
                "image/svg+xml"
            ]);
        });

        services.Configure<BrotliCompressionProviderOptions>(options => {
            options.Level = CompressionLevel.Optimal;
        });

        services.Configure<GzipCompressionProviderOptions>(options => {
            options.Level = CompressionLevel.Fastest;
        });

        services.AddMemoryCache();
        services.AddHttpClient();
        services.AddRequestDecompression();
        services.AddHealthChecks()
            .AddCheck("db-main", () => {
                try {
                    using SqlConnection conn = new SqlConnection(Configuration.GetConnectionString(
#if DEBUG
                        ConnectionStringNames.Local
#else
                        ConnectionStringNames.Remote
#endif
                    ));
                    conn.Open();
                    return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy();
                } catch (Exception ex) {
                    return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(ex.Message);
                }
            });
        services.AddRateLimiter(options => {
            options.RejectionStatusCode = 429;
            options.OnRejected = async (context, cancellationToken) => {
                context.HttpContext.Response.ContentType = "application/json";
                context.HttpContext.Response.Headers[HeaderNames.CacheControl] = "no-store";
                await context.HttpContext.Response.WriteAsync(
                    JsonSerializer.Serialize(new {
                        statusCode = StatusCodes.Status429TooManyRequests,
                        message = "Too many requests"
                    }),
                    cancellationToken);
            };

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    GetClientPartitionKey(context),
                    _ => new SlidingWindowRateLimiterOptions {
                        PermitLimit = 1200,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            options.AddPolicy("auth", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetClientPartitionKey(context),
                    _ => new FixedWindowRateLimiterOptions {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            options.AddPolicy("search", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    GetClientPartitionKey(context),
                    _ => new SlidingWindowRateLimiterOptions {
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            options.AddPolicy("api", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    GetClientPartitionKey(context),
                    _ => new SlidingWindowRateLimiterOptions {
                        PermitLimit = 600,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));
        });

        services.AddHsts(options => {
            options.MaxAge = TimeSpan.FromDays(365);
            options.IncludeSubDomains = true;
            options.Preload = true;
        });

        services.AddResponseCaching(options => {
            options.MaximumBodySize = 67108864;
            options.UseCaseSensitivePaths = false;
        });

        services.AddOutputCache(options => {
            options.SizeLimit = 256 * 1024 * 1024;
            options.MaximumBodySize = 4 * 1024 * 1024;
            options.UseCaseSensitivePaths = false;

            options.AddPolicy("AnonymousProductSearch", builder => builder
                .With(IsAnonymousGetRequest)
                .With(IsCacheableSearchQuery)
                .Expire(TimeSpan.FromMinutes(2))
                .SetVaryByRouteValue("culture")
                .SetVaryByQuery("value", "query", "limit", "offset", "withVat")
                .Tag("products")
                .SetLocking(true));

            options.AddPolicy("Regions", builder => builder
                .With(IsAnonymousGetRequest)
                .Expire(TimeSpan.FromHours(1))
                .SetVaryByRouteValue("culture")
                .SetVaryByQuery("netId")
                .Tag("regions")
                .SetLocking(true));

            options.AddPolicy("Brands", builder => builder
                .With(IsAnonymousGetRequest)
                .Expire(TimeSpan.FromHours(2))
                .SetVaryByRouteValue("culture")
                .Tag("brands")
                .SetLocking(true));

            options.AddPolicy("LookupShort", builder => builder
                .With(IsAnonymousGetRequest)
                .Expire(TimeSpan.FromMinutes(10))
                .SetVaryByRouteValue("culture")
                .SetVaryByQuery("netId")
                .Tag("lookups")
                .SetLocking(true));

            options.AddPolicy("ExchangeRates", builder => builder
                .With(IsAnonymousGetRequest)
                .Expire(TimeSpan.FromMinutes(10))
                .SetVaryByRouteValue("culture")
                .Tag("exchange-rates")
                .SetLocking(true));

            options.AddPolicy("Static", builder => builder
                .With(IsAnonymousGetRequest)
                .Expire(TimeSpan.FromHours(24))
                .SetVaryByRouteValue("culture")
                .SetVaryByQuery("locale")
                .Tag("static")
                .SetLocking(true));
        });

        services.AddCors(options => {
            options.AddPolicy(_corsPolicy, builder => builder
                .WithOrigins(SecuritySettings.Instance.CorsOrigins)
                .AllowAnyMethod().AllowAnyHeader()
                .AllowCredentials());
        });

        services.AddIdentity<UserIdentity, IdentityRole>(options => {
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = true;
                options.Password.RequiredUniqueChars = 4;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.User.RequireUniqueEmail = true;
            }).AddEntityFrameworkStores<ConcordIdentityContext>()
            .AddDefaultTokenProviders();

        ConfigureJwtAuthService(services);

        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.AddSwaggerGen();
        services.AddMvc(options => {
            options.CacheProfiles.Add(
                CacheControlProfiles.Default,
                new CacheProfile {
                    Duration = 60
                });
            options.CacheProfiles.Add(
                CacheControlProfiles.TwoHours,
                new CacheProfile {
                    Duration = 7200
                });
            options.CacheProfiles.Add(
                CacheControlProfiles.HalfDay,
                new CacheProfile {
                    Duration = 43200
                });
        }).AddJsonOptions(options => {
            options.JsonSerializerOptions.PropertyNamingPolicy = null;
            options.JsonSerializerOptions.DictionaryKeyPolicy = null;
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        }).AddDataAnnotationsLocalization();

        services.AddScoped<IResponseFactory, ResponseFactory>();
        services.AddScoped<IGlobalExceptionHandler, GlobalExceptionHandler>();
        services.AddScoped<IGlobalExceptionFactory, GlobalExceptionFactory>();

        services.AddScoped<IIdentityRepository, IdentityRepository>();
        services.AddScoped<IIdentityRolesRepository, IdentityRolesRepository>();
        services.AddScoped<IIdentityRepositoriesFactory, IdentityRepositoriesFactory>();
        services.AddScoped<IAgreementRepositoriesFactory, AgreementRepositoriesFactory>();
        services.AddScoped<IOrganizationRepositoriesFactory, OrganizationRepositoriesFactory>();
        services.AddScoped<ICurrencyRepositoriesFactory, CurrencyRepositoriesFactory>();
        services.AddScoped<IPricingRepositoriesFactory, PricingRepositoriesFactory>();
        services.AddScoped<IExchangeRateRepositoriesFactory, ExchangeRateRepositoriesFactory>();
        services.AddScoped<IUserRoleRepositoriesFactory, UserRoleRepositoriesFactory>();
        services.AddScoped<IUserRepositoriesFactory, UserRepositoriesFactory>();
        services.AddScoped<IClientRepositoriesFactory, ClientRepositoriesFactory>();
        services.AddScoped<IRegionRepositoriesFactory, RegionRepositoriesFactory>();
        services.AddScoped<IProductRepositoriesFactory, ProductRepositoriesFactory>();
        services.AddScoped<ISaleRepositoriesFactory, SaleRepositoriesFactory>();
        services.AddScoped<ITransporterRepositoriesFactory, TransporterRepositoriesFactory>();
        services.AddScoped<IDeliveryRepositoriesFactory, DeliveryRepositoriesFactory>();
        services.AddScoped<IProductOneCRepositoriesFactory, ProductOneCRepositoriesFactory>();
        services.AddScoped<IEcommerceAdminPanelRepositoriesFactory, EcommerceAdminPanelRepositoriesFactory>();
        services.AddScoped<IPaymentOrderRepositoriesFactory, PaymentOrderRepositoriesFactory>();
        services.AddScoped<IStorageRepositoryFactory, StorageRepositoryFactory>();
        services.AddScoped<IAccountingRepositoriesFactory, AccountingRepositoriesFactory>();
        services.AddScoped<IReSaleRepositoriesFactory, ReSaleRepositoriesFactory>();
        services.AddScoped<IRetailClientRepositoriesFactory, RetailClientRepositoriesFactory>();

        services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();

        services.AddScoped<IRequestTokenService, RequestTokenService>();
        services.AddScoped<ISignUpService, SignUpService>();
        services.AddScoped<IEmailAvailabilityService, EmailAvailabilityService>();
        services.AddScoped<IEmailValidationService, EmailValidationService>();
        services.AddScoped<IClientRegistrationTaskService, ClientRegistrationTaskService>();
        services.AddScoped<IClientShoppingCartService, ClientShoppingCartService>();
        services.AddHostedService<GBA.Ecommerce.Background.ExpiredCartCleanupBackgroundService>();
        services.AddSingleton<GBA.Common.Search.ISearchReindexSignal, GBA.Common.Search.SearchReindexSignal>();
        services.AddHostedService<GBA.Ecommerce.Background.ProductReindexBackgroundService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IClientAgreementService, ClientAgreementService>();
        services.AddScoped<IRegionService, RegionService>();
        services.AddScoped<IRegionCodeService, RegionCodeService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<IExchageRateService, ExchageRateService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IGeoLocationService, GeoLocationService>();
        services.AddScoped<ITransporterService, TransporterService>();
        services.AddScoped<IOfferService, OfferService>();
        services.AddScoped<IPreOrderService, PreOrderService>();
        services.AddScoped<IDeliveryRecipientService, DeliveryRecipientService>();
        services.AddScoped<IProductMostPurchasedService, ProductMostPurchasedService>();
        services.AddScoped<ICarBrandService, CarBrandService>();
        services.AddScoped<ISeoPageService, SeoPageService>();
        services.AddScoped<IEcommerceRegionService, EcommerceRegionService>();
        services.AddScoped<IAgreementService, AgreementService>();
        services.AddScoped<IAccountingCashFlowService, AccountingCashFlowService>();
        services.AddScoped<IPaymentLinkService, PaymentLinkService>();

        // Price caching for logged-in users
        services.AddSingleton<IPriceCacheService, PriceCacheService>();

        services.AddScoped<ISqlContextFactory, SqlContextFactory>();
        services.AddScoped<ISqlDbContext>(t => new SqlDbContext(t.GetRequiredService<ConcordContext>()));

        // Elasticsearch search
        services.AddElasticsearchSearch(
            Configuration,
            () => new SqlConnection(ConfigurationManager.LocalDatabaseConnectionString));
    }

    public void Configure(
        IApplicationBuilder app,
        ILoggerFactory loggerFactory,
        IGlobalExceptionFactory globalExceptionFactory) {

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();

        app.UseResponseCompression();
        app.UseRequestDecompression();

        if (!_environment.IsDevelopment()) {
            app.UseHsts();
        }

        app.Use(async (context, next) => {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

            // Strict API-only CSP to avoid impacting static pages.
            if (context.Request.Path.StartsWithSegments("/api")) {
                context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
            }

            context.Response.Headers.Remove("Server");
            await next();
        });

        app.UseHttpsRedirection();
        app.UseRouting();
        ConfigureRequestLocalization(app);
        app.UseCors(_corsPolicy);
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<UserNetIdMiddleware>();
        app.UseResponseCaching();
        app.UseOutputCache();
        app.UseDefaultFiles();
        app.UseStaticFiles(new StaticFileOptions {
            RequestPath = "/documents",
            FileProvider = new PhysicalFileProvider(Path.Combine(_environment.ContentRootPath, "Documents")),
            ServeUnknownFileTypes = true,
            OnPrepareResponse = ctx => {
                ctx.Context.Response.Headers[HeaderNames.CacheControl] = "public,max-age=604800";
            }
        });

        app.UseExceptionHandler(builder => {
            builder.Run(async context => {
                IExceptionHandlerFeature? error = context.Features.Get<IExceptionHandlerFeature>();
                if (error?.Error == null) {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    return;
                }
                IGlobalExceptionHandler globalExceptionHandler = globalExceptionFactory.New();

                await globalExceptionHandler.HandleException(context, error, _environment.IsDevelopment());
            });
        });

        app.UseMiddleware<ReflectionTypeLoadExceptionLoggingMiddleware>();

        if (_environment.IsDevelopment()) {
            app.UseSwagger();
            app.UseSwaggerUI(options => {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
                options.RoutePrefix = string.Empty;
            });
        }

        app.UseEndpoints(endpoints => {
            endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}");
            endpoints.MapHealthChecks("/health", new HealthCheckOptions {
                ResponseWriter = async (context, report) => {
                    context.Response.ContentType = "application/json";
                    string result = System.Text.Json.JsonSerializer.Serialize(new {
                        status = report.Status.ToString(),
                        checks = report.Entries.Select(e => new {
                            name = e.Key,
                            status = e.Value.Status.ToString(),
                            duration = e.Value.Duration.TotalMilliseconds
                        })
                    });
                    await context.Response.WriteAsync(result);
                }
            }).DisableRateLimiting();
        });
    }

    private static bool IsAnonymousGetRequest(OutputCacheContext context) {
        HttpRequest request = context.HttpContext.Request;
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method)) return false;

        if (context.HttpContext.User.Identity?.IsAuthenticated == true) return false;

        return !request.Headers.ContainsKey(HeaderNames.Authorization)
               && !request.Headers.ContainsKey(HeaderNames.Cookie);
    }

    private static bool IsCacheableSearchQuery(OutputCacheContext context) {
        IQueryCollection query = context.HttpContext.Request.Query;

        string? term = query.TryGetValue("value", out var value)
            ? value.FirstOrDefault()
            : query.TryGetValue("query", out var searchQuery)
                ? searchQuery.FirstOrDefault()
                : null;

        return !string.IsNullOrWhiteSpace(term)
               && term.Length <= _maxCachedSearchTermLength
               && QueryIntAtMost(query, "limit", _maxCachedSearchLimit)
               && QueryIntAtMost(query, "offset", _maxCachedSearchOffset);
    }

    private static bool QueryIntAtMost(IQueryCollection query, string key, int maxValue) {
        if (!query.TryGetValue(key, out var values)) return true;

        string? value = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(value)) return true;

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
               && parsed >= 0
               && parsed <= maxValue;
    }

    private static string GetClientPartitionKey(HttpContext context) {
        IPAddress? remoteIp = context.Connection.RemoteIpAddress;

        if (IsPrivateOrLoopback(remoteIp)) {
            string? forwardedIp = GetFirstHeaderIp(context, "CF-Connecting-IP")
                ?? GetFirstHeaderIp(context, "X-Forwarded-For")
                ?? GetFirstHeaderIp(context, "X-Real-IP");

            if (!string.IsNullOrWhiteSpace(forwardedIp)) {
                return forwardedIp;
            }
        }

        return remoteIp?.ToString() ?? "unknown";
    }

    private static string? GetFirstHeaderIp(HttpContext context, string headerName) {
        string? value = context.Request.Headers[headerName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(value)) return null;

        string first = value.Split(',')[0].Trim();
        return IPAddress.TryParse(first, out IPAddress? ipAddress) ? ipAddress.ToString() : null;
    }

    private static bool IsPrivateOrLoopback(IPAddress? address) {
        if (address is null) return false;
        if (IPAddress.IsLoopback(address)) return true;

        if (address.IsIPv4MappedToIPv6) {
            address = address.MapToIPv4();
        }

        byte[] bytes = address.GetAddressBytes();
        return address.AddressFamily switch {
            System.Net.Sockets.AddressFamily.InterNetwork =>
                bytes[0] == 10 ||
                bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31 ||
                bytes[0] == 192 && bytes[1] == 168,
            System.Net.Sockets.AddressFamily.InterNetworkV6 =>
                bytes[0] == 0xfd || bytes[0] == 0xfc,
            _ => false
        };
    }

    private void ConfigureRequestLocalization(IApplicationBuilder app) {
        RequestLocalizationOptions localizationOptions = new() {
            SupportedCultures = new List<CultureInfo> { new("uk") },
            SupportedUICultures = new List<CultureInfo> { new("uk") }
        };

        localizationOptions.DefaultRequestCulture = new RequestCulture("uk");
        localizationOptions.RequestCultureProviders.Insert(0, new UrlCultureProvider(localizationOptions.DefaultRequestCulture));
        app.UseRequestLocalization(localizationOptions);
    }

    private void ConfigureDbContext(IServiceCollection services) {
        services.AddDbContextPool<ConcordContext>(options => {
#if DEBUG
            options.UseSqlServer(Configuration.GetConnectionString(ConnectionStringNames.Local), sqlOptions => {
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(30);
                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                sqlOptions.MinBatchSize(5);
                sqlOptions.MaxBatchSize(100);
            });
#else
            options.UseSqlServer(Configuration.GetConnectionString(ConnectionStringNames.Remote), sqlOptions => {
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(30);
                // Performance optimizations
                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                sqlOptions.MinBatchSize(5);
                sqlOptions.MaxBatchSize(100);
            });
#endif
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        }, poolSize: 256);

        services.AddDbContextPool<ConcordIdentityContext>(options => {
#if DEBUG
            options.UseSqlServer(Configuration.GetConnectionString(ConnectionStringNames.LocalIdentity), sqlOptions => {
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(30);
                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                sqlOptions.MinBatchSize(1);
                sqlOptions.MaxBatchSize(50);
            });
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();
#else
            options.UseSqlServer(Configuration.GetConnectionString(ConnectionStringNames.RemoteIdentity), sqlOptions => {
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(30);
                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                sqlOptions.MinBatchSize(1);
                sqlOptions.MaxBatchSize(50);
            });
#endif
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        }, poolSize: 64);
    }

    private static void ConfigureJwtAuthService(IServiceCollection services) {
        SymmetricSecurityKey signingKey = AuthOptions.GetSymmetricSecurityKey();

        TokenValidationParameters tokenValidationParameters = new() {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateIssuer = true,
            ValidIssuer = AuthOptions.ISSUER,
            ValidateAudience = true,
            ValidAudience = AuthOptions.AUDIENCE,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        services.AddAuthentication(options => {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(o => {
            o.TokenValidationParameters = tokenValidationParameters;
        });
    }
}
