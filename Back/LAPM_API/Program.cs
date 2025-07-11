using LAPM_API.Data;
using LAPM_API.Services;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.IISIntegration; // <-- Add this using statement
using Microsoft.EntityFrameworkCore;
using LAPM_API.Data;
using LAPM_API.Services;
using System.Text.Json.Serialization;

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

// --- 1. Configure Services ---

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("https://lapm.control.lab.local")
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials();
                      });
});

// --- MODIFICATION: Configure IIS Integration ---
// This explicitly tells the application to use the authentication result from IIS.
// This is the key to solving the User.Identity issue in production.
builder.Services.Configure<IISOptions>(options =>
{
    options.AutomaticAuthentication = true;
});

// Configure Windows Authentication (Negotiate)
builder.Services.AddAuthentication(IISDefaults.AuthenticationScheme); // <-- Use IISDefaults for production

var adminGroup = builder.Configuration["AccessControl:AdminGroup"];
var userGroup = builder.Configuration["AccessControl:UserGroup"];
if (string.IsNullOrEmpty(adminGroup) || string.IsNullOrEmpty(userGroup))
{
    throw new Exception("AdminGroup and UserGroup must be configured in appsettings.json under AccessControl.");
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("IsAdmin", policy => policy.RequireRole(adminGroup));
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireRole(userGroup)
        .Build();
});

builder.Services.AddScoped<IActiveDirectoryService, ActiveDirectoryService>();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<ExpiredRequestCleanupService>();

// --- 2. Build the App ---
var app = builder.Build();

// --- 3. Configure the HTTP request pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors(MyAllowSpecificOrigins);

// Authentication must come before Authorization.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
