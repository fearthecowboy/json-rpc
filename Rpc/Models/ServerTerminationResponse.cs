using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Perks.JsonRpc.Messages
{

    public class ServerTerminationResponse
    {
        [JsonPropertyAttribute(PropertyName = "exitCode")]
        public int ExitCode { get; set; }
    }
}