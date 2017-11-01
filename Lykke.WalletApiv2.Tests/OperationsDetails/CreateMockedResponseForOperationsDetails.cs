using Lykke.Service.Registration.Models;
using System;
using System.Threading.Tasks;

namespace Lykke.WalletApiv2.Tests.OperationsDetails
{
    public class CreateMockedResponseForOperationsDetails
    {
        public static Task<string> RegisterOerationDetail()
        {
            return Task.FromResult(Guid.NewGuid().ToString("N"));
        }

        public static async Task CreateOperation()
        {
            await Task.FromResult(Guid.NewGuid().ToString("N"));
        }

        public static Task<AuthResponse> Auth()
        {
            return Task.FromResult(new AuthResponse()
            {
                Token = "dd5bc654826b4e11ab3607592bf80587d302f64f160642f588f687f01db09e65"
            });
        }
    }
}
