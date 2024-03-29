using System.Linq;
using System.Text.RegularExpressions;
using Core.Constants;
using Lykke.Common.ApiLibrary.Exceptions;
using LykkeApi2.Models.Operations;

namespace LykkeApi2.Services
{
    public static class SwiftDataValidator
    {
        /// <summary>
        /// Valid SWIFT characters: http://connect-content.us.hsbc.com/hsbc_pcm/onetime/2016/June/16_swift_supported_characters.html
        /// </summary>
        /// <remarks>\p{L} refers to all "letter" characters with or without diacritics</remarks>
        private static readonly Regex SwiftAllowedCharactersOnly = new(@"^[-\p{L}0-9/?:().,'+ ]*$", RegexOptions.Compiled);

        /// <summary>
        /// Despite using regular expression <see cref="SwiftAllowedCharactersOnly"/>,
        /// some problematic characters are from the same unicode groups as allowed characters.
        /// Namely, whenever ligatures (combinations of two letters like ae) are encountered
        /// by bank api (SWIFT), they are rejected.
        /// /// </summary>
        private static readonly char[] SwiftDisallowedCharacters = new[] {'Æ', 'æ', 'œ', 'Œ', 'œ', 'Ĳ', 'ĳ'};

        public static void ValidateSwiftFields(CreateSwiftCashoutRequest request)
        {
            if (!IsValidForSwift(request.Bic))
                ThrowInvalidCharactersForSwift(nameof(request.Bic), request.Bic);
            if (!IsValidForSwift(request.AccNumber))
                ThrowInvalidCharactersForSwift(nameof(request.AccNumber), request.AccNumber);
            if (!IsValidForSwift(request.AccName))
                ThrowInvalidCharactersForSwift(nameof(request.AccName), request.AccName);
            if (!IsValidForSwift(request.BankName))
                ThrowInvalidCharactersForSwift(nameof(request.BankName), request.BankName);
            if (!IsValidForSwift(request.AccHolderAddress))
                ThrowInvalidCharactersForSwift(nameof(request.AccHolderAddress), request.AccHolderAddress);
            if (!IsValidForSwift(request.AccHolderCity))
                ThrowInvalidCharactersForSwift(nameof(request.AccHolderCity), request.AccHolderCity);
            if (!IsValidForSwift(request.AccHolderZipCode))
                ThrowInvalidCharactersForSwift(nameof(request.AccHolderZipCode), request.AccHolderZipCode);
        }
        
        private static bool IsValidForSwift(string input)
        {
            return SwiftAllowedCharactersOnly.IsMatch(input) && !HasExplicitlyDisallowedCharacters(input);
        }

        private static bool HasExplicitlyDisallowedCharacters(string input)
        {
            return !string.IsNullOrEmpty(input) && input.Any(x => SwiftDisallowedCharacters.Contains(x));
        }
        
        private static void ThrowInvalidCharactersForSwift(string fieldName, string fieldValue)
        {
            throw LykkeApiErrorException.BadRequest(LykkeApiErrorCodes.Service.InvalidInput,
                $"{fieldName} cannot be accepted as it contains SWIFT-unsupported character(s). Provided value was: '{fieldValue}'.");
        }
    }
}
