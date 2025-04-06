using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OnlineAssessment.Web.Models;
using OnlineAssessment.Compiler.Services;
using OnlineAssessment.Infrastructure.Database;
using System.Text;
using Microsoft.Extensions.FileProviders;
using StackExchange.Redis;
using Confluent.Kafka;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using Serilog;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Polly;
using Polly.Retry;

var builder = WebApplication.CreateBuilder(args);

// ✅ Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// ✅ Load Configuration explicitly
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
var configuration = builder.Configuration;

// ✅ Configure Retry Policies
var retryPolicy = Policy
    .Handle<Exception>()
    .WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

// ✅ Configure Rate Limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting();

// ✅ Ensure JWT Secret is Valid
var jwtSecret = configuration["JWT:Secret"];
if (string.IsNullOrEmpty(jwtSecret) || jwtSecret.Length < 16)
{
    throw new Exception("JWT Secret Key is invalid! Ensure it is at least 16 characters long.");
}

// ✅ Add Database Context (SQL Server) with Retry Policy
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null)
            .MigrationsAssembly("OnlineAssessment.Web");
        });
});

// ✅ Add Redis with Retry Policy
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var redis = ConnectionMultiplexer.Connect(
        $"{configuration["Redis:Host"]}:{configuration["Redis:Port"]},abortConnect=false");
    
    redis.ConnectionFailed += (sender, e) =>
    {
        Log.Warning("Redis connection failed: {Exception}", e.Exception);
    };
    
    redis.ConnectionRestored += (sender, e) =>
    {
        Log.Information("Redis connection restored");
    };
    
    return redis;
});

// ✅ Add Kafka Producer with Retry Policy
builder.Services.AddSingleton<IProducer<string, string>>(sp =>
{
    var config = new ProducerConfig
    {
        BootstrapServers = configuration["Kafka:BootstrapServers"],
        MessageTimeoutMs = 5000,
        RequestTimeoutMs = 5000,
        RetryBackoffMs = 1000,
        MessageSendMaxRetries = 3
    };
    return new ProducerBuilder<string, string>(config).Build();
});

// ✅ Add Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>()
    .AddRedis($"{configuration["Redis:Host"]}:{configuration["Redis:Port"]}")
    .AddKafka(new ProducerConfig { BootstrapServers = configuration["Kafka:BootstrapServers"] });

// ✅ Configure CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

// ✅ Add Authentication with JWT and Cookies
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AccessDenied";
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = configuration["JWT:Issuer"],
        ValidAudience = configuration["JWT:Audience"],
        ClockSkew = TimeSpan.Zero
    };
    
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Log.Warning("Authentication failed: {Exception}", context.Exception);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Log.Information("Token validated for user: {User}", context.Principal.Identity.Name);
            return Task.CompletedTask;
        }
    };
});

// ✅ Add Authorization
builder.Services.AddAuthorization();

// ✅ Add Controllers
builder.Services.AddControllersWithViews();

// ✅ Add Code Execution Service
builder.Services.AddScoped<ICodeExecutionService, CodeExecutionService>();

// ✅ Configure Swagger with JWT Support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "OnlineAssessment API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by a space and your JWT token."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] { }
        }
    });
});

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

// ✅ Configure Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "OnlineAssessment API v1"));
}

// ✅ Add Global Error Handling
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";

        var exception = context.Features.Get<IExceptionHandlerFeature>();
        if (exception != null)
        {
            Log.Error(exception.Error, "Unhandled exception occurred");
            
            var error = new
            {
                StatusCode = context.Response.StatusCode,
                Message = "An error occurred while processing your request.",
                Detail = app.Environment.IsDevelopment() ? exception.Error.Message : "An error occurred"
            };

            await context.Response.WriteAsJsonAsync(error);
        }
    });
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseIpRateLimiting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// ✅ Add Health Check Endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            Status = report.Status.ToString(),
            Checks = report.Entries.Select(e => new
            {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                Description = e.Value.Description,
                Duration = e.Value.Duration
            }),
            TotalDuration = report.TotalDuration
        };
        await context.Response.WriteAsJsonAsync(result);
    }
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

// Configure routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "test",
    pattern: "Test/{action}/{id?}",
    defaults: new { controller = "Test" });

app.MapControllers();

// Ensure uploads directory exists
var uploadsPath = Path.Combine(builder.Environment.WebRootPath, "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

// Add static file serving for uploads
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.Run();
