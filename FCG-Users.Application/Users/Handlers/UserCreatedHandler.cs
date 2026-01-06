using FCG.Shared.EventService.Consumer;
using FCG.Shared.EventService.Contracts.User;
using FCG.Shared.EventService.Publisher;
using FCG_Users.Application.Shared.Repositories;
using FCG_Users.Domain.Users.Entities;
using FCG_Users.Domain.Users.Enums;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace FCG_Users.Application.Users.Handlers
{
    public class UserCreatedHandler(
        IAccountRepository repository,
        IEventServicePublisher publisher,
        IConfiguration configuration) : IMessageHandler
    {
        public string MessageType => "UserCreated";

        public async Task HandleAsync(string message, CancellationToken cancellationToken)
        {
            UserCreatedEvent userCreatedEvent = JsonSerializer.Deserialize<UserCreatedEvent>(message)!;

            var account = Account.Create(
                userCreatedEvent.UserId,
                userCreatedEvent.Name,
                userCreatedEvent.PasswordHash,
                userCreatedEvent.Email,
                (EProfileType)Enum.Parse(typeof(EProfileType), userCreatedEvent.Profile, true));

            await repository.CreateAsync(account, cancellationToken);

            var eventLog = new UserCreatedEmailEvent(account.Id, account.Name, account.Email);
            await publisher.PublishAsync(eventLog, configuration["ServiceBus:Queues:EmailEvents"]!, configuration["ServiceBus:Queues:EmailEvents"]!);
        }
    }
}
