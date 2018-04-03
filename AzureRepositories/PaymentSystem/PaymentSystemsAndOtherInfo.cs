using System;
using System.Collections.Generic;
using Core.PaymentSystem;

namespace AzureRepositories.PaymentSystem
{
    public static class PaymentSystemsAndOtherInfo
    {
        public static readonly Dictionary<CashInPaymentSystem, Type> PsAndOtherInfoLinks = new Dictionary<CashInPaymentSystem, Type>
        {
            [CashInPaymentSystem.CreditVoucher] = typeof(OtherPaymentInfo),
            [CashInPaymentSystem.Fxpaygate] = typeof(OtherPaymentInfo)
        };
    }
}