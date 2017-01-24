using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace JsonRPCServer.Controllers
{
    public class Thing{ 
        public string Name{ get;set;}
        public string Address{ get;set;}

    }

    [Route("http://localhost:8181")]
    public class HomeController : Controller
    {
        [HttpPost("/ServerAPI/init/")]
        public Thing Post([FromBody]string[] msgs)
        {
            return new Thing{ Name ="Garrett", Address = "123 Anyhwre"};
        }

        [HttpPost("/ServerAPI/GetString/")]
        public string GetString([FromBody]string[] msgs)
        {
            return "Acknowledging handshake protocol message: "+ msgs[0] + " This is Houston, over!";
        }

        [HttpPost("/ServerAPI/Throw/")]
        public string ThrowSomething()
        {
            throw new Exception("Rats. Threw");
        }

    }

}
