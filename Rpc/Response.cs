// --------------------------------------------------------------------------------------------------------------------
// <copyright>
//   Copyright (c) Microsoft. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Perks.JsonRpc
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Features;
    using Microsoft.AspNetCore.Http.Internal;

    public class Response : IHttpResponseFeature
    {
        public int? Id;
        public int StatusCode { get; set; }

        public string ReasonPhrase { get; set; }

        public IHeaderDictionary Headers { get; set; }

        public Stream Body { get; set; }

        public string Content{get;set;}

        public override string ToString()
        {
            if( Content != null ) {
                return Content;
            }

            if (Body.CanSeek)
            {
                Body.Seek(0, SeekOrigin.Begin);
            }

            try
            {
                using (var reader = new StreamReader(Body, Encoding.UTF8))
                {
                    return (Content = reader.ReadToEndAsync().Result);
                }
            }
            finally
            {
                if (Body.CanSeek)
                {
                    Body.Seek(0, SeekOrigin.Begin);
                }
            }
        }

        public bool HasStarted
        {
            get { return false; }
        }

        public Response()
        {
            StatusCode = 500;
            Headers = new HeaderDictionary();
            Body = new MemoryStream();
        }

        public void OnStarting(Func<object, Task> callback, object state)
        {
            throw new NotImplementedException();
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
            throw new NotImplementedException();
        }
    }
}