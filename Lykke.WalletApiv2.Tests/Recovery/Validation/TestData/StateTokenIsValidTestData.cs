using System.Collections;

namespace Lykke.WalletApiv2.Tests.Recovery.Validation.TestData
{
    internal class StateTokenIsValidTestData : IEnumerable
    {
        private readonly string[] _data =
        {
            "a1.b2.c3.d4.e5",
            "a1..c3.d4.e5"
        };

        public IEnumerator GetEnumerator()
        {
            return _data.GetEnumerator();
        }
    }
}