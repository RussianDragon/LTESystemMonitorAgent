namespace LTESM.DAL.Abstractions.Entities;

public enum OutboxMessageStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2
}
