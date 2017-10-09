using System;

namespace Core.Mappers
{
    public class HistoryOperationMapper<TResult, TCashOperation, TCashOutAttemptOperation,
       TClientTrade, TTransferEvent> : IHistoryOperationMapper<TResult, HistoryOperationSourceData>
       where TCashOperation : TResult
       where TCashOutAttemptOperation : TResult
       where TClientTrade : TResult
       where TTransferEvent : TResult
    {
        #region Operation Type Consts

        private const string CashInOut = "CashInOut";
        private const string CashOutAttempt = "CashOutAttempt";
        private const string Transfer = "TransferEvent";
        private const string ClientTrade = "ClientTrade";

        #endregion

        private readonly IHistoryOperationMapProvider _mapProvider;

        public HistoryOperationMapper(IHistoryOperationMapProvider mapProvider)
        {
            _mapProvider = mapProvider;
        }

        public TResult Map(HistoryOperationSourceData source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            switch (source.OperationType)
            {
                case CashInOut:
                    return new HistoryOperationJsonDeserializer<TCashOperation>()
                        .Deserialize(source.JsonData, _mapProvider.Cash);
                case CashOutAttempt:
                    return new HistoryOperationJsonDeserializer<TCashOutAttemptOperation>()
                        .Deserialize(source.JsonData, _mapProvider.CashOutAttempt);
                case ClientTrade:
                    return new HistoryOperationJsonDeserializer<TClientTrade>()
                        .Deserialize(source.JsonData, _mapProvider.ClientTrade);
                case Transfer:
                    return new HistoryOperationJsonDeserializer<TTransferEvent>()
                        .Deserialize(source.JsonData, _mapProvider.TransferEvent);
                default:
                    throw new ArgumentException("Unknown operation type");
            }
        }
    }
}
