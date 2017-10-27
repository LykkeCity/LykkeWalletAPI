using Lykke.Service.OperationsRepository.AutorestClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.WalletApiv2.Tests.TransactionsHistory
{
    public static class CreateMockedResponseForOperationsDetailsHistory
    {
        public static Task<IEnumerable<OperationDetailsInformation>> GetOperationsDetails()
        {
            List<OperationDetailsInformation> result = new List<OperationDetailsInformation>();

            result.Add(new OperationDetailsInformation()
            {
                TransactionId = "b64e64bb-eff6-43aa-aee8-d37e8dda7bed",
                ClientId = "4e276be2-5fb8-438d-9d73-15687a84d5e9",
                CreatedAt = DateTime.Now,
                Comment = "Test comment",
            });
            return Task.FromResult(result.AsEnumerable());
        }
    }
}
