using HealthyCron.Enums;

namespace HealthyCron.Logic.Interfaces
{
    public interface IProjectAuthService
    {
        /// <summary>
        /// Returns the effective role for a user on a project.
        /// Returns null if user has no access (not owner, not member).
        /// Owner (ownerId == userId) always returns null from project_members but has full rights — check IsOwner first.
        /// </summary>
        Task<ProjectRole?> GetMemberRoleAsync(Guid projectId, Guid userId);

        /// <summary>True if userId == project.UserId (the owner).</summary>
        bool IsOwner(Guid projectOwnerId, Guid userId);

        /// <summary>
        /// Can the user view this project at all?
        /// Owner or any project_members entry.
        /// </summary>
        Task<bool> CanViewProjectAsync(Guid projectId, Guid projectOwnerId, Guid userId);

        /// <summary>Can create, edit, or delete monitors and manage integrations.</summary>
        Task<bool> CanManageMonitorsAsync(Guid projectId, Guid projectOwnerId, Guid userId);

        /// <summary>Can invite or remove members.</summary>
        Task<bool> CanManageMembersAsync(Guid projectId, Guid projectOwnerId, Guid userId);

        /// <summary>Owner-only actions: delete project, manage API keys, access billing.</summary>
        bool CanDeleteProject(Guid projectOwnerId, Guid userId);
    }
}
