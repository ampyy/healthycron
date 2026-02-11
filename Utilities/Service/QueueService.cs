using Amazon.SQS;
using Amazon.SQS.Model;
using HealthyCron.Models.Configuration;
using HealthyCron.Utilities.Interface;
using Newtonsoft.Json;

namespace HealthyCron.Utilities.Service
{
    public class QueueService : IQueueService
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly string _queueUrl;

        public QueueService(IAmazonSQS sqsClient, QueueSettings queueSettings)
        {
            _sqsClient = sqsClient;
            _queueUrl = queueSettings.HeartbeatQueueUrl;
        }

        public async Task SendMessageAsync(object message)
        {
            var jsonMessage = JsonConvert.SerializeObject(message);
            var sendRequest = new SendMessageRequest
            {
                QueueUrl = _queueUrl,
                MessageBody = jsonMessage
            };

            await _sqsClient.SendMessageAsync(sendRequest);
        }
    }
}
