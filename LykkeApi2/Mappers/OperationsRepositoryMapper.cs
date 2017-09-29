using AutoMapper;
using Lykke.Service.OperationsRepository.AutorestClient.Models;
using System;
using CashInOutOperation = Lykke.Service.OperationsRepository.AutorestClient.Models.CashInOutOperation;
using CashOutAttemptEntity = Lykke.Service.OperationsRepository.AutorestClient.Models.CashOutAttemptEntity;

namespace LykkeApi2.Mappers
{
    public class OperationsRepositoryMapper
    {
        private static IMapper _mapper;

        public static IMapper Instance
        {
            get
            {
                if (_mapper != null)
                {
                    return _mapper;
                }

                var config = new MapperConfiguration(cfg =>
                {
                    cfg.CreateMap<CashInOutOperation, Core.CashOperations.CashInOutOperation>();
                    cfg.CreateMap<CashOutAttemptEntity, Core.CashOperations.SwiftCashOutRequest>();
                    cfg.CreateMap<Core.CashOperations.SwiftCashOutRequest, CashOutAttemptEntity>()
                        .ForMember(dest => dest.StateVal, opt => opt.Ignore())
                        .ForMember(dest => dest.StatusVal, opt => opt.Ignore())
                        .ForMember(dest => dest.PartitionKey, opt => opt.Ignore())
                        .ForMember(dest => dest.RowKey, opt => opt.Ignore())
                        .ForMember(dest => dest.Timestamp,
                            opt => opt.UseValue(DateTime.MinValue))
                        .ForMember(dest => dest.ETag, opt => opt.Ignore())
                        .ForMember(dest => dest.VolumeText, opt => opt.Ignore());
                    cfg.CreateMap<ClientTrade, Core.CashOperations.ClientTrade>();
                    cfg.CreateMap<TransferEvent, Core.CashOperations.TransferEvent>();
                    cfg.CreateMap<LimitTradeEvent, Core.CashOperations.LimitTradeEvent>();
                    cfg.CreateMap<LimitOrder, Core.Exchange.LimitOrder>();
                    cfg.CreateMap<MarketOrder, Core.Exchange.MarketOrder>();
                });

                config.AssertConfigurationIsValid();

                _mapper = config.CreateMapper();

                return _mapper;
            }
        }
    }
}
