// --------------------------------------------------------------------------------------------------------------------
// <copyright>
//   Copyright (c) Microsoft. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Perks.JsonRpc
{
    using Microsoft.Perks.JsonRpc.Messages;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Features;
    
    
    public class Request : IHttpRequestFeature
    {
        private string _queryString;

        public int? Id { get; set; }

        public string HashKey { get; set; }

        public DateTime ReceivedTime { get; set; }

        public string Protocol { get; set; }

        public string Scheme { get; set; }

        public string Method { get; set; }

        public string PathBase { get; set; }
        public string RawTarget { get; set; }

        public string Path
        {
            get { return AbsolutePath; }
            set { AbsolutePath = value; }
        }

        public string AbsolutePath { get; set; }

        public Dictionary<string,object> queryString { get; set; }

        string IHttpRequestFeature.QueryString
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_queryString) && queryString != null && queryString.Count > 0)
                {
                    _queryString = queryString.Keys.Aggregate((current, each) => $"{current};{queryString[each]}");
                }
                return _queryString;
            }
            set { _queryString = value; }
        }

        public IHeaderDictionary Headers { get; set; }

        Stream IHttpRequestFeature.Body
        {
            get { return new MemoryStream(Encoding.UTF8.GetBytes(Body)); }
            set { }
        }

        public string Body { get; set; } = string.Empty;
        
        public Request()
        {
            Headers = new HeaderDictionary();
            Protocol = string.Empty;
            Scheme = string.Empty;
            Method = string.Empty;
            PathBase = string.Empty;
            Path = string.Empty;
            this._queryString = string.Empty;
        }

        public Request(JObject jsonRpcRequest)
        {
            Headers = new HeaderDictionary();
            Headers["Content-Type"] = "application/json";
            Path = jsonRpcRequest.Property("method").Value.ToString();
            Method = "POST";
            
            var p = jsonRpcRequest.Property("params");
            if( p!=null ) {
                Body = p.Value.ToString();
            } else {
                Body = string.Empty;
            }

            //Body = jsonRpcRequest.Property("params")?.Value.ToString() ?? "";
            
            var it = jsonRpcRequest.Property("id")?.Value;
            if( it != null ) {
                Id = (int)it;
            }
            
        }
    }
}