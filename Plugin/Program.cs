// --------------------------------------------------------------------------------------------------------------------
// <copyright>
//   Copyright (c) Microsoft. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace JsonRPCServer
{
    using System;
    using System.IO;
    
    using Microsoft.AspNetCore.Hosting;
    using System.Threading.Tasks;
    using Microsoft.Perks.JsonRpc;
    using Microsoft.AspNetCore.Mvc;

    public class Thing{ 
        public string Address{ get;set;}
        public string Name{ get;set;}
    }

    public class HostClient {
        private Connection _connection;
        public HostClient(Connection connection)  { 
            _connection = connection;
        }
        public Task<Thing> Initialize() =>
             _connection.Request<Thing>("/ServerAPI/init/");

        public Task<string> GetString() => 
            _connection.Request<string>("/ServerAPI/GetString/");

        public Task<string> ThrowSomething() =>
            _connection.Request<string>("/ServerAPI/Throw/");
    }

    [Route("http://localhost:8181")]
    public class Plugin : Controller {
        [Route("/plugin/initialize")]
        public void InitializePlugin() {
            Log.WriteLine("[PLUGIN]: Initialize Called");
        }

        [Route("/plugin/GetBool")]
        public bool GetBool() => true;
        
        [Route("/plugin/flipBool")]
        public bool FlipBool([FromBody]bool b) => !b;
            
        [Route("/plugin/getThing")]
        public Thing GetThing() => 
            new Thing { 
                Name = "Tom",
                Address = "Homeless"
            };

        [Route("/plugin/sendThing")]
        public string SendThing([FromBody]Thing something) {
            Log.WriteLine($"SendThing Called - {something.Name} - {something.Address}");
            if( something == null ) {
                Log.WriteLine("Somethign is null");
                return false.ToString();
            }
            return something.Name.Equals("Garrett").ToString();
        }
            
    }
    public static class Program
    {
        public static int Main(string[] args)
        {
            return MainAsync(args).Result;
        }

        public static async Task<int> MainAsync(string[] args) {
            Log.Color = ConsoleColor.DarkYellow;
            Log.Name = "PLUGIN";

            // create the host and server 
            //Hosting.HandleBreak();
            // var myServer = new LanguageService(new StreamReader(Console.OpenStandardInput()), new StreamWriter(Console.OpenStandardOutput()));
            var rpcServer = new Server();

            // start up the hosts.
            var host =
                // listen via our custom servicebus listener
                new WebHostBuilder()
                    .UseServer(rpcServer)
                    .UseStartup<Microsoft.Perks.JsonRpc.Startup>()
                    .Build();

            host.Start();
            rpcServer.Wait();

            // capture stdin/stdout
            var connection = new Connection(Console.Out,Console.In);
            
            // process requests from that connection
            rpcServer.Subscribe(connection);

            // create a client for the remote services            
            var client = new HostClient(connection);
           
            var thing = client.Initialize();
            Log.WriteLine("[PLUGIN]: called initialize...");
            await thing;
            Log.WriteLine($"[PLUGIN]: initialize result {thing.Result.Name} ...");

           /*
            var foo = await client.GetString();
            Log.WriteLine($"[PLUGIN]: Get String: {foo}");
            */
            Log.WriteLine("Sleeping for a long time before close");
            Task.Delay(200000).Wait();
            Log.WriteLine("Closing");
            try {
                var willThrow = await client.ThrowSomething();
            } catch( Exception e ) {
                Log.WriteLine($"EXCEPTION: {e.Message}");
            }

            return 0;
        }
    }
}