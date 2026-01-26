using Amazon.SQS;
using Amazon.SQS.Model;
using HealthyCron.Utilities.Interface;
using System.Text.Json;

namespace HealthyCron.Utilities.Service
{
    public class QueueService : IQueueService
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly string _queueUrl;

        public QueueService(IAmazonSQS sqsClient, IConfiguration configuration)
        {
            _sqsClient = sqsClient;
            _queueUrl = configuration["QueueSettings:HeartbeatQueueUrl"]
                ?? throw new InvalidOperationException("QueueSettings:HeartbeatQueueUrl is missing in configuration.");
        }

        public async Task SendMessageAsync(object message)
        {
            var jsonMessage = JsonSerializer.Serialize(message);
            var sendRequest = new SendMessageRequest
            {
                QueueUrl = _queueUrl,
                MessageBody = jsonMessage
            };

            await _sqsClient.SendMessageAsync(sendRequest);
        }
    }
}
