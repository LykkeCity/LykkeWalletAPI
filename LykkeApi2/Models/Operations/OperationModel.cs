using System;
using Lykke.Contracts.Operations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LykkeApi2.Models.Operations
{
    public class OperationModel
    {
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public OperationType Type { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
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
