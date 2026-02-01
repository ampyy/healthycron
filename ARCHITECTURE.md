# Configuration Architecture

## Configuration Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                     APPLICATION STARTUP                          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   CONFIGURATION SOURCES                          │
│                   (Loaded in order)                              │
├─────────────────────────────────────────────────────────────────┤
│  1. appsettings.json                    (Base defaults)         │
│  2. appsettings.Development.json        (Dev overrides)         │
│  3. User Secrets                        (Local secrets)         │
│  4. Environment Variables               (Production secrets)    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│              STRONGLY-TYPED CONFIGURATION CLASSES                │
├─────────────────────────────────────────────────────────────────┤
│  • DatabaseSettings      (Connection strings)                   │
│  • EmailSettings         (SMTP configuration)                   │
│  • RedisSettings         (Cache connection)                     │
│  • AwsSettings           (AWS credentials)                      │
│  • QueueSettings         (SQS queue URLs)                       │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    VALIDATION LAYER                              │
│         (ConfigurationValidator + Data Annotations)              │
├─────────────────────────────────────────────────────────────────┤
│  ✓ Required fields present?                                     │
│  ✓ Email addresses valid?                                       │
│  ✓ Port numbers in range?                                       │
│  ✓ URLs properly formatted?                                     │
└─────────────────────────────────────────────────────────────────┘
                              │
                    ┌─────────┴─────────┐
                    │                   │
                    ▼                   ▼
        ┌──────────────────┐  ┌──────────────────┐
        │   VALIDATION     │  │   VALIDATION     │
        │     FAILED       │  │    SUCCEEDED     │
        └──────────────────┘  └──────────────────┘
                    │                   │
                    ▼                   ▼
        ┌──────────────────┐  ┌──────────────────┐
        │  APP FAILS TO    │  │  CONFIGURATION   │
        │  START WITH      │  │  REGISTERED AS   │
        │  CLEAR ERROR     │  │  SINGLETONS      │
        └──────────────────┘  └──────────────────┘
                                        │
                                        ▼
                              ┌──────────────────┐
                              │  SERVICES CAN    │
                              │  INJECT CONFIG   │
                              │  OBJECTS         │
                              └──────────────────┘
```

## Configuration Hierarchy

```
Development Environment:
┌──────────────────────────────────────┐
│  appsettings.json                    │  ← Base defaults
│  (No secrets, empty values)          │
└──────────────────────────────────────┘
              ↓ Overridden by
┌──────────────────────────────────────┐
│  appsettings.Development.json        │  ← Dev-specific settings
│  (Can contain dev secrets)           │
└──────────────────────────────────────┘
              ↓ Overridden by
┌──────────────────────────────────────┐
│  User Secrets                        │  ← Local secrets (RECOMMENDED)
│  (Stored outside project)            │
└──────────────────────────────────────┘

Production Environment:
┌──────────────────────────────────────┐
│  appsettings.json                    │  ← Base defaults
│  (No secrets, empty values)          │
└──────────────────────────────────────┘
              ↓ Overridden by
┌──────────────────────────────────────┐
│  Environment Variables               │  ← Production secrets
│  (Set by hosting platform)           │
└──────────────────────────────────────┘
```

## Service Dependency Injection

```
┌─────────────────────────────────────────────────────────────────┐
│                         Program.cs                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  var emailSettings = builder.Services                           │
│      .AddValidatedConfiguration<EmailSettings>(...)             │
│                                                                  │
│  // EmailSettings is now registered as a singleton              │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      EmailService.cs                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  public EmailService(EmailSettings emailSettings)               │
│  {                                                               │
│      _smtpHost = emailSettings.SmtpHost;                        │
│      _fromPassword = emailSettings.FromPassword;                │
│      // Already validated! No null checks needed                │
│  }                                                               │
└─────────────────────────────────────────────────────────────────┘
```

## Configuration Class Structure

```
┌─────────────────────────────────────────────────────────────────┐
│              Models/Configuration/EmailSettings.cs               │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  public class EmailSettings                                     │
│  {                                                               │
│      public const string SectionName = "Email";                 │
│                                                                  │
│      [Required(ErrorMessage = "SmtpHost is required")]          │
│      public string SmtpHost { get; set; }                       │
│                                                                  │
│      [Range(1, 65535)]                                          │
│      public int SmtpPort { get; set; }                          │
│                                                                  │
│      [Required]                                                 │
│      [EmailAddress]                                             │
│      public string FromEmail { get; set; }                      │
│                                                                  │
│      [Required]                                                 │
│      public string FromPassword { get; set; }                   │
│  }                                                               │
└─────────────────────────────────────────────────────────────────┘
```

## Validation Process

```
┌─────────────────────────────────────────────────────────────────┐
│         AddValidatedConfiguration<EmailSettings>()               │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  1. Create new EmailSettings instance                           │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  2. Bind configuration section to instance                      │
│     configuration.GetSection("Email").Bind(emailSettings)       │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  3. Validate using Data Annotations                             │
│     Validator.TryValidateObject(emailSettings, ...)             │
└─────────────────────────────────────────────────────────────────┘
                              │
                    ┌─────────┴─────────┐
                    │                   │
                    ▼                   ▼
        ┌──────────────────┐  ┌──────────────────┐
        │  Has Errors?     │  │  Valid?          │
        │  Throw Exception │  │  Register as     │
        │  with Details    │  │  Singleton       │
        └──────────────────┘  └──────────────────┘
