using System.Collections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;

namespace Lykke.WalletApiv2.Tests.Recovery.Validation.TestData
{
    internal class SelfieFileStreamIsNullTestData : IEnumerable
    {
        private readonly IFormFile[] _data =
        {
            new FormFile(null, 0, 10, "image.jpg", "image.jpg")
        };

        public IEnumerator GetEnumerator()
        {
            return _data.GetEnumerator();
        }
    }
}