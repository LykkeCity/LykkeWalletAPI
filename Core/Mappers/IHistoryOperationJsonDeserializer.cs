using System.Collections.Generic;

namespace Core.Mappers
{
    public interface IHistoryOperationJsonDeserializer<out T>
    {
        T Deserialize(string json, IDictionary<string, string> map);
    }
}
