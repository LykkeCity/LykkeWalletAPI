using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LykkeApi2.Models.TransactionHistoryModels
{
    public class OperationsDetailHistoryRequestModel
    {
        public string TransactionId { get; set; }
        public string ClientId { get; set; }
    }
}
