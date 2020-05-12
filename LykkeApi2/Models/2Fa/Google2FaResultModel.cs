namespace LykkeApi2.Models._2Fa
{
    public class Google2FaResultModel<T>
    {
        public T Payload { get; set; }
        public bool IsCodeValid { get; set; }
        public Googl2FaErrorModel Error { get; set; }

        public static Google2FaResultModel<T> Success(T data)
        {
            return new Google2FaResultModel<T>{Payload = data, IsCodeValid = true};
        }

        public static Google2FaResultModel<T> Fail(string code, string message)
        {
            return new Google2FaResultModel<T>{
                Payload = default(T),
                IsCodeValid = false,
                Error = new Googl2FaErrorModel
            {
                Code = code,
                Message = message
            }};
        }
    }

    public class Googl2FaErrorModel
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }
}
