
namespace HealthyCron.Utilities.Interface
{
    public interface IQueueService
    {
        Task SendMessageAsync(object message);
    }
}
