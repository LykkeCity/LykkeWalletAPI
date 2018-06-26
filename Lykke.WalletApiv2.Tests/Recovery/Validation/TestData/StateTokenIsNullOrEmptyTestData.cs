using System.Collections;

namespace Lykke.WalletApiv2.Tests.Recovery.Validation.TestData
{
    internal class StateTokenIsNullOrEmptyTestData : IEnumerable
    {
        private const string ExpectedMessage = "State Token should not be empty";

        private readonly string[][] _data =
        {
            new[] {null, ExpectedMessage},
            new[] {"", ExpectedMessage}
        };

        public IEnumerator GetEnumerator()
        {
            return _data.GetEnumerator();
        }
    }
}