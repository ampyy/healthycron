
namespace HealthyCron.Utilities.Interface
{
    public interface IAxiomLogger
    {
        Task LogInfo(string message, Dictionary<string, object>? additionalData = null);
        Task LogWarn(string message, Dictionary<string, object>? additionalData = null);
        Task LogError(string message, Dictionary<string, object>? additionalData = null);
    }
}
