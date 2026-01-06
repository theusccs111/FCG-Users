using FCG.Shared.EventService.Consumer;

namespace FCG_Users.Consumer
{
    public class Worker(IServiceScopeFactory scopeFactory, IConfiguration configuration) : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly string _queueName = configuration["ServiceBus:Queues:UsersEvents"]!;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var consumer = scope.ServiceProvider.GetRequiredService<IQueueConsumer>();

            await consumer.ProcessQueueMessages(_queueName, stoppingToken);
        }
    }
}
