using Microsoft.Perks.JsonRpc.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace JsonRPCServer.Controllers
{
    [Route("http://localhost:1818")]
    public class ShutDownController : Controller
    {
        [HttpPost("/LanguageServiceAPI/shutDown/")]
        public ServerTerminationResponse Post()
        {
            // do some checks and prepare to shutdown
            return new ServerTerminationResponse() { ExitCode = 0 };
        }

    }

}
