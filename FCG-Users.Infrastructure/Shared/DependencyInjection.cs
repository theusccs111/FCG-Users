using Azure.Messaging.ServiceBus;
using FCG.Shared.EventLog.Publisher;
using FCG.Shared.EventLog.Publisher.MongoDB;
using FCG.Shared.EventService.Publisher;
using FCG.Shared.EventService.Publisher.ServiceBus;
using FCG_Users.Application.Shared.Interfaces;
using FCG_Users.Application.Shared.Repositories;
using FCG_Users.Infrastructure.Shared.Context;
using FCG_Users.Infrastructure.Shared.Services;
using FCG_Users.Infrastructure.Users.Repositorories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace FCG_Users.Infrastructure.Shared
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<UsersDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            var connectionString = configuration["ServiceBus:ConnectionString"];
            var connectionStringMongodb = configuration["Mongodb:ConnectionString"];

            services.AddSingleton(new ServiceBusClient(connectionString));

            services.AddScoped<IEventServicePublisher>(sp =>
            {
                var client = sp.GetRequiredService<ServiceBusClient>();
                return new ServiceBusEventPublisher(client);
            });

            services.AddSingleton(new MongoClient(connectionStringMongodb));

            services.AddScoped<IEventLogPublisher>(sp =>
            {
                var client = sp.GetRequiredService<MongoClient>();
                var databaseName = configuration["Mongodb:DatabaseName"];
                var collectionName = configuration["Mongodb:CollectionName"];

                return new MongoDBEventLogPublisher(client, databaseName!, collectionName!);
            });

            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddSingleton<IJwtTokenService, JwtTokenService>();

            return services;
        }
    }
}
