using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LykkeApi2.Mappers
{
    public class PledgesMapper
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
                    cfg.CreateMap<Lykke.Service.Pledges.Client.AutorestClient.Models.CreatePledgeRequest, LykkeApi2.Models.Pledges.CreatePledgeRequest>().ReverseMap();
                    cfg.CreateMap<Lykke.Service.Pledges.Client.AutorestClient.Models.CreatePledgeResponse, LykkeApi2.Models.Pledges.CreatePledgeResponse>().ReverseMap();
                    cfg.CreateMap<Lykke.Service.Pledges.Client.AutorestClient.Models.GetPledgeResponse, LykkeApi2.Models.Pledges.GetPledgeResponse>().ReverseMap();
                    cfg.CreateMap<Lykke.Service.Pledges.Client.AutorestClient.Models.UpdatePledgeRequest, LykkeApi2.Models.Pledges.UpdatePledgeRequest>().ReverseMap();
                    cfg.CreateMap<Lykke.Service.Pledges.Client.AutorestClient.Models.UpdatePledgeResponse, LykkeApi2.Models.Pledges.UpdatePledgeResponse>().ReverseMap();
                });

                config.AssertConfigurationIsValid();

                _mapper = config.CreateMapper();

                return _mapper;
            }
        }
    }
}
