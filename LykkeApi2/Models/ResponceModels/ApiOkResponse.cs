namespace LykkeApi2.Models.ResponceModels
{
    public class ApiOkResponse : ApiResponse
    {
        public object Result { get; }

        public ApiOkResponse(object result)
            : base(ResponseStatusCode.OK)
        {
            Result = result;
        }
    }
}
