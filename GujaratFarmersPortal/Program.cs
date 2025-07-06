using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using Microsoft.Data.SqlClient;

using GujaratFarmersPortal.Data;
using GujaratFarmersPortal.Services;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddControllersWithViews();

// Add SQL Connection for ADO.NET with Stored Procedures
builder.Services.AddScoped<IDbConnection>(provider =>
    new SqlConnection(builder.Configuration.GetConnectionString("SqlConnection")));

// Register Data Access Layer
builder.Services.AddScoped<IAdminDataAccess, AdminDataAccess>();
builder.Services.AddScoped<IAccountDataAccess, AccountDataAccess>();

// Register Service Layer
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAccountService, AccountService>();

// Add Authentication with Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(
            builder.Configuration.GetValue<int>("AppSettings:SessionTimeoutMinutes", 30));
        options.SlidingExpiration = true;
        options.Cookie.Name = "GujaratFarmersAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

// Add Authorization with Role-based policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
    options.AddPolicy("ModeratorOrAdmin", policy => policy.RequireRole("Moderator", "Admin"));
    options.AddPolicy("AdminOrModerator", policy => policy.RequireRole("Admin", "Moderator"));
});

// Add Session Support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(
        builder.Configuration.GetValue<int>("AppSettings:SessionTimeoutMinutes", 30));
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "GujaratFarmersSession";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Add Memory Cache
builder.Services.AddMemoryCache();

// Add HTTP Context Accessor
builder.Services.AddHttpContextAccessor();

