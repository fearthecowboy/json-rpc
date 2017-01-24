using Microsoft.Perks.JsonRpc.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace JsonRPCServer.Controllers
{
    [Route("http://localhost:1818")]
    public class HomeController : Controller
    {
        [HttpPost("/LanguageServiceAPI/greetMe/")]
        public string Post([FromBody]string name)
        {
            return "Hello, "+name;
        }

    }

}
