using Core.Constants;
using Core.Exceptions;

namespace Core.Domain.LykkeApiError
{
    /// <summary>
    ///     Error code for any expected errors.
    ///     Used in <see cref="LykkeApiErrorException" />.
    /// </summary>
    public interface ILykkeApiErrorCode
    {
        /// <summary>
        ///     Error code name that would be returned as contract.
        ///     Always should match field name in <see cref="LykkeApiErrorCodes" />.
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     Default message describing error code reason.
        ///     Could be overriden by more specific message by passing it to <see cref="LykkeApiErrorException" /> constructor.
        /// </summary>
        string DefaultMessage { get; }
    }
}