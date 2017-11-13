using System;

namespace Core.OperationsDetails
{
    public interface IOperationsDetailsInformation
    {
        string Id { get; set; }
        string TransactionId { get; set; }
        string ClientId { get; set; }
        DateTime CreatedAt { get; set; }
        string Comment { get; set; }
    }
}
