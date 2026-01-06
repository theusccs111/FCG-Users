using FCG.Shared.EventLog.Publisher;
using FCG.Shared.EventService.Contracts.User;
using FCG.Shared.EventService.Publisher;
using FCG_Users.Application.Shared.Interfaces;
using FCG_Users.Application.Shared.Repositories;
using FCG_Users.Application.Shared.Results;
using FCG_Users.Application.Users.Requests;
using FCG_Users.Application.Users.Responses;
using FCG_Users.Domain.Users.Entities;
using FCG_Users.Domain.Users.Enums;
using FCG_Users.Domain.Users.SourcingEvents;
using Fgc.Domain.Usuario.ObjetosDeValor;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FCG_Users.Application.Users.Services
{
    public class AccountService(IAccountRepository repository,
        IJwtTokenService jwtService,
        IEventServicePublisher publisher,
        IEventLogPublisher eventLogPublisher,
        IValidator<AccountRequest> validator,
        IConfiguration configuration,
        ILogger<AccountService> logger) : IAccountService
    {
        public async Task<Result<AuthResponse>> AuthAsync(
            AuthRequest request,
            CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Iniciando autenticação para o email: {Email}", request.Email);

            var email = Email.Create(request.Email);

            var conta = await repository.Auth(email, cancellationToken);
            if (conta is null)
            {
                logger.LogWarning("Falha de autenticação: conta não encontrada para o email {Email}", request.Email);
                return Result.Failure<AuthResponse>(new Error("401", "Credenciais inválidas."));
            }

            var passwordValid = Password.CreateWithHash(conta.PasswordHash).Verify(request.Password);
            if (!passwordValid)
            {
                logger.LogWarning("Falha de autenticação: senha inválida para o email {Email}, Senha {Password}, Hash {PasswordHash}", request.Email, request.Password, conta.PasswordHash);
                return Result.Failure<AuthResponse>(new Error("401", "Credenciais inválidas."));
            }

            if (!conta.Active)
            {
                logger.LogWarning("Tentativa de login com usuário inativo. Email: {Email}", request.Email);
                return Result.Failure<AuthResponse>(new Error("403", "Usuário inativo."));
            }

            logger.LogInformation("Credenciais validadas com sucesso para o email {Email}", request.Email);

            var tokenInfo = jwtService.CreateToken(conta);

            logger.LogInformation("Token gerado para o email {Email}. Expira em {ExpiresAt}",
                request.Email,
                tokenInfo.ExpiresAt);

            var response = new AuthResponse(tokenInfo.Token, tokenInfo.ExpiresAt);

            logger.LogInformation("Autenticação finalizada com sucesso para o email {Email}", request.Email);

            return Result.Success(response);
        }

        public async Task<Result> CreateAccountAsync(AccountRequest request, CancellationToken cancellationToken = default)
        {
            var validation = validator.Validate(request);
            if (!validation.IsValid)
                return Result.Failure(new Error("400", string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

            var userExists = await repository.Exists(request.Email, cancellationToken);

            if (userExists)
                return Result.Failure(new Error("409", "Este usuário já está cadastrado."));

            var account = Account.Create(request.Name, request.Password, request.Email, EProfileType.Common);

            var eventLog = new UserCreatedEventLog(account.Id, account.Name, account.Email, account.PasswordHash, account.Profile.ToString(), account.Active);
            await eventLogPublisher.PublishAsync(eventLog);

            var eventService = new UserCreatedEvent(account.Id, account.Name, account.Email, account.PasswordHash, account.Profile.ToString(), account.Active);
            await publisher.PublishAsync(eventService, configuration["ServiceBus:Queues:UsersEvents"]!, "UserCreated");

            return Result.Success();
        }

        public async Task<Result<AccountResponse>> GetUserAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var user = await repository.GetByIdAsync(id, cancellationToken);
            if (user is null)
                return Result.Failure<AccountResponse>(new Error("404", "Usuário não encontrado."));

            return Result.Success(new AccountResponse(
                user.Id,
                user.Name,
                user.PasswordHash,
                user.Email,
                user.Profile,
                user.Active
                ));            
        }

        public async Task<Result> RemoveUserAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var usuario = await repository.GetByIdAsync(id, cancellationToken);

            if (usuario is null)
                return Result.Failure(new Error("404", "Usuário não encontrado"));

            var eventLog = new UserDeletedEventLog(usuario.Id);
            await eventLogPublisher.PublishAsync(eventLog);
            
            var evt = new UserDeletedEvent(usuario.Id);
            await publisher.PublishAsync(evt, configuration["ServiceBus:Queues:UsersEvents"]!, "UserDeleted");

            return Result.Success();
        }
    }
}
