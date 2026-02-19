using Dapper;
using HealthyCron.Data.Interfaces;
using HealthyCron.Enums;
using HealthyCron.Models;
using HealthyCron.Utilities.Interface;

namespace HealthyCron.Data.Repository
{
    public class ProjectMemberRepository : BaseRepository, IProjectMemberRepository
    {
        public ProjectMemberRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

        public async Task<ProjectMember?> GetMemberAsync(Guid projectId, Guid userId)
        {
            const string sql = @"
                SELECT pm.project_id, pm.user_id, pm.role, pm.joined_at, u.email as UserEmail
                FROM project_members pm
                JOIN users u ON u.id = pm.user_id
                WHERE pm.project_id = @ProjectId AND pm.user_id = @UserId";
            return await QueryFirstOrDefaultAsync<ProjectMember>(sql, new { ProjectId = projectId, UserId = userId });
        }

        public async Task<IEnumerable<ProjectMember>> GetMembersAsync(Guid projectId)
        {
            const string sql = @"
                SELECT pm.project_id, pm.user_id, pm.role, pm.joined_at, u.email as UserEmail
                FROM project_members pm
                JOIN users u ON u.id = pm.user_id
                WHERE pm.project_id = @ProjectId
                ORDER BY pm.joined_at ASC";
            return await QueryAsync<ProjectMember>(sql, new { ProjectId = projectId });
        }

        public async Task AddMemberAsync(Guid projectId, Guid userId, ProjectRole role)
        {
            const string sql = @"
                INSERT INTO project_members (project_id, user_id, role)
                VALUES (@ProjectId, @UserId, @Role)
                ON CONFLICT (project_id, user_id) DO UPDATE SET role = @Role";
            await ExecuteAsync(sql, new { ProjectId = projectId, UserId = userId, Role = (int)role });
        }

        public async Task RemoveMemberAsync(Guid projectId, Guid userId)
        {
            const string sql = "DELETE FROM project_members WHERE project_id = @ProjectId AND user_id = @UserId";
            await ExecuteAsync(sql, new { ProjectId = projectId, UserId = userId });
        }

        public async Task<ProjectInvitation?> GetInvitationByTokenAsync(string token)
        {
            const string sql = "SELECT * FROM project_invitations WHERE token = @Token";
            return await QueryFirstOrDefaultAsync<ProjectInvitation>(sql, new { Token = token });
        }

        public async Task<IEnumerable<ProjectInvitation>> GetPendingInvitationsAsync(Guid projectId)
        {
            const string sql = @"
                SELECT * FROM project_invitations
                WHERE project_id = @ProjectId
                  AND accepted_at IS NULL
                  AND expires_at > NOW()
                ORDER BY created_at DESC";
            return await QueryAsync<ProjectInvitation>(sql, new { ProjectId = projectId });
        }

        public async Task<Guid> CreateInvitationAsync(ProjectInvitation invitation)
        {
            if (invitation.Id == Guid.Empty) invitation.Id = Guid.NewGuid();
            const string sql = @"
                INSERT INTO project_invitations (id, project_id, email, role, token, expires_at, created_at)
                VALUES (@Id, @ProjectId, @Email, @Role, @Token, @ExpiresAt, @CreatedAt)
                RETURNING id";
            return await ExecuteScalarAsync<Guid>(sql, new
            {
                invitation.Id,
                invitation.ProjectId,
                invitation.Email,
                Role = (int)invitation.Role,
                invitation.Token,
                invitation.ExpiresAt,
                CreatedAt = DateTime.UtcNow
            });
        }

        public async Task AcceptInvitationAsync(Guid invitationId, DateTime acceptedAt)
        {
            const string sql = "UPDATE project_invitations SET accepted_at = @AcceptedAt WHERE id = @Id";
            await ExecuteAsync(sql, new { Id = invitationId, AcceptedAt = acceptedAt });
        }

        public async Task<IEnumerable<ProjectMember>> GetAllMembersForOwnerAsync(Guid ownerUserId)
        {
            const string sql = @"
                SELECT pm.project_id, pm.user_id, pm.role, pm.joined_at,
                       u.email as UserEmail,
                       p.name  as ProjectName,
                       p.slug  as ProjectSlug
                FROM project_members pm
                JOIN users u    ON u.id  = pm.user_id
                JOIN projects p ON p.id  = pm.project_id
                WHERE p.user_id = @OwnerUserId
                  AND p.is_deleted = FALSE
                ORDER BY p.name, pm.joined_at ASC";
            return await QueryAsync<ProjectMember>(sql, new { OwnerUserId = ownerUserId });
        }
    }
}
