using System.Text.Json.Serialization;
using System.Text.Json;

namespace Slck.Envelope.Utils
{
    public static class JsonSerializerOptionsProvider
    {
        private static readonly JsonSerializerOptions _default = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static JsonSerializerOptions Default => _default;
    }
}
