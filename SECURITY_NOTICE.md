# ⚠️ IMPORTANT: Secrets in appsettings.Development.json

## Current Status

The file `appsettings.Development.json` currently contains **REAL SECRETS** including:

- Database password
- Email password  
- Redis connection string with password
- AWS Access Key and Secret Key

## Action Required

### Option 1: Move to User Secrets (Recommended)

1. **Initialize user secrets:**
   ```bash
   dotnet user-secrets init
   ```

2. **Copy secrets to user secrets:**
   ```bash
   # Database
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=ep-purple-frog-a1a0l7c4-pooler.ap-southeast-1.aws.neon.tech;Port=5432;Database=neondb;Username=neondb_owner;Password=npg_k8TKISlYrc7Z;SSL Mode=Require"

   # Email
   dotnet user-secrets set "Email:FromEmail" "thesheetsbackend@gmail.com"
   dotnet user-secrets set "Email:FromPassword" "ynvd nzbu ejto rvpo"

   # Redis
   dotnet user-secrets set "Redis:ConnectionString" "rediss://default:AbwNAAIncDE1YjAyNTBkYmNhMGQ0M2FmODA5NjkyYjQ2MDEzY2NiY3AxNDgxNDE@above-ghost-48141.upstash.io:6379,abortConnect=false"

   # AWS
   dotnet user-secrets set "AWS:Region" "ap-south-1"
   dotnet user-secrets set "AWS:AccessKey" "AKIAYDR22MDCPVNFDN7R"
   dotnet user-secrets set "AWS:SecretKey" "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"

   # SQS
   dotnet user-secrets set "QueueSettings:HeartbeatQueueUrl" "https://sqs.ap-south-1.amazonaws.com/557395370180/healthycron-heartbeats"
   ```

3. **Remove secrets from appsettings.Development.json:**

   Replace the content with:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     }
   }
   ```

4. **Verify it works:**
   ```bash
   dotnet run
   ```

### Option 2: Keep in appsettings.Development.json

If you prefer to keep secrets in the file:

1. **Ensure it's in .gitignore** (it's currently NOT ignored by default)
2. **Add to .gitignore:**
   ```bash
   echo "appsettings.Development.json" >> .gitignore
   ```
3. **Remove from git tracking:**
   ```bash
   git rm --cached appsettings.Development.json
   git commit -m "Remove appsettings.Development.json from tracking"
   ```

## Security Recommendations

### ⚠️ Exposed Credentials

The following credentials are currently exposed in your repository:

1. **Database Password:** `npg_k8TKISlYrc7Z`
   - **Action:** Rotate this password in your Neon database
   
2. **Email App Password:** `ynvd nzbu ejto rvpo`
   - **Action:** Revoke and generate a new Gmail app password
   
3. **Redis Password:** `AbwNAAIncDE1YjAyNTBkYmNhMGQ0M2FmODA5NjkyYjQ2MDEzY2NiY3AxNDgxNDE`
   - **Action:** Rotate this password in Upstash
   
4. **AWS Access Key:** `AKIAYDR22MDCPVNFDN7R`
5. **AWS Secret Key:** `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`
   - **Action:** Deactivate these keys and create new ones in AWS IAM

### Immediate Actions

1. ✅ **Move secrets to user secrets** (Option 1 above)
2. ✅ **Rotate all exposed credentials**
3. ✅ **Check git history** for exposed secrets
4. ✅ **Consider using git-secrets** to prevent future commits of secrets

### Long-term Best Practices

- 🔐 Use user secrets for local development
- 🔐 Use environment variables for production
- 🔐 Never commit secrets to source control
- 🔐 Use secret scanning tools (GitHub Advanced Security, GitGuardian, etc.)
- 🔐 Rotate credentials regularly
- 🔐 Use IAM roles instead of AWS keys when possible

## Questions?

See [CONFIGURATION.md](./CONFIGURATION.md) for detailed instructions on using user secrets and environment variables.
