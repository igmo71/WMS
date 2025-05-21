using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace WMS.Backend.Common
{
    public class AppSettings
    {
        // MAX_LENGTH
        public const int NUMBER_MAX_LENGTH = 50;
        public const int NAME_MAX_LENGTH = 150;
        public const int DESCRIPTION_MAX_LENGTH = 400;
        // DEFAULT
        public const int DEFAULT_SKIP = 0;
        public const int DEFAULT_TAKE = 100;

        public const int BACKGROUND_SERVICE_DELAY = 100;

        // TODO: CorrelationId Не используется сейчас
        public const string CORRELATION_HEADER = "X-Correlation-ID";

        public static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            //ReferenceHandler = ReferenceHandler.Preserve,
            //MaxDepth = 3,
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
        };

        public static class Events
        {
            public const string Created = "Created";
            public const string Updated = "Updated";
            public const string Deleted = "Deleted";
        }

        // appsettings.json
        public bool UseArchiving { get; set; }
    }
}