```

## Environment Variable Mapping

```
Configuration Structure:          Environment Variable:
─────────────────────────────────────────────────────────
Email                             
  ├─ SmtpHost                     Email__SmtpHost
  ├─ SmtpPort                     Email__SmtpPort
  ├─ FromEmail                    Email__FromEmail
  └─ FromPassword                 Email__FromPassword

ConnectionStrings
  └─ DefaultConnection            ConnectionStrings__DefaultConnection

AWS
  ├─ Region                       AWS__Region
  ├─ AccessKey                    AWS__AccessKey
  └─ SecretKey                    AWS__SecretKey

QueueSettings
  └─ HeartbeatQueueUrl            QueueSettings__HeartbeatQueueUrl

Note: Use double underscores (__) to represent nested sections
```

## Security Flow

```
Development:
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   Developer  │────▶│ User Secrets │────▶│     App      │
│              │     │ (encrypted)  │     │              │
└──────────────┘     └──────────────┘     └──────────────┘
                            │
                            ▼
                     Stored in:
                     ~/.microsoft/usersecrets/<id>/secrets.json
                     (Outside project directory)

Production:
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   Platform   │────▶│ Environment  │────▶│     App      │
│   Admin      │     │  Variables   │     │              │
└──────────────┘     └──────────────┘     └──────────────┘
                            │
                            ▼
                     Examples:
                     - Azure App Service Configuration
                     - AWS Elastic Beanstalk Environment
                     - Kubernetes Secrets
                     - Docker Compose .env files
```

## Benefits Visualization

```
Before (Magic Strings):                After (Strongly-Typed):
─────────────────────────────────────────────────────────────────
var host = config["Email:SmtpHost"]    var host = settings.SmtpHost
           ↑                                      ↑
           No IntelliSense                        IntelliSense ✓
           No compile-time checking               Compile-time checking ✓
           Runtime errors possible                Validated at startup ✓
           Hard to discover                       Self-documenting ✓
```

## Error Handling

```
Missing Configuration:
┌─────────────────────────────────────────────────────────────────┐
│  Configuration validation failed for section 'Email':           │
│    - FromEmail is required                                      │
│    - FromPassword is required                                   │
│                                                                  │
│  Application startup failed.                                    │
└─────────────────────────────────────────────────────────────────┘

Invalid Configuration:
┌─────────────────────────────────────────────────────────────────┐
│  Configuration validation failed for section 'Email':           │
│    - FromEmail must be a valid email address                    │
│    - SmtpPort must be between 1 and 65535                       │
│                                                                  │
│  Application startup failed.                                    │
└─────────────────────────────────────────────────────────────────┘
```

## File Organization

```
HealthyCron/
├── Models/
│   └── Configuration/
│       ├── DatabaseSettings.cs      ← Configuration classes
│       ├── EmailSettings.cs
│       ├── RedisSettings.cs
│       ├── AwsSettings.cs
│       └── QueueSettings.cs
│
├── Utilities/
│   ├── ConfigurationValidator.cs    ← Validation logic
│   └── Extensions/
│       └── ConfigurationExtensions.cs
│
├── Program.cs                        ← Registration & validation
│
├── appsettings.json                  ← Base (no secrets)
├── appsettings.Development.json      ← Dev overrides
├── appsettings.Development.template.json  ← Template for team
│
└── Documentation/
    ├── CONFIGURATION.md              ← Setup guide
    ├── CONFIGURATION_EXAMPLES.md     ← Code examples
    ├── QUICKSTART_CONFIG.md          ← Quick reference
    └── SECURITY_NOTICE.md            ← Security actions
```
