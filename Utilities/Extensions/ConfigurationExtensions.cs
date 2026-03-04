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
            // (Removed to reduce console noise during startup)
            var children = section.GetChildren().ToList();

            section.Bind(configObject);
            
            // Register for IOptions<T> support
            services.Configure<T>(section);

            // DEBUG: Log the bound object properties using reflection
            // (Removed to reduce console noise during startup)

            // Validate the configuration
            ConfigurationValidator.ValidateAndThrow(configObject, sectionName);

            // Register as singleton so it can be injected directly
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
