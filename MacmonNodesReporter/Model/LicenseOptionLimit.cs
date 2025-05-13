using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MacmonNodesReporter.Model
{
    public class LicenseOptionLimit
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("current")]
        public int? Current { get; set; }
        [JsonPropertyName("limit")]
        public int? Limit { get; set; }
    }
}
