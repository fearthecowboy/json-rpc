// --------------------------------------------------------------------------------------------------------------------
// <copyright>
//   Copyright (c) Microsoft. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Perks.JsonRpc
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting.Server;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Features;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class Server : IServer
    {
        private ManualResetEvent _ready = new ManualResetEvent(false);
        private object requestLock = new object();
        public IServer CreateServer(IConfiguration configuration)
        {
            return this;
        }

        public void Subscribe(Connection connection)  {
            connection.OnMessage += ProcessRequestMessage;
        }

        public async Task ProcessResponse(Response response,Connection connection)  {
              Log.WriteLine("Starting Response.");
                if( response.Id == null ) { 
                    Log.WriteLine("This is a notification");
                    return;
                }
                int id = (int)response.Id;

                var value =response.ToString();

                Log.WriteLine($"VALUE: {value}.");

                if( value.StartsWith("{") || value.StartsWith("[") ) {
                    await connection.Respond( id, value );
                } else {
                    await connection.Respond(id, ProtocolExtensions.Quote(value));
                }
                Log.WriteLine("[----]: sent result.");
                
        }
        public async Task ProcessRequestMessage(Connection connection, JObject jsonObject)
        {
            var request =  new Request(jsonObject) {
                PathBase =  "http://localhost:8181/"
            };
            try {
                var x = await ProcessRequest(request);
                await ProcessResponse(x,connection );
            } catch (Exception e ) {
                Log.WriteLine($"ERROR. {e.GetType().Name}/{e.Message}");
                if( request.Id != null) {
                 await connection.SendError((int)request.Id, -1,e.Message);
                }
            }
        }

        public void Wait()
        {
            _ready.WaitOne();
        }

        private static Task<Response> NoResponse
        {
            get
            {
                var result = new TaskCompletionSource<Response>(TaskCreationOptions.AttachedToParent);
                result.SetResult(new Response { StatusCode = (int)HttpStatusCode.ServiceUnavailable, Body = Stream.Null });
                return result.Task;
            }
        }

        public Func<IHttpRequestFeature, Task<Response>> ProcessRequest { get; private set; }

        public Server()
        {
            // can not process requests until the Start Method is called.
            ProcessRequest = (r) => NoResponse;
        }

        public void Dispose()
        {
            // turn off the ability to process messages.
            ProcessRequest = (r) => NoResponse;
        }

        public void Start<TContext>(IHttpApplication<TContext> application)
        {
            ProcessRequest = (request) =>
            {
                var response = new Response {
                    Id = ((Request)request).Id
                };

                var context = application.CreateContext(new FeatureCollection()
                {
                    [typeof(IHttpRequestFeature)] = request,
                    [typeof(IHeaderDictionary)] = request.Headers,
                    [typeof(IHttpResponseFeature)] = response,
                });

                return application.ProcessRequestAsync(context).ContinueWith(antecedent =>
                {
                    var requestException = antecedent.IsFaulted ? antecedent.Exception?.InnerException : antecedent.IsCanceled ? new OperationCanceledException() : null;
                    var tosend = response.ToString();
                    // application.DisposeContext(context, requestException);
                    return response;
                });
            };

            // we're good to go!
            _ready.Set();
        }

        public IFeatureCollection Features => new FeatureCollection();
    }
}