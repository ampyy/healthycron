using Dapper;
using Npgsql;
using System;
using System.Threading.Tasks;

public class ProgramTest
{
    public static async Task Main()
    {
        string connStr = "Host=ep-purple-frog-a1a0l7c4-pooler.ap-southeast-1.aws.neon.tech;Port=5432;Database=neondb;Username=neondb_owner;Password=npg_k8TKISlYrc7Z;SSL Mode=Require";
        using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync();
        
        try {
            Console.WriteLine("Connected to DB");
            
            // 1. Create a pending subscription
            string token = Guid.NewGuid().ToString("N");
            Guid projectId = Guid.NewGuid();
            
            // First we need a user to own the project
            Guid userId = Guid.NewGuid();
            await conn.ExecuteAsync("INSERT INTO users (id, email, password_hash, status) VALUES (@Id, 'test-pushover@test.com', 'test', 1) ON CONFLICT DO NOTHING", new { Id = userId });
            userId = await conn.QuerySingleAsync<Guid>("SELECT id FROM users WHERE email='test-pushover@test.com' LIMIT 1");
            Console.WriteLine($"User ID: {userId}");
            
            // Create the project
            await conn.ExecuteAsync("INSERT INTO projects (id, user_id, name, slug) VALUES (@Id, @UserId, 'Test Project', 'test-project-" + Guid.NewGuid().ToString().Substring(0, 8) + "')", new { Id = projectId, UserId = userId });
            Console.WriteLine($"Project ID: {projectId}");
            
            var expiresAt = DateTime.UtcNow.AddHours(1);
            
            Console.WriteLine("Attempting to insert into pushover_pending_subscriptions...");
            const string sqlPending = @"
                INSERT INTO pushover_pending_subscriptions (token, project_id, created_at, expires_at)
                VALUES (@Token, @ProjectId, @CreatedAt, @ExpiresAt)";
            await conn.ExecuteAsync(sqlPending, new { Token = token, ProjectId = projectId, CreatedAt = DateTime.UtcNow, ExpiresAt = expiresAt });
            
            Console.WriteLine("Success: inserted into pushover_pending_subscriptions");
            
            // 2. Insert into integrations
            Console.WriteLine("Attempting to insert into integrations...");
            Guid integrationId = Guid.NewGuid();
            const string sqlInt = @"
                INSERT INTO integrations (id, project_id, type, name, is_active)
                VALUES (@Id, @ProjectId, @Type, @Name, @IsActive)
                RETURNING id";
            await conn.ExecuteScalarAsync<Guid>(sqlInt, new { 
                Id = integrationId, 
                ProjectId = projectId, 
                Type = 11, // Pushover 
                Name = "Pushover", 
                IsActive = true 
            });
            Console.WriteLine($"Success: inserted into integrations. ID: {integrationId}");
            
            // 3. Insert into pushover_integrations
            Console.WriteLine("Attempting to insert into pushover_integrations...");
            const string sqlPush = @"
                INSERT INTO pushover_integrations (integration_id, subscription_key, device, sound)
                VALUES (@IntegrationId, @SubscriptionKey, @Device, @Sound)";
            await conn.ExecuteAsync(sqlPush, new { 
                IntegrationId = integrationId, 
                SubscriptionKey = "test_key", 
                Device = (string?)null, 
                Sound = (string?)null 
            });
            Console.WriteLine("Success: inserted into pushover_integrations");
            
            // Cleanup
            await conn.ExecuteAsync("DELETE FROM projects WHERE id = @Id", new { Id = projectId });
            Console.WriteLine("Cleanup succesful.");
            
        } catch (Exception ex) {
            Console.WriteLine("CRITICAL ERROR: " + ex.Message);
            if (ex.InnerException != null) {
                Console.WriteLine("Inner Exception: " + ex.InnerException.Message);
            }
        }
    }
}
