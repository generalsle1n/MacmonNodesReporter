using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MacmonNodesReporter.Model
{
    public class LicenseOptionFile
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("creationTime")]
        public DateTime CreationTime { get; set; }
        [JsonPropertyName("serialNumber")]
        public string SerialNumber { get; set; }

    }
}
