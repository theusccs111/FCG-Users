using FCG.Shared.EventService.Consumer;
using FCG.Shared.EventService.Contracts.User;
using FCG_Users.Application.Shared.Repositories;
using System.Text.Json;

namespace FCG_Users.Application.Users.Handlers
{
    public class UserDeletedHandler(IAccountRepository repository) : IMessageHandler
    {
        public string MessageType => "UserDeleted";

        public async Task HandleAsync(string message, CancellationToken cancellationToken)
        {
            UserDeletedEvent userDeletedEvent = JsonSerializer.Deserialize<UserDeletedEvent>(message)!;

            var account = await repository.GetByIdAsync(userDeletedEvent.UserId);
            if (account is null)
                return;

            await repository.DeleteAsync(userDeletedEvent.UserId, cancellationToken);
        }
    }
}
