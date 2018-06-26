using System.Collections;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;

namespace Lykke.WalletApiv2.Tests.Recovery.Validation.TestData
{
    internal class SelfieFileInvalidExtensionTestData : IEnumerable
    {
        private readonly IFormFile[] _data =
        {
            new FormFile(new MemoryStream(new byte[] {0xFF, 0xD8, 0xFF, 0xDB, 0x00, 0x00, 0x00, 0x00}), 0, 8,
                "image.tiff", "image.tiff")
        };

        public IEnumerator GetEnumerator()
        {
            return _data.GetEnumerator();
        }
    }
}