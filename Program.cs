using HealthyCron.Models.Configuration;
using HealthyCron.Utilities.Extensions;

var builder = WebApplication.CreateBuilder(args);
// builder.WebHost.UseUrls("http://localhost:5032");

// ============================================================================
// CONFIGURATION SETUP - Strongly-typed and validated at startup
// ============================================================================

// Load and validate all configuration sections
// These will throw exceptions at startup if required values are missing
var databaseSettings = builder.Services.AddValidatedConfiguration<DatabaseSettings>(
    builder.Configuration, DatabaseSettings.SectionName);

var emailSettings = builder.Services.AddValidatedConfiguration<EmailSettings>(
    builder.Configuration, EmailSettings.SectionName);

var redisSettings = builder.Services.AddValidatedConfiguration<RedisSettings>(
    builder.Configuration, RedisSettings.SectionName);

var awsSettings = builder.Services.AddValidatedConfiguration<AwsSettings>(
    builder.Configuration, AwsSettings.SectionName);

var queueSettings = builder.Services.AddValidatedConfiguration<QueueSettings>(
    builder.Configuration, QueueSettings.SectionName);

// ============================================================================
// SERVICE REGISTRATION
// ============================================================================

// Add services to the container.
builder.Services.AddControllersWithViews();

// Database Connection Factory Setup
builder.Services.AddSingleton<HealthyCron.Utilities.Interface.IDbConnectionFactory>(
    new HealthyCron.Utilities.Service.DbConnectionFactory(databaseSettings.DefaultConnection));

// Configure Dapper to support snake_case column names
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

// Register Repositories
builder.Services.AddScoped<HealthyCron.Data.Interfaces.IAuthRepository, HealthyCron.Data.Repository.AuthRepository>();
builder.Services.AddScoped<HealthyCron.Data.Interfaces.IProjectRepository, HealthyCron.Data.Repository.ProjectRepository>();
builder.Services.AddScoped<HealthyCron.Data.Interfaces.IMonitorRepository, HealthyCron.Data.Repository.MonitorRepository>();
builder.Services.AddScoped<HealthyCron.Data.Interfaces.IProjectAccessKeyRepository, HealthyCron.Data.Repository.AccessKeyRepository>();

// Register Logic Services
builder.Services.AddScoped<HealthyCron.Logic.Interfaces.IAuthService, HealthyCron.Logic.Service.AuthService>();
builder.Services.AddScoped<HealthyCron.Logic.Service.ProjectService>();
builder.Services.AddScoped<HealthyCron.Logic.Interfaces.IAccessKeyService, HealthyCron.Logic.Service.AccessKeyService>();
builder.Services.AddScoped<HealthyCron.Logic.Interfaces.IAlertService, HealthyCron.Logic.Service.AlertService>();
builder.Services.AddScoped<HealthyCron.Logic.Interfaces.IPingService, HealthyCron.Logic.Service.PingService>();

// Register Background Services
builder.Services.AddHostedService<HealthyCron.Background.MonitorCheckWorker>();

// Register Email Service
// Register Email Service based on Environment
if (builder.Environment.IsDevelopment())
{
    // Use SMTP service for local development (MailHog, local SMTP, etc.)
    builder.Services.AddScoped<HealthyCron.Utilities.Interface.IEmailService, HealthyCron.Utilities.Service.SmtpEmailService>();
}
else
{
    // Use Resend API for production
    builder.Services.AddScoped<HealthyCron.Utilities.Interface.IEmailService, HealthyCron.Utilities.Service.ResendEmailService>();
}

// ============================================================================
// REDIS CONFIGURATION
// ============================================================================

// Parse the rediss:// URL to extract host, port, and password
// Format: rediss://default:PASSWORD@HOST:PORT
var uri = new Uri(redisSettings.ConnectionString.Split(',')[0]); // Remove any additional parameters
var password = uri.UserInfo.Split(':')[1]; // Extract password from default:PASSWORD
var host = uri.Host;
var port = uri.Port;

// Configure Redis options manually for Upstash (SSL/TLS connection)
var redisOptions = new StackExchange.Redis.ConfigurationOptions
{
    EndPoints = { { host, port } },
    Password = password,
    Ssl = true,
    SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
    AbortOnConnectFail = false,
    ConnectTimeout = 15000,
    SyncTimeout = 15000,
    AsyncTimeout = 15000,
    ConnectRetry = 3
};

// Connect synchronously (top-level await causes issues with hot reload)
var redisMultiplexer = StackExchange.Redis.ConnectionMultiplexer.Connect(redisOptions);
builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(redisMultiplexer);

// Register Redis Services as Singletons
// Singleton: One instance shared across the entire application lifetime
builder.Services.AddSingleton<HealthyCron.Utilities.Interface.ICacheService, HealthyCron.Utilities.Service.CacheService>();

// ============================================================================
// AWS CONFIGURATION
// ============================================================================

// Configure AWS credentials and region
var awsOptions = new Amazon.Extensions.NETCore.Setup.AWSOptions
{
    Credentials = new Amazon.Runtime.BasicAWSCredentials(awsSettings.AccessKey, awsSettings.SecretKey),
    Region = Amazon.RegionEndpoint.GetBySystemName(awsSettings.Region)
};

builder.Services.AddDefaultAWSOptions(awsOptions);
builder.Services.AddAWSService<Amazon.SQS.IAmazonSQS>();
builder.Services.AddSingleton<HealthyCron.Utilities.Interface.IQueueService, HealthyCron.Utilities.Service.QueueService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Development specific logic
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Custom Session Middleware
app.UseMiddleware<HealthyCron.Utilities.SessionMiddleware>();
app.UseMiddleware<HealthyCron.Utilities.ApiKeyMiddleware>();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
