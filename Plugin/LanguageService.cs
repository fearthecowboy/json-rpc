#if false
using Microsoft.Perks.JsonRpc.Messages;
using Microsoft.Perks.JsonRpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace JsonRPCServer 
{
    public class LanguageService : Server
    {
        private int Id;
        private StreamReader reader;
        private StreamWriter writer;
        public bool IsRunning { get; set;  }
        Dictionary<int, Action<string>> tasks = new Dictionary<int, Action<string>>();

        public LanguageService(StreamReader instream, StreamWriter outstream)
        {
            this.reader = instream;
            this.writer = outstream;
            this.writer.AutoFlush = true;
            this.IsRunning = true;
        }

        public Task<string> Initialize()
        {
            var id = Interlocked.Increment(ref Id);

            var result = new TaskCompletionSource<string>();

            tasks.Add(id, (text) =>
            {
                // deserialize output
                // but for now do nothing
                var resp = new JsonRPCResponseMessage();
                resp.Id = JsonConvert.DeserializeObject<JsonRPCResponseMessage>(text).Id;
                var msg = "Initialization successful!";
                resp.Result = msg;
                writer.WriteLine(JsonConvert.SerializeObject(resp));
                
                result.TrySetResult(msg);
            });
            SendRequest(id, "/ServerAPI/init/", new object[] { "Commencing Handshake protocol, this is base station, over!" }, writer);
            return result.Task;
        }

        public Task DoNothingRequest()
        {
            var id = Interlocked.Increment(ref Id);

            var result = new TaskCompletionSource<string>();
            tasks.Add(id, (text) =>
            {
                // parse the object
                var token = JObject.Parse(text);
                var resultObj = token.GetValue("Result");
                // do something with this obj now......
                result.TrySetResult(resultObj.ToString());
            });
            SendRequest(id, "/ServerAPI/init/", new object[] { "1" }, writer);
            return result.Task;
        }

        public async void Listen()
        {
            Console.Error.WriteLine($"[CLIENT]: Block on read?");
            var inputStr = reader.ReadLine();
            Console.Error.WriteLine($"[CLIENT]: {inputStr}");
            var dict = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(inputStr);
            Console.Error.WriteLine($"Back with {dict}");
            // Is this a request or a response?
            if (!dict.ContainsKey("result"))
            {
                await ProcessRequestMessage(inputStr);
            }
            else
            {
                await Task.Factory.StartNew(() =>tasks[JsonConvert.DeserializeObject<JsonRPCResponseMessage>(inputStr).Id](inputStr));
                //ProcessResponseMessage(inputStr);
            }
        }

        private Task ProcessRequestMessage(string reqStr)
        {
            var jReq = JsonConvert.DeserializeObject<JsonRPCRequestMessage>(reqStr);
            Request hReq = new Request(jReq);
            hReq.PathBase = "http://localhost:1818/";
            return ProcessRequest(hReq).ContinueWith((antecedent) =>
            {
                var resp = new JsonRPCResponseMessage();
                resp.Id = hReq.Id;
                if (antecedent.Exception != null)
                {
                    var errorResp = new JsonRPCResponseError();
                    errorResp.Code = antecedent.Exception.HResult;
                    errorResp.Message = antecedent.Exception.Message;
                    resp.Error = errorResp;
                }
                else
                {
                    if (antecedent.Result.GetType() == typeof(Microsoft.Perks.JsonRpc.Messages.ServerTerminationResponse))
                    {
                        // begin termination
                       // IsRunning = false;
                    }
                    var value =  antecedent.Result.ToString();
                    if( value.StartsWith("{") || value.StartsWith("[") ) {
                        resp.Result = value;
                    } else {
                        resp.Result = JsonConvert.SerializeObject(value);
                    }
                }
                Console.Error.WriteLine($"[Client] writing {resp.Result}");
                    writer.WriteLine(JsonConvert.SerializeObject(resp));
                Console.Error.WriteLine($"[Client] wrote {resp.Result}");
            });

        }
        // Simply flush the request to given stream, let's not await this for now
        public async void SendRequest(int id, string reqMethod, object[] reqParams, StreamWriter writer)
        {
           await Task.Factory.StartNew(() =>
            {
                var jReq = new JsonRPCRequestMessage();
                jReq.Id = id;
                jReq.Method = reqMethod;
                jReq.Params = reqParams;
                writer.AutoFlush = true;
                    writer.WriteLine(JsonConvert.SerializeObject(jReq));
            });
        }
    }
}
#endif