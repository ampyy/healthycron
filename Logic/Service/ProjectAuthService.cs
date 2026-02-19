using HealthyCron.Data.Interfaces;
using HealthyCron.Enums;
using HealthyCron.Logic.Interfaces;

namespace HealthyCron.Logic.Service
{
    public class ProjectAuthService : IProjectAuthService
    {
        private readonly IProjectMemberRepository _memberRepo;

        public ProjectAuthService(IProjectMemberRepository memberRepo)
        {
            _memberRepo = memberRepo;
        }

        public async Task<ProjectRole?> GetMemberRoleAsync(Guid projectId, Guid userId)
        {
            var member = await _memberRepo.GetMemberAsync(projectId, userId);
            return member?.Role;
        }

        public bool IsOwner(Guid projectOwnerId, Guid userId) => projectOwnerId == userId;

        public async Task<bool> CanViewProjectAsync(Guid projectId, Guid projectOwnerId, Guid userId)
        {
            if (IsOwner(projectOwnerId, userId)) return true;
            var member = await _memberRepo.GetMemberAsync(projectId, userId);
            return member != null;
        }

        public async Task<bool> CanManageMonitorsAsync(Guid projectId, Guid projectOwnerId, Guid userId)
        {
            if (IsOwner(projectOwnerId, userId)) return true;
            var role = await GetMemberRoleAsync(projectId, userId);
            if (role == null) return false;
            // TeamMember (0) and Manager (1) can manage; ReadOnly (2) cannot
            return role == ProjectRole.TeamMember || role == ProjectRole.Manager;
        }

        public async Task<bool> CanManageMembersAsync(Guid projectId, Guid projectOwnerId, Guid userId)
        {
            if (IsOwner(projectOwnerId, userId)) return true;
            var role = await GetMemberRoleAsync(projectId, userId);
            if (role == null) return false;
            // Only Manager can manage members (not TeamMember, not ReadOnly)
            return role == ProjectRole.Manager;
        }

        public bool CanDeleteProject(Guid projectOwnerId, Guid userId) => IsOwner(projectOwnerId, userId);
    }
}
