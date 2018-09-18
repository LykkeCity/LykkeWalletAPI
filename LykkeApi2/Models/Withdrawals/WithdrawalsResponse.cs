using System.Collections.Generic;

namespace LykkeApi2.Models.Withdrawals
{
    public class WithdrawalMethodsResponse
    {
        public IEnumerable<WithdrawalMethod> WithdrawalMethods { get; set; }
    }

    public class WithdrawalMethod
    {
        public string Name { get; set; }

        public IEnumerable<string> Assets { get; set; }
    }
}
