using FluentValidation;
using LykkeApi2.Models.AssetPairsModels;
using LykkeApi2.Strings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LykkeApi2.Models.ValidationModels.AssetPairValidations
{
    public class AssetPairRequestValidationModel : AbstractValidator<AssetPairRequestModel>
    {
        public AssetPairRequestValidationModel()
        {
            RuleFor(r => r.AssetPairId).NotEmpty().WithMessage(Phrases.FieldShouldNotBeEmpty);            
        }
    }
}
