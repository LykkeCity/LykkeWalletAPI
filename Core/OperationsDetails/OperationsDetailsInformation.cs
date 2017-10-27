using System;

namespace Core.OperationsDetails
{
    public class OperationsDetailsInformation : IOperationsDetailsInformation
    {
        public string Id { get; set; }
        public string Comment { get; set; }
        public string ClientId { get; set; }
        public string TransactionId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
