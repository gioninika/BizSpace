using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BizSpace.Controllers;
using BizSpace.Data;
using BizSpace.Dtos;
using BizSpace.Middleware;
using BizSpace.Repositories;
using BizSpace.Repositories.Services;
using BizSpace.Services.Images;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace BizSpace;

public class Program
{
    public static void Main(string[] args)
    {
        LoadDotEnv();
        var builder = WebApplication.CreateBuilder(args);
        ValidateRequiredConfiguration(builder.Configuration);

        builder.Services.AddDbContext<StoreDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
        builder.Services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(
                new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper)))
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(entry => entry.Value?.Errors.Count > 0)
                        .ToDictionary(
                            entry => entry.Key,
                            entry => entry.Value!.Errors.Select(error =>
                                !string.IsNullOrWhiteSpace(error.ErrorMessage)
                                    ? error.ErrorMessage
                                    : error.Exception is JsonException
                                        ? "The request body contains invalid JSON."
                                        : "The value is invalid.").ToArray());

                    return new BadRequestObjectResult(new ApiResponse<Dictionary<string, string[]>>
                    {
                        Success = false,
                        Message = "Validation failed.",
                        Data = errors
                    });
                };
            });
        builder.Services.AddHealthChecks();
        builder.Services.AddSignalR();
        builder.Services.AddOpenApi();
        builder.Services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter the JWT token returned by /api/auth/login."
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
        });

        builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IJwtService, JwtService>();
        builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
        builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
        builder.Services.AddScoped<IProductService, ProductService>();
        builder.Services.Configure<CloudinaryOptions>(builder.Configuration.GetSection(CloudinaryOptions.SectionName));
        builder.Services.AddHttpClient<IProductImageStorage, CloudinaryProductImageStorage>(client =>
            client.Timeout = TimeSpan.FromSeconds(120));
        builder.Services.AddScoped<ProductImageDeletionProcessor>();
        builder.Services.AddHostedService<ProductImageDeletionWorker>();
        builder.Services.AddScoped<ICategoryService, CategoryService>();
        builder.Services.AddScoped<IStoreService, StoreService>();
        builder.Services.AddScoped<INotificationService, NotificationService>();
        builder.Services.AddScoped<ISalesAnalyticsService, SalesAnalyticsService>();
        builder.Services.AddScoped<IAddressBookService, AddressBookService>();
        builder.Services.AddScoped<IMarketingCampaignService, MarketingCampaignService>();
        builder.Services.AddScoped<IPricingService, PricingService>();
        builder.Services.AddScoped<ICartService, CartService>();
        builder.Services.AddScoped<IOrderService, OrderService>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IWishlistService, WishlistService>();
        builder.Services.AddScoped<IProductReviewService, ProductReviewService>();

        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

        var jwtKey = builder.Configuration["Jwt:Key"]!;
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken) &&
                            context.HttpContext.Request.Path.StartsWithSegments("/hubs/notifications"))
                            context.Token = accessToken;
                        return Task.CompletedTask;
                    }
                };
            });
        builder.Services.AddAuthorization();

        var app = builder.Build();
        if (app.Configuration.GetValue<bool>("Database:ApplyMigrations"))
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
            db.Database.Migrate();
            DatabaseSeeder.SeedAsync(scope.ServiceProvider, app.Configuration).GetAwaiter().GetResult();
        }

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseMiddleware<ExceptionMiddleware>();
        app.UseHttpsRedirection();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapHealthChecks("/health");
        app.MapHub<NotificationsHub>("/hubs/notifications").RequireAuthorization();
        app.MapControllers();
        app.Run();
    }

    private static void ValidateRequiredConfiguration(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Configure ConnectionStrings__DefaultConnection in .env or environment variables.");

        RequireSecret(configuration, "Jwt:Key", 32);
        RequireSecret(configuration, "Auth:CodeHmacKey", 32);
        if (string.IsNullOrWhiteSpace(configuration["Jwt:Issuer"]) ||
            string.IsNullOrWhiteSpace(configuration["Jwt:Audience"]))
            throw new InvalidOperationException("Configure Jwt:Issuer and Jwt:Audience before startup.");
    }

    private static void RequireSecret(IConfiguration configuration, string key, int minimumBytes)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value) || Encoding.UTF8.GetByteCount(value) < minimumBytes)
            throw new InvalidOperationException($"Configure {key} with at least {minimumBytes} bytes in .env or environment variables.");
    }

    private static void LoadDotEnv()
    {
        var path = FindDotEnvPath();
        if (path is null)
            return;

        foreach (var sourceLine in File.ReadLines(path))
        {
            var line = sourceLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;
            if (line.StartsWith("export ", StringComparison.Ordinal))
                line = line[7..].TrimStart();

            var separator = line.IndexOf('=');
            if (separator <= 0)
                continue;

            var name = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            if (name.Length == 0 || Environment.GetEnvironmentVariable(name) is not null)
                continue;

            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                var quote = value[0];
                value = value[1..^1];
                if (quote == '"')
                    value = UnescapeDotEnvValue(value);
            }

            Environment.SetEnvironmentVariable(name, value);
        }
    }

    private static string? FindDotEnvPath()
    {
        foreach (var startPath in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(Path.GetFullPath(startPath));
            while (directory is not null)
            {
                var path = Path.Combine(directory.FullName, ".env");
                if (File.Exists(path))
                    return path;
                directory = directory.Parent;
            }
        }

        return null;
    }

    private static string UnescapeDotEnvValue(string value)
    {
        var result = new StringBuilder(value.Length);
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] == '\\' && index + 1 < value.Length && value[index + 1] is '\\' or '"')
                result.Append(value[++index]);
            else
                result.Append(value[index]);
        }

        return result.ToString();
    }
}
