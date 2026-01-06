using FCG.Shared.EventLog.Contracts;

namespace FCG_Users.Domain.Users.SourcingEvents
{
    public class UserDeletedEventLog(Guid UserId) : EventLogMessage
    {
        public Guid UserId { get; private set; } = UserId;
    }
}
