using HealthyCron.Models.Configuration;
using HealthyCron.Utilities.Extensions;

var builder = WebApplication.CreateBuilder(args);
// builder.WebHost.UseUrls("https://localhost:5032");
//
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

var slackSettings = builder.Services.AddValidatedConfiguration<SlackSettings>(
    builder.Configuration, SlackSettings.SectionName);

var encryptionSettings = builder.Services.AddValidatedConfiguration<EncryptionSettings>(
    builder.Configuration, EncryptionSettings.SectionName);




// ============================================================================
// SERVICE REGISTRATION
// ============================================================================

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// Authentication for [Authorize]
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
    });

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
builder.Services.AddScoped<HealthyCron.Data.Interfaces.IIntegrationRepository, HealthyCron.Data.Repository.IntegrationRepository>();
builder.Services.AddScoped<HealthyCron.Data.Interfaces.IProjectMemberRepository, HealthyCron.Data.Repository.ProjectMemberRepository>();


// Register Logic Services
builder.Services.AddScoped<HealthyCron.Logic.Interfaces.IAuthService, HealthyCron.Logic.Service.AuthService>();
builder.Services.AddScoped<HealthyCron.Logic.Service.ProjectService>();
builder.Services.AddScoped<HealthyCron.Logic.Interfaces.IAccessKeyService, HealthyCron.Logic.Service.AccessKeyService>();
builder.Services.AddScoped<HealthyCron.Logic.Interfaces.IPingService, HealthyCron.Logic.Service.PingService>();
builder.Services.AddScoped<HealthyCron.Logic.Interfaces.IProjectAuthService, HealthyCron.Logic.Service.ProjectAuthService>();


// Register Utility Services
builder.Services.AddSingleton<HealthyCron.Utilities.Interface.IEncryptionService, HealthyCron.Utilities.Service.EncryptionService>();
builder.Services.AddSingleton<HealthyCron.Utilities.Service.AxiomLogger>();
builder.Services.AddSingleton<HealthyCron.Utilities.Interface.IAxiomLogger>(sp => sp.GetRequiredService<HealthyCron.Utilities.Service.AxiomLogger>());

// Register HttpClient for SlackOAuthService
builder.Services.AddHttpClient<HealthyCron.Logic.Interfaces.ISlackOAuthService, HealthyCron.Logic.Service.SlackOAuthService>();

// Register HttpClient for PagerDutyService
builder.Services.AddHttpClient<HealthyCron.Logic.Service.IPagerDutyService, HealthyCron.Logic.Service.PagerDutyService>();

// Register Background Services

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
    ConnectRetry = 3,
    CheckCertificateRevocation = false
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

// Configure Forwarded Headers for Railway/Proxy
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

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

// Global Rate Limiting - 100 requests per 60s
app.UseMiddleware<HealthyCron.Utilities.RateLimitingMiddleware>();

app.UseRouting();

// Custom Session Middleware
app.UseMiddleware<HealthyCron.Utilities.SessionMiddleware>();
app.UseMiddleware<HealthyCron.Utilities.ApiKeyMiddleware>();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<HealthyCron.Hubs.MonitorHub>("/monitorHub");

// AUTO-MIGRATION: Ensure new columns exist
using (var scope = app.Services.CreateScope())
{
    try 
    {
        var db = scope.ServiceProvider.GetRequiredService<HealthyCron.Utilities.Interface.IDbConnectionFactory>();
        using var conn = db.CreateConnection();
        await Dapper.SqlMapper.ExecuteAsync(conn, @"
            ALTER TABLE users ADD COLUMN IF NOT EXISTS timezone VARCHAR(50);
            ALTER TABLE users ADD COLUMN IF NOT EXISTS receive_weekly_reports BOOLEAN DEFAULT TRUE;
            ALTER TABLE users ADD COLUMN IF NOT EXISTS receive_monthly_reports BOOLEAN DEFAULT TRUE;
            ALTER TABLE users ADD COLUMN IF NOT EXISTS receive_incident_reminders BOOLEAN DEFAULT TRUE;

            CREATE TABLE IF NOT EXISTS telegram_integrations (
                integration_id UUID PRIMARY KEY,
                chat_id TEXT NOT NULL,
                chat_name TEXT,
                chat_type TEXT,
                setup_token TEXT,
                setup_token_expires_at TIMESTAMPTZ,
                confirmed_at TIMESTAMPTZ,
                created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
                CONSTRAINT fk_telegram_integrations FOREIGN KEY (integration_id) REFERENCES integrations (id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS pushover_integrations (
                integration_id UUID PRIMARY KEY,
                subscription_key TEXT NOT NULL,
                device TEXT,
                sound TEXT,         
                created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
                CONSTRAINT fk_pushover_integrations FOREIGN KEY (integration_id) REFERENCES integrations (id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS pushover_pending_subscriptions (
                token TEXT PRIMARY KEY,
                project_id UUID NOT NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
                expires_at TIMESTAMPTZ NOT NULL,
                used_at TIMESTAMPTZ,
                CONSTRAINT fk_pushover_pending_project FOREIGN KEY (project_id) REFERENCES projects (id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS spike_integrations (
                integration_id UUID PRIMARY KEY,
                webhook_url TEXT NOT NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
                CONSTRAINT fk_spike_integrations FOREIGN KEY (integration_id) REFERENCES integrations (id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS webhook_integrations (
                integration_id UUID PRIMARY KEY,
                down_method TEXT NOT NULL DEFAULT 'POST',
                down_url TEXT NOT NULL,
                down_headers TEXT,
                down_body TEXT,
                up_method TEXT,
                up_url TEXT,
                up_headers TEXT,
                up_body TEXT,
                created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
                CONSTRAINT fk_webhook_integrations FOREIGN KEY (integration_id) REFERENCES integrations (id) ON DELETE CASCADE
            );

            ALTER TABLE webhook_integrations ALTER COLUMN down_url DROP NOT NULL;
            ALTER TABLE webhook_integrations ALTER COLUMN down_method DROP NOT NULL;

            CREATE TABLE IF NOT EXISTS temp_telegram_handshakes (
                token TEXT PRIMARY KEY,
                chat_id TEXT NOT NULL,
                chat_name TEXT,
                chat_type TEXT,
                expires_at TIMESTAMPTZ NOT NULL,
                used_at TIMESTAMPTZ,
                created_at TIMESTAMPTZ NOT NULL DEFAULT now()
            );
        ");
        Console.WriteLine("✅ Database schema verified/updated.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Migration warning: {ex.Message}");
    }
}

app.Run();
