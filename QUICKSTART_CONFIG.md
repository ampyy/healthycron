# Quick Start: Environment Configuration

## For New Developers

### 1. Clone and Setup

```bash
git clone <repo-url>
cd healthycron
```

### 2. Initialize User Secrets

```bash
dotnet user-secrets init
```

### 3. Configure Secrets

Copy and run these commands with your actual values:

```bash
# Database
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=healthycron;Username=postgres;Password=YOUR_PASSWORD"

# Email (use Gmail app password)
dotnet user-secrets set "Email:FromEmail" "your-email@gmail.com"
dotnet user-secrets set "Email:FromPassword" "your-app-password"

# Redis
dotnet user-secrets set "Redis:ConnectionString" "redis://localhost:6379"

# AWS
dotnet user-secrets set "AWS:Region" "ap-south-1"
dotnet user-secrets set "AWS:AccessKey" "YOUR_AWS_ACCESS_KEY"
dotnet user-secrets set "AWS:SecretKey" "YOUR_AWS_SECRET_KEY"

# SQS Queue
dotnet user-secrets set "QueueSettings:HeartbeatQueueUrl" "https://sqs.ap-south-1.amazonaws.com/YOUR_ACCOUNT/YOUR_QUEUE"
```

### 4. Run the Application

```bash
dotnet run
```

✅ If all configuration is valid, the app will start!  
❌ If configuration is missing, you'll see a clear error message.

---

## For Production Deployment

Set these environment variables on your hosting platform:

```bash
ConnectionStrings__DefaultConnection="Host=prod-db;Port=5432;Database=healthycron;Username=app;Password=STRONG_PASSWORD"
Email__FromEmail="noreply@healthycron.com"
Email__FromPassword="PRODUCTION_PASSWORD"
Redis__ConnectionString="rediss://default:PASSWORD@redis.prod:6379"
AWS__Region="ap-south-1"
AWS__AccessKey="PROD_ACCESS_KEY"
AWS__SecretKey="PROD_SECRET_KEY"
QueueSettings__HeartbeatQueueUrl="https://sqs.ap-south-1.amazonaws.com/ACCOUNT/QUEUE"
```

---

## Troubleshooting

### App won't start?

Check the error message. It will tell you exactly what's missing:

```
Configuration validation failed for section 'Email':
  - FromEmail is required
  - FromPassword is required
```

### Verify your secrets:

```bash
dotnet user-secrets list
```

### Clear all secrets:

```bash
dotnet user-secrets clear
```

---

## Need More Help?

- 📖 Full Guide: See [CONFIGURATION.md](./CONFIGURATION.md)
- 💡 Examples: See [CONFIGURATION_EXAMPLES.md](./CONFIGURATION_EXAMPLES.md)
