using System;
using System.ComponentModel.DataAnnotations;

namespace LykkeApi2.Models._2Fa
{
    public class OperationConfirmationModel
    {
        [Required]
        public string Type { set; get; }

        [Required]
        public Guid OperationId { set; get; }

        [Required]
        public OperationConfirmationSignature Signature { set; get; }
    }

    public class OperationConfirmationSignature
    {
        public string Code { set; get; }
    }
}