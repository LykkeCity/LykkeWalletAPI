using System.Collections;

namespace Lykke.WalletApiv2.Tests.Recovery.Validation.TestData
{
    internal class StateTokenIsInvalidTestData : IEnumerable
    {
        private const string ExpectedMessage = "State Token should be a valid JWE token.";

        private readonly string[][] _data =
        {
            new[] {"abcd", ExpectedMessage},
            new[] {"12345", ExpectedMessage}
        };

        public IEnumerator GetEnumerator()
        {
            return _data.GetEnumerator();
        }
    }
}