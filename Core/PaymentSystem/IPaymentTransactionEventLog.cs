using System;

namespace Core.PaymentSystem
{
    public interface IPaymentTransactionEventLog
    {
        string PaymentTransactionId { get; }
        DateTime DateTime { get; }
        /// <summary>
        /// We have for shit cleaning processes
        /// </summary>
        string TechData { get; }
        /// <summary>
        /// We have for backoffice and other reports
        /// </summary>
        string Message { get; }
        string Who { get; }
    }
}