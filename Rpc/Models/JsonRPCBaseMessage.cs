using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Perks.JsonRpc.Messages
{
    public class JsonRPCBaseMessage
    {
        [JsonProperty(PropertyName = "jsonrpc")]
        public string Version { get; set; }

        [JsonProperty(PropertyName = "id")]
        public int Id;
    }
}
