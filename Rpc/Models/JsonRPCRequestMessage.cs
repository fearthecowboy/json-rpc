using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Perks.JsonRpc.Messages
{
    public class JsonRPCRequestMessage : JsonRPCBaseMessage
    {
        [JsonProperty(PropertyName = "method")]
        public string Method { get; set; }

        [JsonProperty(PropertyName = "params")]
        public object[] Params{get; set; }
    }
}

