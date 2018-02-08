using System;

namespace LykkeApi2.Domain.Commands
{
    public class SignCommand
    {
        public Guid RequestId { get; set; }        
        public string RequestType { get; set; }
        public string ClientId { get; set; }
        public string Context { get; set; }        
    }
}