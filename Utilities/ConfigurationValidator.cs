using System.ComponentModel.DataAnnotations;

namespace HealthyCron.Utilities
{
    /// <summary>
    /// Validates configuration objects using Data Annotations
    /// </summary>
    public static class ConfigurationValidator
    {
        /// <summary>
        /// Validates a configuration object and throws an exception if validation fails
        /// </summary>
        /// <typeparam name="T">Type of configuration object</typeparam>
        /// <param name="config">Configuration object to validate</param>
        /// <param name="sectionName">Name of the configuration section (for error messages)</param>
        /// <exception cref="InvalidOperationException">Thrown when validation fails</exception>
        public static void ValidateAndThrow<T>(T config, string sectionName) where T : class
        {
            var validationContext = new ValidationContext(config);
            var validationResults = new List<ValidationResult>();

            bool isValid = Validator.TryValidateObject(config, validationContext, validationResults, validateAllProperties: true);

            if (!isValid)
            {
                var errors = validationResults
                    .Select(r => $"  - {r.ErrorMessage}")
                    .ToList();

                var errorMessage = $"Configuration validation failed for section '{sectionName}':\n{string.Join("\n", errors)}";
                throw new InvalidOperationException(errorMessage);
            }
        }

        /// <summary>
        /// Validates a configuration object and returns validation results
        /// </summary>
        /// <typeparam name="T">Type of configuration object</typeparam>
        /// <param name="config">Configuration object to validate</param>
        /// <returns>List of validation results (empty if valid)</returns>
        public static IList<ValidationResult> Validate<T>(T config) where T : class
        {
            var validationContext = new ValidationContext(config);
            var validationResults = new List<ValidationResult>();
            Validator.TryValidateObject(config, validationContext, validationResults, validateAllProperties: true);
            return validationResults;
        }
    }
}
