using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace WMS.Backend.Common
{
    public static class AppConfig
    {
        // MAX_LENGTH
        public const int NUMBER_MAX_LENGTH = 50;
        public const int NAME_MAX_LENGTH = 150;
        public const int DESCRIPTION_MAX_LENGTH = 400;
        // DEFAULT
        public const int DEFAULT_SKIP = 0;
        public const int DEFAULT_TAKE = 100;

        public const int BACKGROUND_SERVICE_DELAY = 100;
        public const string CORRELATION_ID = "CorrelationId";

        public static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
        };
    }
}
