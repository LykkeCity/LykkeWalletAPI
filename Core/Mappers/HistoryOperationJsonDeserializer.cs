using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Core.Mappers
{
    public class HistoryOperationJsonDeserializer<T> : IHistoryOperationJsonDeserializer<T>
    {
        #region CustomMappingResolver
        private class CustomMappingResolver : DefaultContractResolver
        {
            private readonly IDictionary<Type, IDictionary<string, string>> _jsonMap;

            public CustomMappingResolver(IDictionary<Type, IDictionary<string, string>> jsonMap)
            {
                _jsonMap = jsonMap;
            }

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var prop = base.CreateProperty(member, memberSerialization);
                if (_jsonMap.TryGetValue(
                        member.DeclaringType,
                        out IDictionary<string, string> dict) &&
                    dict.TryGetValue(member.Name, out string jsonName))
                {
                    prop.PropertyName = jsonName;
                }

                return prop;
            }
        }
        #endregion

        public T Deserialize(string json, IDictionary<string, string> map = null)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentNullException(nameof(json));
            }

            var settings = CreateSerializerSettings(map);

            return JsonConvert.DeserializeObject<T>(json, settings);
        }

        public static JsonSerializerSettings CreateSerializerSettings(IDictionary<string, string> map)
        {
            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            if (map != null)
            {
                var resolverMap = new Dictionary<Type, IDictionary<string, string>>
                {
                    {typeof(T), map}
                };
                settings.ContractResolver = new CustomMappingResolver(resolverMap);
            }

            return settings;
        }
    }
}
