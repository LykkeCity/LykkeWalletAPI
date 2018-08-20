using AutoMapper;
using Core.Domain.Recovery;
using Core.Dto.Recovery;
using Lykke.Service.ClientAccountRecovery.Client.AutoRestClient.Models;
using LykkeApi2.Models.Recovery;

namespace LykkeApi2.Automapper
{
    internal class RecoveryAutomapperProfile : Profile
    {
        public RecoveryAutomapperProfile()
        {
            CreateMap<RecoveryStartRequestModel, RecoveryStartDto>()
                .ForMember(dest => dest.Ip,
                    opt => opt.ResolveUsing((src, dest, destMember, context) => context.Items["Ip"]))
                .ForMember(dest => dest.UserAgent,
                    opt => opt.ResolveUsing((src, dest, destMember, context) => context.Items["UserAgent"]));

            CreateMap<RecoveryStartDto, NewRecoveryRequest>()
                .ForMember(dest => dest.ClientId,
                    opt => opt.ResolveUsing((src, dest, destMember, context) => context.Items["ClientId"]));

            CreateMap<RecoveryStatusResponse, RecoveryStatus>();

            CreateMap<RecoveryStatus, RecoveryStatusResponseModel>();

            CreateMap<RecoverySubmitChallengeDto, ChallengeRequest>()
                .ForMember(dest => dest.Challenge,
                    opt => opt.ResolveUsing((src, dest, destMember, context) => context.Items["Challenge"]))
                .ForMember(dest => dest.RecoveryId,
                    opt => opt.ResolveUsing((src, dest, destMember, context) => context.Items["RecoveryId"]));

            CreateMap<RecoverySubmitChallengeRequestModel, RecoverySubmitChallengeDto>()
                .ForMember(dest => dest.Ip,
                    opt => opt.ResolveUsing((src, dest, destMember, context) => context.Items["Ip"]))
                .ForMember(dest => dest.UserAgent,
                    opt => opt.ResolveUsing((src, dest, destMember, context) => context.Items["UserAgent"]));

            CreateMap<RecoveryCompleteRequestModel, RecoveryCompleteDto>()
                .ForMember(dest => dest.Ip,
                    opt => opt.ResolveUsing((src, dest, destMember, context) => context.Items["Ip"]))
                .ForMember(dest => dest.UserAgent,
                    opt => opt.ResolveUsing((src, dest, destMember, context) => context.Items["UserAgent"]));


            CreateMap<RecoveryCompleteDto, PasswordRequest>()
                .ForMember(dest => dest.RecoveryId,
                    opt => opt.ResolveUsing((src, dest, destMember, context) => context.Items["RecoveryId"]));
        }
    }
}