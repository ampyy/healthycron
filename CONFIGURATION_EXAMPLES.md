# HealthyCron - Strongly-Typed Configuration Example

This example demonstrates how to read configuration values using the strongly-typed configuration classes.

## Reading Configuration in Your Services

### Example 1: Using DatabaseSettings

```csharp
using HealthyCron.Models.Configuration;

public class MyRepository
{
    private readonly string _connectionString;

    // Inject the strongly-typed configuration
    public MyRepository(DatabaseSettings databaseSettings)
    {
        _connectionString = databaseSettings.DefaultConnection;
        // Use _connectionString...
    }
}
```

### Example 2: Using EmailSettings

```csharp
using HealthyCron.Models.Configuration;

public class NotificationService
{
    private readonly EmailSettings _emailSettings;

    public NotificationService(EmailSettings emailSettings)
    {
        _emailSettings = emailSettings;
        
        // Access properties with IntelliSense support
        var host = _emailSettings.SmtpHost;
        var port = _emailSettings.SmtpPort;
        var from = _emailSettings.FromEmail;
    }
}
```

### Example 3: Using AWS Settings

```csharp
using HealthyCron.Models.Configuration;

public class S3Service
{
    private readonly AwsSettings _awsSettings;

    public S3Service(AwsSettings awsSettings)
    {
        _awsSettings = awsSettings;
        
        // Create AWS client with credentials
        var credentials = new Amazon.Runtime.BasicAWSCredentials(
            _awsSettings.AccessKey,
            _awsSettings.SecretKey
        );
        
        var region = Amazon.RegionEndpoint.GetBySystemName(_awsSettings.Region);
    }
}
```

## Benefits of Strongly-Typed Configuration

### ✅ Type Safety
- No more magic strings like `configuration["Email:SmtpHost"]`
- Compile-time checking of property names
- IntelliSense support in your IDE

### ✅ Validation at Startup
- Application fails fast if configuration is invalid
- Clear error messages about what's missing
- No runtime surprises in production

### ✅ Testability
- Easy to mock configuration in unit tests
- No need to set up IConfiguration infrastructure

### ✅ Discoverability
- Easy to see what configuration is available
- Self-documenting through property names and XML comments

## Adding New Configuration Sections

### Step 1: Create Configuration Class

```csharp
using System.ComponentModel.DataAnnotations;

namespace HealthyCron.Models.Configuration
{
    public class MyNewSettings
    {
        public const string SectionName = "MyNewSection";

        [Required(ErrorMessage = "ApiKey is required")]
        public string ApiKey { get; set; } = string.Empty;

        [Range(1, 100)]
        public int MaxRetries { get; set; } = 3;
    }
}
```

### Step 2: Register in Program.cs

```csharp
var mySettings = builder.Services.AddValidatedConfiguration<MyNewSettings>(
    builder.Configuration, MyNewSettings.SectionName);
```

### Step 3: Add to appsettings.json

```json
{
  "MyNewSection": {
    "ApiKey": "",
    "MaxRetries": 5
  }
}
```

### Step 4: Use in Your Services

```csharp
public class MyService
{
    public MyService(MyNewSettings settings)
    {
        // Use settings...
    }
}
```

## JWT Configuration Example

If you need JWT configuration, here's how to add it:

### JwtSettings.cs

```csharp
using System.ComponentModel.DataAnnotations;

namespace HealthyCron.Models.Configuration
{
    public class JwtSettings
    {
        public const string SectionName = "Jwt";

        [Required(ErrorMessage = "Secret is required")]
        [MinLength(32, ErrorMessage = "Secret must be at least 32 characters")]
        public string Secret { get; set; } = string.Empty;

        [Required(ErrorMessage = "Issuer is required")]
        public string Issuer { get; set; } = string.Empty;

        [Required(ErrorMessage = "Audience is required")]
        public string Audience { get; set; } = string.Empty;

        [Range(1, 525600, ErrorMessage = "ExpiryMinutes must be between 1 and 525600 (1 year)")]
        public int ExpiryMinutes { get; set; } = 60;
    }
}
```

### appsettings.json

```json
{
  "Jwt": {
    "Secret": "",
    "Issuer": "HealthyCron",
    "Audience": "HealthyCronUsers",
    "ExpiryMinutes": 60
  }
}
```

### User Secrets (Development)

```bash
dotnet user-secrets set "Jwt:Secret" "your-super-secret-key-at-least-32-characters-long"
dotnet user-secrets set "Jwt:Issuer" "HealthyCron"
dotnet user-secrets set "Jwt:Audience" "HealthyCronUsers"
```

### Environment Variables (Production)

```bash
Jwt__Secret="production-secret-key-at-least-32-chars"
Jwt__Issuer="HealthyCron"
Jwt__Audience="HealthyCronUsers"
Jwt__ExpiryMinutes="120"
```

### Register in Program.cs

```csharp
var jwtSettings = builder.Services.AddValidatedConfiguration<JwtSettings>(
    builder.Configuration, JwtSettings.SectionName);

// Configure JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Secret))
        };
    });
```

### Usage in Controllers

```csharp
public class AuthController : Controller
{
    private readonly JwtSettings _jwtSettings;

    public AuthController(JwtSettings jwtSettings)
    {
        _jwtSettings = jwtSettings;
    }

    public string GenerateToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        
        var credentials = new SigningCredentials(
            securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

## Testing Configuration

### Unit Test Example

```csharp
using HealthyCron.Models.Configuration;
using Xunit;

public class EmailServiceTests
{
    [Fact]
    public void SendEmail_WithValidSettings_Succeeds()
    {
        // Arrange
        var emailSettings = new EmailSettings
        {
            SmtpHost = "smtp.test.com",
            SmtpPort = 587,
            FromEmail = "test@example.com",
            FromPassword = "password"
        };

        var service = new EmailService(emailSettings);

        // Act & Assert
        // Test your service...
    }
}
```

## Migration Guide

If you have existing code using `IConfiguration` directly:

### Before (Old Way)
```csharp
public class MyService
{
    private readonly string _apiKey;

    public MyService(IConfiguration configuration)
    {
        _apiKey = configuration["MySection:ApiKey"] 
            ?? throw new InvalidOperationException("ApiKey not found");
    }
}
```

### After (New Way)
```csharp
public class MyService
{
    private readonly string _apiKey;

    public MyService(MySettings settings)
    {
        _apiKey = settings.ApiKey; // Already validated at startup
    }
}
```

## Summary

- ✅ Create configuration classes in `Models/Configuration/`
- ✅ Use `[Required]`, `[Range]`, `[EmailAddress]` for validation
- ✅ Register with `AddValidatedConfiguration<T>()` in Program.cs
- ✅ Inject configuration classes directly into your services
- ✅ Store secrets in User Secrets (dev) or Environment Variables (prod)
- ✅ Application fails at startup if configuration is invalid
