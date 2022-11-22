using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzDOEmailOnFieldChange.Classes
{
    public class CreateFields
    {
        [JsonProperty("Custom.CSPMResource")]
        public string CustomCSPMResource { get; set; }
        [JsonProperty("Custom.CSLResource")]
        public string CustomCSLResource { get; set; }
        [JsonProperty("Custom.DeveloperResource")]
        public string CustomDeveloperResource { get; set; }
    }
}
