using System.Text;
using System.Threading.Channels;
using System.Threading.RateLimiting;
using Asp.Versioning;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.IdentityModel.Tokens;
using TmsApi.Api.ExceptionHandlers;
using TmsApi.Api.Filters;
using TmsApi.Api.Options;
using TmsApi.Api.RateLimiting;
using TmsApi.Api.Workers;
using TmsApi.Application.Behaviors;
using TmsApi.Application.Enrollments.Commands;
using TmsApi.Application.Interfaces;
using TmsApi.Application.Notifications;
using TmsApi.Application.Transcripts;
using TmsApi.Infrastructure.Identity;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Services;
using TmsApi.Infrastructure.Transcripts;
using TmsApi.Infrastructure.Workers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using TmsApi.Api.Authorization;
using TmsApi.Api.Notifications;
namespace TmsApi.Api.Extensions;


public static class ServiceCollectionExtensions
{
  // ── Authentication & Identity ─────────────────────────────────────
  public static IServiceCollection AddTmsAuthentication(
      this IServiceCollection services,
      IConfiguration config)
  {
    services.AddScoped<TokenService>();

    services
        .AddAuthentication(options =>
        {
          options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
          options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
          options.TokenValidationParameters = new TokenValidationParameters
          {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = config["Jwt:Issuer"],
            ValidAudience = config["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                      Encoding.UTF8.GetBytes(config["Jwt:Key"]!))
          };
        });

    services.AddIdentityCore<TmsUser>(options =>
    {
      options.Password.RequiredLength = 12;
      options.Password.RequireUppercase = true;
      options.Password.RequireDigit = true;
      options.Password.RequireNonAlphanumeric = true;
      options.Lockout.MaxFailedAccessAttempts = 5;
      options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
      options.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<TmsDbContext>();

    services.AddAuthorizationBuilder()
     .AddPolicy("CanEditCourse", policy =>
         policy.Requirements.Add(new CourseInstructorRequirement()));

    services.AddAntiforgery(options =>
    {
      options.HeaderName = "X-XSRF-TOKEN";
    });

    return services;
  }

  // ── CORS ──────────────────────────────────────────────────────────
  public static IServiceCollection AddTmsCors(
      this IServiceCollection services,
      IConfiguration config)
  {
    var allowedOrigins = config
        .GetSection("AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:4200"];

    services.AddCors(options =>
    {
      options.AddPolicy("TmsClient", policy =>
          {
          policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
        });
    });

    return services;
  }

  // ── Controllers ───────────────────────────────────────────────────
  public static IServiceCollection AddTmsControllers(
      this IServiceCollection services)
  {
    services.AddControllers(options =>
    {
      options.Filters.Add<AuditLogFilter>();
    })
    .AddJsonOptions(options =>
    {
      options.JsonSerializerOptions.Converters.Add(
              new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

    return services;
  }

  // ── API Versioning ────────────────────────────────────────────────
  public static IServiceCollection AddTmsApiVersioning(
      this IServiceCollection services)
  {
    services.AddApiVersioning(options =>
    {
      options.DefaultApiVersion = new ApiVersion(1, 0);
      options.AssumeDefaultVersionWhenUnspecified = true;
      options.ReportApiVersions = true;
      options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(options =>
    {
      options.GroupNameFormat = "'v'VVV";
      options.SubstituteApiVersionInUrl = true;
    });

    return services;
  }

  // ── Rate Limiting ─────────────────────────────────────────────────
  public static IServiceCollection AddTmsRateLimiting(
      this IServiceCollection services)
  {
    services.AddRateLimiter(options =>
    {
      options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
              httpContext =>
              {
                var (partitionKey, tier) = ApiKeyResolver.Resolve(httpContext);

                return tier switch
                {
                  ApiKeyTier.Paid => RateLimitPartition.GetTokenBucketLimiter(
                          partitionKey: $"paid:{partitionKey}",
                          factory: _ => new TokenBucketRateLimiterOptions
                          {
                            TokenLimit = 200,
                            TokensPerPeriod = 100,
                            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                            QueueLimit = 0,
                            AutoReplenishment = true
                          }),

                  ApiKeyTier.Free => RateLimitPartition.GetTokenBucketLimiter(
                          partitionKey: $"free:{partitionKey}",
                          factory: _ => new TokenBucketRateLimiterOptions
                          {
                            TokenLimit = 30,
                            TokensPerPeriod = 10,
                            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                            QueueLimit = 0,
                            AutoReplenishment = true
                          }),

                  _ => RateLimitPartition.GetTokenBucketLimiter(
                          partitionKey: $"anon:{partitionKey}",
                          factory: _ => new TokenBucketRateLimiterOptions
                          {
                            TokenLimit = 10,
                            TokensPerPeriod = 5,
                            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                            QueueLimit = 0,
                            AutoReplenishment = true
                          })
                };
              });

      options.AddFixedWindowLimiter("AuthLimiter", opt =>
       {
         opt.PermitLimit = 5;                      // 5 requests
         opt.Window = TimeSpan.FromMinutes(1);      // Per 1 minute
         opt.QueueLimit = 0;                        // No queuing
       });


      options.AddConcurrencyLimiter("transcripts", opt =>
          {
          opt.PermitLimit = 5;
          opt.QueueLimit = 20;
          opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        });

      options.AddTokenBucketLimiter("search", opt =>
          {
          opt.TokenLimit = 10;
          opt.TokensPerPeriod = 5;
          opt.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
          opt.QueueLimit = 2;
        });

      options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

      options.OnRejected = async (context, ct) =>
          {
          var retryAfter = "10";
          if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var ts))
            retryAfter = ((int)ts.TotalSeconds).ToString();

          context.HttpContext.Response.Headers.RetryAfter = retryAfter;
          context.HttpContext.Response.ContentType = "application/problem+json";

          await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
          {
            Title = "Rate limit exceeded",
            Detail = $"Too many requests. Retry after {retryAfter} seconds.",
            Status = StatusCodes.Status429TooManyRequests,
            Type = "https://tms.local/errors/rate_limit_exceeded"
          }, ct);
        };
    });

    return services;
  }

  // ── MediatR + FluentValidation ────────────────────────────────────
  public static IServiceCollection AddTmsMediator(
      this IServiceCollection services)
  {
    services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(EnrollStudentHandler).Assembly));

    services.AddValidatorsFromAssembly(typeof(EnrollStudentValidator).Assembly);

    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

    return services;
  }

  // ── Application Services ──────────────────────────────────────────
  public static IServiceCollection AddTmsServices(
      this IServiceCollection services)
  {
    // Transcript channel — bounded for backpressure
    services.AddSingleton(Channel.CreateBounded<TranscriptRequest>(
        new BoundedChannelOptions(100)
        {
          FullMode = BoundedChannelFullMode.Wait
        }));

    // Singleton services
    services.AddSingleton<ITranscriptStatusStore, InMemoryTranscriptStatusStore>();
    services.AddSingleton<ITranscriptNotificationService, SignalRTranscriptNotificationService>();
    services.AddSingleton<IAuthorizationHandler, CourseInstructorHandler>();
    services.AddSingleton<EnrollmentWorker>();

    // Scoped services
    services.AddScoped<ICourseServices, CourseQueryService>();
    services.AddScoped<ICachedCourseService, CachedCourseService>();
    services.AddScoped<IEnrollmentServices, EnrollmentQueryService>();
    services.AddScoped<ICourseService, CourseService>();
    services.AddScoped<IStudentService, StudentService>();
    services.AddScoped<ICourseDbService, CourseDbService>();
    services.AddScoped<IEnrollmentDbService, EnrollmentDbService>();
    services.AddScoped<IEnrollmentService, EnrollmentService>();

    // Hosted background service
    services.AddHostedService<TranscriptWorker>();

    return services;
  }

  // ── Caching ───────────────────────────────────────────────────────
  public static IServiceCollection AddTmsCaching(
      this IServiceCollection services)
  {
    services.AddHybridCache(options =>
    {
      options.DefaultEntryOptions = new HybridCacheEntryOptions
      {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
      };
    });

    return services;
  }

  // ── Database ──────────────────────────────────────────────────────
  public static IServiceCollection AddTmsDatabase(
      this IServiceCollection services,
      IConfiguration config)
  {
    services.AddDbContext<TmsDbContext>(options =>
        options.UseNpgsql(config.GetConnectionString("TmsDatabase"))
               .LogTo(Console.WriteLine, LogLevel.Information)
               .EnableSensitiveDataLogging());

    return services;
  }

  // ── OpenAPI / Scalar ──────────────────────────────────────────────
  public static IServiceCollection AddTmsOpenApi(
      this IServiceCollection services)
  {
    services.AddOpenApi("v1", options =>
    {
      options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
    });

    services.AddOpenApi("v2", options =>
    {
      options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
    });

    return services;
  }

  // ── Error Handling ────────────────────────────────────────────────
  public static IServiceCollection AddTmsErrorHandling(
      this IServiceCollection services)
  {
    services.AddExceptionHandler<GlobalExceptionHandler>();
    services.AddProblemDetails();

    return services;
  }

  // ── Options Pattern ───────────────────────────────────────────────
  public static IServiceCollection AddTmsOptions(
      this IServiceCollection services)
  {
    services.AddOptions<PaymentOptions>()
        .BindConfiguration("Payments")
        .ValidateDataAnnotations()
        .ValidateOnStart();

    return services;
  }
}