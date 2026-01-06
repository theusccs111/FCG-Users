using FCG.Shared.EventLog.Contracts;

namespace FCG_Users.Domain.Users.SourcingEvents
{
    public class UserCreatedEventLog(Guid UserId, string Name, string Email, string PasswordHash, string Profile, bool Active) : EventLogMessage
    {
        public Guid UserId { get; private set; } = UserId;
        public string Name { get; private set; } = Name;
        public string Email { get; private set; } = Email;
        public string PasswordHash { get; private set; } = PasswordHash;
        public string Profile { get; private set; } = Profile;
        public bool Active { get; private set; } = Active;
    }
}
