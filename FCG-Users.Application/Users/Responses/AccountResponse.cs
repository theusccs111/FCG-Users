using FCG_Users.Domain.Users.Enums;
using Fgc.Domain.Usuario.ObjetosDeValor;

namespace FCG_Users.Application.Users.Responses
{
    public sealed record AccountResponse(Guid Id, string Name, string PasswordHash, Email Email, EProfileType ProfileType, bool Active);
}
