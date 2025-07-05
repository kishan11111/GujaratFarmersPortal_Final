using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Data.SqlClient;
using System.Data;
using GujaratFarmersPortal.Data;
using GujaratFarmersPortal.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add SQL Connection for ADO.NET with Stored Procedures
builder.Services.AddScoped<IDbConnection>(provider =>
    new SqlConnection(builder.Configuration.GetConnectionString("SqlConnection")));

// Register Data Access Layer
builder.Services.AddScoped<IAdminDataAccess, AdminDataAccess>();

// Register Service Layer
builder.Services.AddScoped<IAdminService, AdminService>();

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

// Register Configuration as Injectable Service
builder.Services.Configure<IConfiguration>(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

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
    name: "post",
    pattern: "Post/{action=Index}/{id?}",
    defaults: new { controller = "Post" });

app.MapControllerRoute(
    name: "api",
    pattern: "api/{controller}/{action}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

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
}

app.Run();