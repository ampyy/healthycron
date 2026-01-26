var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5032");

// Add services to the container.
builder.Services.AddControllersWithViews();

// Database Connection Factory Setup
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddSingleton<HealthyCron.Utilities.Interface.IDbConnectionFactory>(
    new HealthyCron.Utilities.Service.DbConnectionFactory(connectionString));

// Configure Dapper to support snake_case column names
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

// Register Repositories
builder.Services.AddScoped<HealthyCron.Data.Interfaces.IAuthRepository, HealthyCron.Data.Repository.AuthRepository>();
builder.Services.AddScoped<HealthyCron.Data.Interfaces.IProjectRepository, HealthyCron.Data.Repository.ProjectRepository>();
builder.Services.AddScoped<HealthyCron.Data.Interfaces.IMonitorRepository, HealthyCron.Data.Repository.MonitorRepository>();

// Register Logic Services
builder.Services.AddScoped<HealthyCron.Logic.Interfaces.IAuthService, HealthyCron.Logic.Service.AuthService>();
builder.Services.AddScoped<HealthyCron.Logic.Service.ProjectService>();

// Register Email Service
builder.Services.AddScoped<HealthyCron.Utilities.Interface.IEmailService, HealthyCron.Utilities.Service.EmailService>();

// Redis Connection Setup for Upstash
var redisConnectionString = builder.Configuration["REDIS_CONNECTION"]
                           ?? throw new InvalidOperationException("Redis connection string 'REDIS_CONNECTION' not found.");

// Parse the rediss:// URL to extract host, port, and password
// Format: rediss://default:PASSWORD@HOST:PORT
var uri = new Uri(redisConnectionString.Split(',')[0]); // Remove any additional parameters
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

// AWS SQS Configuration
var awsOptions = builder.Configuration.GetAWSOptions();
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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
