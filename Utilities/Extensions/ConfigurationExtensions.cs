using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyCron.Utilities.Extensions
{
    /// <summary>
    /// Extension methods for IConfiguration and IServiceCollection
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Binds a configuration section to a strongly-typed object, validates it, and registers it as a singleton
        /// </summary>
        /// <typeparam name="T">Type of configuration object</typeparam>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration root</param>
        /// <param name="sectionName">Name of the configuration section</param>
        /// <returns>The bound and validated configuration object</returns>
        public static T AddValidatedConfiguration<T>(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName) where T : class, new()
        {
            // Bind the configuration section to the object
            var configObject = new T();
            var section = configuration.GetSection(sectionName);

            // DEBUG: Log configuration section existence and values
            Console.WriteLine($"=== Configuration Debug for '{sectionName}' ===");
            Console.WriteLine($"Section exists: {section.Exists()}");
            Console.WriteLine($"Section path: {section.Path}");

            // Log all children in the section
            var children = section.GetChildren().ToList();
            Console.WriteLine($"Number of children: {children.Count}");
            foreach (var child in children)
            {
                Console.WriteLine($"  {child.Key} = {child.Value ?? "(null)"}");
            }

            section.Bind(configObject);

            // DEBUG: Log the bound object properties using reflection
            Console.WriteLine($"Bound object properties:");
            foreach (var prop in typeof(T).GetProperties())
            {
                var value = prop.GetValue(configObject);
                Console.WriteLine($"  {prop.Name} = {value ?? "(null)"}");
            }
            Console.WriteLine($"=== End Configuration Debug ===\n");

            // Validate the configuration
            ConfigurationValidator.ValidateAndThrow(configObject, sectionName);

            // Register as singleton so it can be injected
            services.AddSingleton(configObject);

            return configObject;
        }

        /// <summary>
        /// Gets a required configuration value or throws an exception
        /// </summary>
        /// <param name="configuration">Configuration instance</param>
        /// <param name="key">Configuration key</param>
        /// <returns>Configuration value</returns>
        /// <exception cref="InvalidOperationException">Thrown when the key is not found or value is null/empty</exception>
        public static string GetRequiredValue(this IConfiguration configuration, string key)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Required configuration value '{key}' is missing or empty.");
            }
            return value;
        }
    }
}
