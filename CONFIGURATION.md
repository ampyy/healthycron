# Environment Configuration Guide

This guide explains how to configure environment variables and secrets for the HealthyCron application in both local development and production environments.

## Table of Contents
- [Overview](#overview)
- [Local Development Setup](#local-development-setup)
- [Production Setup](#production-setup)
- [Configuration Validation](#configuration-validation)
- [Troubleshooting](#troubleshooting)

---

## Overview

The application uses **strongly-typed configuration** with automatic validation at startup. If any required configuration value is missing or invalid, the application will **fail to start** with a clear error message.

### Configuration Sources (in order of precedence)
1. **User Secrets** (Development only)
2. **Environment Variables** (Production)
3. **appsettings.Development.json** (Development)
4. **appsettings.json** (Base configuration)

---

## Local Development Setup

### Option 1: Using User Secrets (Recommended)

User secrets keep sensitive data out of your source code and appsettings files.

#### Step 1: Initialize User Secrets

```bash
cd /Users/amanpandey/Desktop/Repos/healthycron
dotnet user-secrets init
```

#### Step 2: Set Your Secrets

```bash
# Database Connection
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=healthycron;Username=postgres;Password=your-password"

# Email Settings
dotnet user-secrets set "Email:SmtpHost" "smtp.gmail.com"
dotnet user-secrets set "Email:SmtpPort" "587"
dotnet user-secrets set "Email:FromEmail" "your-email@gmail.com"
dotnet user-secrets set "Email:FromPassword" "your-app-password"

# Redis Connection
dotnet user-secrets set "Redis:ConnectionString" "rediss://default:password@host:6379"

# AWS Credentials
dotnet user-secrets set "AWS:Region" "ap-south-1"
dotnet user-secrets set "AWS:AccessKey" "YOUR_ACCESS_KEY"
dotnet user-secrets set "AWS:SecretKey" "YOUR_SECRET_KEY"

# SQS Queue
dotnet user-secrets set "QueueSettings:HeartbeatQueueUrl" "https://sqs.ap-south-1.amazonaws.com/123456789/your-queue"
```

#### Step 3: Verify User Secrets

```bash
dotnet user-secrets list
```

### Option 2: Using appsettings.Development.json

If you prefer to keep secrets in `appsettings.Development.json` (not recommended for teams):

1. Keep your secrets in `appsettings.Development.json`
2. **Ensure this file is in `.gitignore`** to prevent committing secrets
3. Share a template file (`appsettings.Development.template.json`) with your team

---

## Production Setup

In production, use **environment variables** to provide configuration values. The hosting platform (Azure, AWS, Docker, etc.) should inject these.

### Required Environment Variables

```bash
# Database
ConnectionStrings__DefaultConnection="Host=prod-db.example.com;Port=5432;Database=healthycron;Username=app_user;Password=STRONG_PASSWORD"

# Email
Email__SmtpHost="smtp.gmail.com"
Email__SmtpPort="587"
Email__FromEmail="noreply@healthycron.com"
Email__FromPassword="APP_SPECIFIC_PASSWORD"

# Redis
Redis__ConnectionString="rediss://default:REDIS_PASSWORD@redis.example.com:6379"

# AWS
AWS__Region="ap-south-1"
AWS__AccessKey="PROD_ACCESS_KEY"
AWS__SecretKey="PROD_SECRET_KEY"

# SQS
QueueSettings__HeartbeatQueueUrl="https://sqs.ap-south-1.amazonaws.com/123456789/prod-queue"
```

### Platform-Specific Instructions

#### Docker / Docker Compose

Create a `.env` file (add to `.gitignore`):

```env
ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=healthycron;Username=postgres;Password=secret
Email__SmtpHost=smtp.gmail.com
Email__SmtpPort=587
Email__FromEmail=noreply@healthycron.com
Email__FromPassword=your-password
Redis__ConnectionString=redis://redis:6379
AWS__Region=ap-south-1
AWS__AccessKey=YOUR_KEY
AWS__SecretKey=YOUR_SECRET
QueueSettings__HeartbeatQueueUrl=https://sqs.ap-south-1.amazonaws.com/123/queue
```

Reference in `docker-compose.yml`:

```yaml
services:
  web:
    image: healthycron:latest
    env_file:
      - .env
```

#### Azure App Service

Set environment variables in the Azure Portal:
1. Go to your App Service
2. Navigate to **Configuration** → **Application settings**
3. Add each variable with the format: `ConnectionStrings__DefaultConnection`

#### AWS Elastic Beanstalk

Use the EB CLI or console:

```bash
eb setenv ConnectionStrings__DefaultConnection="..." Email__SmtpHost="..." 
```

#### Kubernetes

Create a Secret:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: healthycron-secrets
type: Opaque
stringData:
  ConnectionStrings__DefaultConnection: "Host=..."
  Email__FromPassword: "..."
  AWS__SecretKey: "..."
```

Reference in your Deployment:

```yaml
envFrom:
  - secretRef:
      name: healthycron-secrets
```

---

## Configuration Validation

The application validates all configuration at startup using Data Annotations.

### Validation Rules

| Setting | Validation |
|---------|-----------|
| `ConnectionStrings:DefaultConnection` | Required |
| `Email:SmtpHost` | Required |
| `Email:SmtpPort` | Required, must be 1-65535 |
| `Email:FromEmail` | Required, must be valid email |
| `Email:FromPassword` | Required |
| `Redis:ConnectionString` | Required |
| `AWS:Region` | Required |
| `AWS:AccessKey` | Required |
| `AWS:SecretKey` | Required |
| `QueueSettings:HeartbeatQueueUrl` | Required, must be valid URL |

### Example Error Messages

If configuration is missing or invalid, you'll see clear error messages:

```
Configuration validation failed for section 'Email':
  - FromEmail is required
  - FromPassword is required
```

```
Configuration validation failed for section 'ConnectionStrings':
  - DefaultConnection is required
```

---

## Troubleshooting

### Application Won't Start

**Error:** `Configuration validation failed for section 'Email'`

**Solution:** Ensure all required Email settings are configured in user secrets or environment variables.

### Can't Find User Secrets

**Error:** User secrets not being loaded

**Solution:** 
1. Verify `<UserSecretsId>` exists in `HealthyCron.csproj`
2. Run `dotnet user-secrets init` if missing
3. Ensure you're running in Development environment

### Environment Variables Not Loading

**Solution:**
1. Verify environment variable names use double underscores: `Email__SmtpHost`
2. Restart your application/container after setting variables
3. Check environment with: `printenv | grep Email`

### Connection String Format

For PostgreSQL, use this format:
```
Host=hostname;Port=5432;Database=dbname;Username=user;Password=pass;SSL Mode=Require
```

For Redis with SSL:
```
rediss://default:password@hostname:6379,abortConnect=false
```

---

## Security Best Practices

1. ✅ **Never commit secrets** to source control
2. ✅ **Use User Secrets** for local development
3. ✅ **Use Environment Variables** in production
4. ✅ **Rotate credentials** regularly
5. ✅ **Use app-specific passwords** for email (not your main password)
6. ✅ **Enable SSL/TLS** for all external connections
7. ✅ **Use IAM roles** instead of AWS keys when possible (EC2, ECS, Lambda)

---

## Example: Complete Local Setup

```bash
# 1. Clone the repository
git clone <repo-url>
cd healthycron

# 2. Initialize user secrets
dotnet user-secrets init

# 3. Configure all secrets
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=healthycron;Username=postgres;Password=dev123"
dotnet user-secrets set "Email:FromEmail" "dev@example.com"
dotnet user-secrets set "Email:FromPassword" "app-password"
dotnet user-secrets set "Redis:ConnectionString" "redis://localhost:6379"
dotnet user-secrets set "AWS:Region" "ap-south-1"
dotnet user-secrets set "AWS:AccessKey" "YOUR_KEY"
dotnet user-secrets set "AWS:SecretKey" "YOUR_SECRET"
dotnet user-secrets set "QueueSettings:HeartbeatQueueUrl" "https://sqs.ap-south-1.amazonaws.com/123/queue"

# 4. Run the application
dotnet run
```

If all configuration is valid, the application will start successfully! 🎉
