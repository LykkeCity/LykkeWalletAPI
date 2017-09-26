using System.Net;

namespace LykkeApi2.Models.ResponceModels
{
    public class ApiOkResponse : ApiResponse
    {
        public object Result { get; }

        public ApiOkResponse(object result)
            : base(HttpStatusCode.OK)
        {
            Result = result;
        }
    }
}
