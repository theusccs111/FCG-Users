using Azure.Messaging.ServiceBus;
using FCG.Shared.EventService.Consumer;
using FCG.Shared.EventService.Consumer.ServiceBus;
using FCG.Shared.EventService.Publisher;
using FCG.Shared.EventService.Publisher.ServiceBus;
using FCG_Users.Application.Shared.Repositories;
using FCG_Users.Application.Users.Handlers;
using FCG_Users.Infrastructure.Shared.Context;
using FCG_Users.Infrastructure.Users.Repositorories;
using Microsoft.EntityFrameworkCore;

namespace FCG_Users.Consumer
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddConsumerServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<UsersDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            var connectionStringServiceBus = configuration["ServiceBus:ConnectionString"];
            var queueName = configuration["ServiceBus:Queues:UsersEvents"];

            services.AddSingleton(new ServiceBusClient(connectionStringServiceBus));
            services.AddScoped<IEventServicePublisher>(sp =>
            {
                var client = sp.GetRequiredService<ServiceBusClient>();
                return new ServiceBusEventPublisher(client);
            });

            services.AddScoped<IAccountRepository, AccountRepository>();

            services.AddScoped<IMessageHandler, UserCreatedHandler>();
            services.AddScoped<IMessageHandler, UserDeletedHandler>();

            services.AddScoped<ServiceBusMessageDispatcher>();
            services.AddScoped<IQueueConsumer, QueueConsumer>();

            return services;
        }
    }
}
