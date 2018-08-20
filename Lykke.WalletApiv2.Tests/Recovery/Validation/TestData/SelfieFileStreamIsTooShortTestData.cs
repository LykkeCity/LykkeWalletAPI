using System;
using System.Collections;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;

namespace Lykke.WalletApiv2.Tests.Recovery.Validation.TestData
{
    internal class SelfieFileStreamIsTooShortTestData : IEnumerable
    {
        private readonly IFormFile[] _data =
        {
            new FormFile(Stream.Null, 0, 1, "image.jpg", "image.jpg")
        };

        public IEnumerator GetEnumerator()
        {
            return _data.GetEnumerator();
        }
    }
}
