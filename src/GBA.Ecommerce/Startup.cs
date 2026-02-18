using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using GBA.Common.Configuration;
using GBA.Search.Extensions;
using Microsoft.Data.SqlClient;
using ConfigurationManager = GBA.Common.Helpers.ConfigurationManager;

namespace GBA.Ecommerce;

public class Startup {
    private const string _corsPolicy = "CorsPolicy";

    private readonly IWebHostEnvironment _environment;

    public Startup(IWebHostEnvironment env) {
        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(env.ContentRootPath)
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables();

        Configuration = builder.Build();

        _environment = env;

        NoltFolderManager.InitializeFolderManager(env.ContentRootPath);

        ConfigurationManager.SetAppSettingsProperties(Configuration);
        ConfigurationManager.SetAppEnvironmentRootPath(env.ContentRootPath);

        SecuritySettings securitySettings = Configuration.GetSection("Security").Get<SecuritySettings>();
        SecuritySettings.Initialize(securitySettings);

#if DEBUG
        env.EnvironmentName = ProductEnvironment.Development;
#else
            env.EnvironmentName = ProductEnvironment.Production;
#endif
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
            options.Level = CompressionLevel.Optimal;
        });

        services.AddMemoryCache();
        services.AddHttpClient();
        services.AddRequestDecompression();
        services.AddHealthChecks();
        services.AddRateLimiter(options => {
            options.RejectionStatusCode = 429;

            options.AddFixedWindowLimiter("auth", opt => {
                opt.PermitLimit = 5;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 2;
            });

            options.AddFixedWindowLimiter("search", opt => {
                opt.PermitLimit = 30;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 5;
            });

            options.AddFixedWindowLimiter("api", opt => {
                opt.PermitLimit = 100;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 10;
            });
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
            options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromSeconds(60)));
            options.AddPolicy("Products", builder => builder.Expire(TimeSpan.FromMinutes(5)).Tag("products"));
            options.AddPolicy("Regions", builder => builder.Expire(TimeSpan.FromHours(1)).Tag("regions"));
            options.AddPolicy("Brands", builder => builder.Expire(TimeSpan.FromHours(2)).Tag("brands"));
            options.AddPolicy("Static", builder => builder.Expire(TimeSpan.FromHours(24)).Tag("static"));
        });

        services.AddCors(options => {
            options.AddPolicy(_corsPolicy, builder => builder
                .WithOrigins("http://localhost:3000", "http://localhost:7000", "http://78.152.175.67:15026", "http://new.concord-shop.com", "https://new.concord-shop.com")
                .AllowAnyMethod().AllowAnyHeader()
                .AllowCredentials());
        });

        services.AddIdentity<UserIdentity, IdentityRole>(options => {
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 7;
                options.Password.RequireDigit = false;
                options.Password.RequiredUniqueChars = 0;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
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
        }).AddNewtonsoftJson(options => {
            options.SerializerSettings.ContractResolver = new DefaultContractResolver();
            options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            options.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Include;
            options.SerializerSettings.DateParseHandling = DateParseHandling.None;
            options.SerializerSettings.FloatParseHandling = FloatParseHandling.Double;
            options.SerializerSettings.MetadataPropertyHandling = MetadataPropertyHandling.Ignore;
            options.SerializerSettings.TypeNameHandling = TypeNameHandling.None;
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
        services.AddScoped<IProductCoPurchaseRecommendationsService, ProductCoPurchaseRecommendationsService>();
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

        // Product search with Typesense (with SQL fallback)
        services.AddProductSearch(
            Configuration,
            () => new SqlConnection(ConfigurationManager.LocalDatabaseConnectionString));

        // Elasticsearch search (V4)
        services.AddElasticsearchSearch(
            Configuration,
            () => new SqlConnection(ConfigurationManager.LocalDatabaseConnectionString));
    }

    public void Configure(
        IApplicationBuilder app,
        ILoggerFactory loggerFactory,
        IGlobalExceptionFactory globalExceptionFactory) {

        app.UseResponseCompression();
        app.UseRequestDecompression();

        if (!_environment.IsDevelopment()) {
            app.UseHsts();
        }

        app.UseRouting();
        app.UseHttpsRedirection();
        app.UseResponseCaching();
        app.UseOutputCache();
        app.UseRateLimiter();
        app.UseDefaultFiles();
        app.UseStaticFiles(new StaticFileOptions {
            RequestPath = "/documents",
            FileProvider = new PhysicalFileProvider(Path.Combine(_environment.ContentRootPath, "Documents")),
            ServeUnknownFileTypes = true,
            OnPrepareResponse = ctx => {
                ctx.Context.Response.Headers[HeaderNames.CacheControl] = "public,max-age=604800";
            }
        });

        ConfigureRequestLocalization(app);

        app.UseCors(_corsPolicy);
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<UserNetIdMiddleware>();

        app.UseExceptionHandler(builder => {
            builder.Run(async context => {
                IExceptionHandlerFeature error = context.Features.Get<IExceptionHandlerFeature>();
                IGlobalExceptionHandler globalExceptionHandler = globalExceptionFactory.New();

                await globalExceptionHandler.HandleException(context, error, _environment.IsDevelopment());
            });
        });

        app.UseMiddleware<ReflectionTypeLoadExceptionLoggingMiddleware>();
        app.UseSwagger();
        app.UseSwaggerUI(options => {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            options.RoutePrefix = string.Empty;
        });

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
            });
        });
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
            });
#else
            options.UseSqlServer(Configuration.GetConnectionString(ConnectionStringNames.RemoteIdentity), sqlOptions => {
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(30);
            });
#endif
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
            ValidAudience = AuthOptions.AUDIENCE_LOCAL,
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