// Add CORS for API endpoints
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    options.AddPolicy("RestrictedCORS", policy =>
    {
        policy.WithOrigins("https://localhost:7251", "https://yourapp.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add API Explorer for Swagger documentation
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen(c =>
//{
//    c.SwaggerDoc("v1", new OpenApiInfo
//    {
//        Title = "Gujarat Farmers Portal API",
//        Version = "v1.0",
//        Description = "Complete API for Gujarat Farmers Classified Portal - Authentication, User Management, and Location Services",
//        Contact = new OpenApiContact
//        {
//            Name = "Gujarat Farmers Portal Team",
//            Email = "support@gujaratfarmers.com",
//            Url = new Uri("https://gujaratfarmers.com")
//        },
//        License = new OpenApiLicense
//        {
//            Name = "MIT License",
//            Url = new Uri("https://opensource.org/licenses/MIT")
//        }
//    });

//    // Add JWT Authentication to Swagger
//    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//    {
//        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
//        Name = "Authorization",
//        In = ParameterLocation.Header,
//        Type = SecuritySchemeType.ApiKey,
//        Scheme = "Bearer"
//    });

//    c.AddSecurityRequirement(new OpenApiSecurityRequirement
//    {
//        {
//            new OpenApiSecurityScheme
//            {
//                Reference = new OpenApiReference
//                {
//                    Type = ReferenceType.SecurityScheme,
//                    Id = "Bearer"
//                }
//            },
//            new string[] {}
//        }
//    });

//    // Add Cookie Authentication to Swagger
//    c.AddSecurityDefinition("Cookie", new OpenApiSecurityScheme
//    {
//        Description = "Cookie Authentication using GujaratFarmersAuth cookie",
//        Name = "GujaratFarmersAuth",
//        In = ParameterLocation.Cookie,
//        Type = SecuritySchemeType.ApiKey,
//        Scheme = "Cookie"
//    });

//    c.AddSecurityRequirement(new OpenApiSecurityRequirement
//    {
//        {
//            new OpenApiSecurityScheme
//            {
//                Reference = new OpenApiReference
//                {
//                    Type = ReferenceType.SecurityScheme,
//                    Id = "Cookie"
//                }
//            },
//            new string[] {}
//        }
//    });

//    // Include XML comments (optional)
//    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
//    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
//    if (File.Exists(xmlPath))
//    {
//        c.IncludeXmlComments(xmlPath);
//    }

//    // Group APIs by tags
//    c.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
//    c.DocInclusionPredicate((name, api) => true);
//});

// Add Health Checks
//builder.Services.AddHealthChecks()
//    .AddSqlServer(builder.Configuration.GetConnectionString("SqlConnection"), name: "database");

// Add Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Add Response Caching
builder.Services.AddResponseCaching();

// Configure Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add Application Insights (optional)
//if (!string.IsNullOrEmpty(builder.Configuration["ApplicationInsights:ConnectionString"]))
//{
//    builder.Services.AddApplicationInsightsTelemetry();
//}

// Register Configuration as Injectable Service
builder.Services.Configure<IConfiguration>(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Enable Swagger in development and staging
//if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI(c =>
//    {
//        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gujarat Farmers Portal API v1.0");
//        c.RoutePrefix = "api-docs";
//        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
//        c.DefaultModelsExpandDepth(-1);
//        c.DisplayRequestDuration();
//        c.EnableFilter();
//        c.ShowExtensions();
//        c.EnableValidator();
//        c.SupportedSubmitMethods(Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Get,
//                                 Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Post,
//                                 Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Put,
//                                 Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Delete);
//    });
//}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Enable Response Compression
app.UseResponseCompression();

// Enable Response Caching
app.UseResponseCaching();

app.UseRouting();

// Enable CORS
app.UseCors("AllowAllOrigins");

// Enable Session before Authentication
app.UseSession();

// Enable Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Configure Custom Routes
app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/{action=Dashboard}/{id?}",
    defaults: new { controller = "Admin" });

app.MapControllerRoute(
    name: "account",
    pattern: "Account/{action=Login}/{id?}",
    defaults: new { controller = "Account" });

app.MapControllerRoute(
    name: "user",
    pattern: "User/{action=Dashboard}/{id?}",
    defaults: new { controller = "User" });

app.MapControllerRoute(
    name: "post",
    pattern: "Post/{action=Index}/{id?}",
    defaults: new { controller = "Post" });

app.MapControllerRoute(
    name: "category",
    pattern: "Category/{action=Index}/{id?}",
    defaults: new { controller = "Category" });

// API Routes
app.MapControllerRoute(
    name: "api",
    pattern: "api/{controller}/{action}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map Health Checks
//app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
//{
//    ResponseWriter = async (context, report) =>
//    {
//        context.Response.ContentType = "application/json";
//        var response = new
//        {
//            status = report.Status.ToString(),
//            checks = report.Entries.Select(x => new
//            {
//                name = x.Key,
//                status = x.Value.Status.ToString(),
//                exception = x.Value.Exception?.Message,
//                duration = x.Value.Duration.ToString()
//            }),
//            duration = report.TotalDuration.ToString()
//        };
//        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
//    }
//});

// Map Controllers
app.MapControllers();

// Create uploads directory if it doesn't exist
var uploadsPath = Path.Combine(app.Environment.WebRootPath, "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
    Directory.CreateDirectory(Path.Combine(uploadsPath, "images"));
    Directory.CreateDirectory(Path.Combine(uploadsPath, "videos"));
    Directory.CreateDirectory(Path.Combine(uploadsPath, "profiles"));
    Directory.CreateDirectory(Path.Combine(uploadsPath, "posts"));
    Directory.CreateDirectory(Path.Combine(uploadsPath, "thumbnails"));
    Directory.CreateDirectory(Path.Combine(uploadsPath, "documents"));
    Directory.CreateDirectory(Path.Combine(uploadsPath, "temp"));
}

// Create logs directory if it doesn't exist
var logsPath = Path.Combine(app.Environment.ContentRootPath, "logs");
if (!Directory.Exists(logsPath))
{
    Directory.CreateDirectory(logsPath);
}

// Startup message
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("=== Gujarat Farmers Portal Started ===");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("Application started at: {StartTime}", DateTime.Now);

if (app.Environment.IsDevelopment())
{
    logger.LogInformation("Swagger UI available at: /api-docs");
    logger.LogInformation("Health checks available at: /health");
}

logger.LogInformation("=== Services Registered ===");
logger.LogInformation("- Admin Services: IAdminService, IAdminDataAccess");
logger.LogInformation("- Account Services: IAccountService, IAccountDataAccess");
logger.LogInformation("- Authentication: Cookie-based with role support");
logger.LogInformation("- Database: SQL Server with Dapper ORM");
logger.LogInformation("- Caching: Memory Cache enabled");
logger.LogInformation("- Session: Enabled with {Timeout} minutes timeout",
    builder.Configuration.GetValue<int>("AppSettings:SessionTimeoutMinutes", 30));

// Global exception handler
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var errorFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (errorFeature != null)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(errorFeature.Error, "Global exception handler caught an error");

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
            {
                error = "An internal server error occurred.",
                message = app.Environment.IsDevelopment() ? errorFeature.Error.Message : "Please contact support.",
                timestamp = DateTime.UtcNow
            }));
        }
    });
});

app.Run();

// Extension method for configuration validation
public static class ConfigurationExtensions
{
    public static void ValidateConfiguration(this IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SqlConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Database connection string 'SqlConnection' is not configured.");
        }

        var sessionTimeout = configuration.GetValue<int>("AppSettings:SessionTimeoutMinutes", 30);
        if (sessionTimeout <= 0)
        {
            throw new InvalidOperationException("Session timeout must be greater than 0.");
        }
    }
}