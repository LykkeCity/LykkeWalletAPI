using System;
using Lykke.Contracts.Operations;

namespace LykkeApi2.Models.Operations
{
    public class OperationModel
    {
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public OperationType Type { get; set; }
        public OperationStatus Status { get; set; }
    }

    public static class OperationModelExtensions
    {
        public static OperationModel ToApiModel(this Lykke.Service.Operations.Contracts.OperationModel src)
        {
            return new OperationModel
            {
                Created = src.Created,
                Id = src.Id,
                Status = src.Status,
                Type = src.Type
            };
        }
    }
}
