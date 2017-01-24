using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Perks.JsonRpc.Messages
{
    public class JsonRPCResponseMessage : JsonRPCBaseMessage
    {
        [JsonProperty(PropertyName = "result")]
        public object Result { get; set; }
        [JsonProperty(PropertyName = "error")]
        public JsonRPCResponseError Error { get; set; }
    }
}