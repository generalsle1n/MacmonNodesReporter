using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MacmonNodesReporter.Model
{
    public class LicenseOption
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("properties")]
        public List<LicenseOptionProperty> Properties { get; set; }
        [JsonPropertyName("expiration")]
        public DateTime? Expiration { get; set; }
        [JsonPropertyName("licenseFile")]
        public LicenseOptionFile LicenseFile { get; set; }

        [JsonPropertyName("disabledReason")]
        public string DisabledReason { get; set; }
        [JsonPropertyName("limits")]
        public List<LicenseOptionLimit> Limits { get; set; }
    }
}
