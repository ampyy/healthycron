using HealthyCron.Enums;
using HealthyCron.Models;

namespace HealthyCron.Data.Interfaces
{
    public interface IProjectMemberRepository
    {
        Task<ProjectMember?> GetMemberAsync(Guid projectId, Guid userId);
        Task<IEnumerable<ProjectMember>> GetMembersAsync(Guid projectId);
        Task AddMemberAsync(Guid projectId, Guid userId, ProjectRole role);
        Task RemoveMemberAsync(Guid projectId, Guid userId);

        Task<ProjectInvitation?> GetInvitationByTokenAsync(string token);
        Task<IEnumerable<ProjectInvitation>> GetPendingInvitationsAsync(Guid projectId);
        Task<Guid> CreateInvitationAsync(ProjectInvitation invitation);
        Task AcceptInvitationAsync(Guid invitationId, DateTime acceptedAt);

        /// <summary>Returns all project_members rows (with ProjectName) for projects owned by ownerUserId.</summary>
        Task<IEnumerable<ProjectMember>> GetAllMembersForOwnerAsync(Guid ownerUserId);
    }
}
