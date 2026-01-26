using System.Text.RegularExpressions;

namespace HealthyCron.Logic.Service
{
    public class ProjectService
    {
        public string GenerateSlug(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;

            // Normalize to lowercase
            string slug = name.ToLowerInvariant();

            // Replace spaces with hyphens
            slug = Regex.Replace(slug, @"\s+", "-");

            // Remove invalid chars (keep only a-z, 0-9, hyphens)
            slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");

            // Remove multiple hyphens
            slug = Regex.Replace(slug, @"\-+", "-");

            // Trim hyphens from ends
            slug = slug.Trim('-');

            return slug;
        }

        // We can add logic here to handle slug uniqueness by appending counts if needed
        // For now, simple slug generation is sufficient
    }
}
