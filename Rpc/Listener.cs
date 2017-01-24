// --------------------------------------------------------------------------------------------------------------------
// <copyright>
//   Copyright (c) Microsoft. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Collections.Generic;

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace Microsoft.Perks.JsonRpc
{
    public static class Log {
        public static string Name {get;set;}
        public static ConsoleColor Color { get;set;}

        public static void WriteLine(string text) {
            Console.ForegroundColor =  Color ;
            Console.Error.WriteLine($"[{Name}] {text}");
            Console.ForegroundColor =  ConsoleColor.White ;
        }
    }

    public class PeekingTextReader : TextReader {
        private char[] _peekAhead = new char[1]; 

        private bool hasChar = false;
       
        public override string ReadLine() => 
            ReadLineAsync().Result;
        public override int Read(char[] buffer, int start, int count) => 
            ReadAsync( buffer, start, count).Result;
        public override string ReadToEnd() => 
            ReadToEndAsync().Result;

        public override int Read() {
            if( !hasChar ) {
                PeekAsync().Wait();
            }
            if( !hasChar ) {
                throw new EndOfStreamException("Unable to read from stream");
            }
            hasChar = false;
            return _peekAhead[0];
        }
      
        public override async Task<int> ReadAsync(char[] buffer, int index, int count) {
            if( hasChar ) {
                //peeked. frontload the first char
                hasChar = false;
                buffer[index++] = _peekAhead[0];
                return await _reader.ReadAsync(buffer, index, count-1) +1;
            }
            // haven't peeked. 
            return await _reader.ReadAsync(buffer, index, count);
        }

        public override async Task<string> ReadLineAsync() {
            if( hasChar ) {
                //peeked. frontload the first char
                hasChar = false;
                return  _peekAhead[0] + await _reader.ReadLineAsync();
            }
            return await _reader.ReadLineAsync();
        }
        public override async Task<string> ReadToEndAsync() {
            if( hasChar ) {
                //peeked. frontload the first char
                hasChar = false;
                return  _peekAhead[0] + await _reader.ReadToEndAsync();
            }
            return await _reader.ReadToEndAsync();
        }

        public async Task<char?> PeekAsync() {
            if( !hasChar ) {
                hasChar = await _reader.ReadAsync(_peekAhead, 0, 1) > 0;
            }
            return hasChar ? (char?) _peekAhead[0]:null;
        }

        private TextReader _reader;
        public PeekingTextReader(TextReader reader) {
            _reader = reader;
        }
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            if( disposing ) {
                _reader?.Dispose();
                _reader = null;
            }
        }
    }

    public class Connection : IDisposable {
        private TextWriter _writer;
        private PeekingTextReader _reader; 
        private bool _isDisposed = false; 
        private int _requestId;
        private Dictionary<int, IClientResponse> _tasks = new Dictionary<int, IClientResponse>(); 
        
        private Task _loop;
        public event EventHandler OnDispose;
        public event Func<Connection, JObject,Task> OnMessage;
        public Connection(TextWriter writer, TextReader reader ) {
            _writer = writer;
            _reader = new PeekingTextReader( reader);
            _loop = Task.Factory.StartNew(Listen);
        }
        public Connection(TextWriter writer, TextReader reader, Action onDispose ): this(writer,reader) {
            // take this action if the connection is disposed.
            // (don't worry about failures)
            OnDispose += (s,a) => { try { onDispose(); } catch { }; };
        }

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private CancellationToken  _cancellationToken=> _cancellationTokenSource.Token;
        public bool IsAlive => !_cancellationToken.IsCancellationRequested && _writer != null && _reader != null;

/*
        private string _content = string.Empty;
        private char[] _buffer = new char[32768];
        private void ReallocBuffer() {
            var oldBuffer = _buffer;

            // replace the existing buffer
            _buffer = new char[_buffer.Length * 2];
            
            // number of chars used in the current buffer
            _tail = _tail-_head;

            // copy the chars to the top of the new buffer
            Array.Copy(oldBuffer, _head, _buffer, 0, _tail );
            
            // set the head to the front.
            _head = 0;
        }
        private int _head = 0;
        private int _tail = 0;


                // move back to the front of the buffer if we have nothing.
                if( _head == _tail ) {
                    _head = _tail = 0;
                }
                int chars = await _reader?.ReadAsync(_buffer, _tail, _buffer.Length - _tail);
                _tail += chars; 
                
                // recieved characters?
                if( chars < 1 ) {
                    return await Listen();
                }

                // is this a header, or a request?
                if(_buffer[_head] == '{') {
                    // request 
                    
                }        
*/        

        private async Task<JToken> ReadJson() {
            var jsonText = string.Empty;
            JToken json = null;
            while( json == null ) {
                Log.WriteLine("In Loop");
                jsonText += await _reader.ReadLineAsync(); // + "\n";
                Log.WriteLine($"JSON: {jsonText}");
                if( jsonText.StartsWith("{") && jsonText.EndsWith("}") ) {
                    Log.WriteLine($"It's an object");
                    // try to parse it.
                    try {
                        json = JObject.Parse(jsonText);
                        if( json != null) {
                            Log.WriteLine($"{json.ToString()}");
                            return json;
                        }
                    } catch {
                        // not enough text?
                    }
                } else if( jsonText.StartsWith("[") && jsonText.EndsWith("]") ) {
                    // try to parse it.
                    Log.WriteLine($"It's an array (batch!)");
                    try {
                        json = JArray.Parse(jsonText);
                        if( json != null) {
                            return json;
                        }
                    } catch {
                        // not enough text?
                    }
                }
            }
            return json;
        }

        private async Task<JToken> ReadJson(int contentLength) {
            var buffer = new char[contentLength+1];
            var chars = await _reader.ReadAsync(buffer, 0, contentLength);
            while( chars < contentLength) {
                chars += await _reader.ReadAsync( buffer, chars, contentLength - chars );
            }
            var jsonText = new String(buffer);
            if( jsonText.StartsWith("{")) {
                return JObject.Parse(jsonText);
            }
            return JArray.Parse(jsonText);
        }

        private async Task<bool> Listen() {
            if(!IsAlive) {
                Log.WriteLine("NOT ALIVE!");
                return false;
            }

            try {
                Log.WriteLine("Listen");
                var ch = await _reader?.PeekAsync();
                Log.WriteLine("Peeked.");
                if( null == ch) {
                    // didn't get anything. start again, it'll know if we're shutting down
                    return await Listen();
                }
                Log.WriteLine($"Checking Char {(char)ch}");
                if( '{' == ch || '[' == ch ) {
                    Log.WriteLine("Reading JSON");
                    // looks like a json block or array. let's do this.
                    Process(await ReadJson());
                    Log.WriteLine("Processed... JSON");
                    // we're done here, start again.
                    return await Listen();
                }
                
                // We're looking at headers
                var headers = new Dictionary<string,string>();
                var line = await _reader.ReadLineAsync();
                while( !string.IsNullOrWhiteSpace(line)) { 
                     var bits = line.Split(new[]{':'},1);
                     if( bits.Length != 2 ) {
                         Log.WriteLine("header not right!");
                     }
                     headers.Add( bits[0].Trim(), bits[1].Trim());
                }
                // looks like the spacer between header and body
                // the next character had better be a { or [
                if( '{' == ch || '[' == ch ) {
                    string value;
                    int contentLength = 0;
                    if( headers.TryGetValue("Content-Length", out value) && Int32.TryParse( value, out contentLength) ) {
                        Process(await ReadJson(contentLength));
                    }
                    // looks like a json block or array. let's do this.
                    Process(await ReadJson());
                    // we're done here, start again.
                    return await Listen();
                }    
                Log.WriteLine("Uh-oh?");
                return await Listen();
            } catch(Exception e) {
                if( IsAlive ) {
                    Log.WriteLine($"Error during Listen {e.GetType().Name}/{e.Message}/{e.StackTrace}");
                    return await Listen();
                }
            }

            return false;
        }

        public void Process( JToken content ) {
            if( content == null ) {
                Log.WriteLine("BAD CONTENT");
                return;
            }
            if( content is JObject ) {
                var jobject = content as JObject;
                try {
                    if( jobject.Properties().Any(each => each.Name == "method")) {
                        // this is a method call.
                        // pass it to the service that is listening...
                        Log.WriteLine($"Dispatching: {jobject.Property("method")}");
                        OnMessage( this, jobject);
                        return;
                    }

                    // this is a result from a previous call.
                    if( jobject.Properties().Any(each => each.Name == "result")) {
                        var id = (int)jobject.Property("id");
                        var f = _tasks[id];
                        _tasks.Remove(id);
                        Log.WriteLine($"result data: {jobject.Property("result").Value.ToString()}");
                        f.SetCompleted(jobject.Property("result").Value );
                        Log.WriteLine("Should have unblocked?");
                    }

                } catch( Exception e ) {
                    Log.WriteLine($"[LISTEN ERROR]: {e.GetType().FullName}/{e.Message}/{e.StackTrace}");
                    Log.WriteLine($"[LISTEN CONTENT]: {jobject.ToString()}");
                }
            }
            if( content is JArray) {
                Log.WriteLine("TODO: Batch");
                return;
            }
        }
        public void DisposeOn(object source, EventArgs args) {
            this.Dispose(true);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            // ensure that we are in a cancelled state.
            _cancellationTokenSource?.Cancel();
            if (!_isDisposed)
            {
                // make sure we can't dispose twice
                _isDisposed = true;
                if (disposing)
                {
                    foreach( var t in _tasks.Values ) {
                        t.SetCancelled();
                    }

                    // do any disposal work before we shut anything down.
                    try {
                        OnDispose?.Invoke(this,new EventArgs());
                    } catch {
                        // if this fails, we don't care.
                    }
                    OnDispose = null;
                    
                    _writer?.Dispose();
                    _writer = null;
                    _reader?.Dispose();
                    _reader = null;

                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                }
            }
        }

        private Semaphore _streamReady = new Semaphore(1, 1);
        private async Task Send( string text ) { 
            _streamReady.WaitOne();
            Log.WriteLine($"SEND {text}");
            await  _writer.WriteLineAsync( text );
            _streamReady.Release();
        }
        public async Task SendError(int id, int code, string message) {
            await Send( ProtocolExtensions.Error(id, code, message)).ConfigureAwait(false);
        }
        public async Task Respond(int request, string value ){
            await Send( ProtocolExtensions.Response(request, value ) ).ConfigureAwait(false);
        }
        public async Task Notify(string methodName ) =>
            await Send( ProtocolExtensions.Call(methodName) ).ConfigureAwait(false);
        public async Task Notify(string methodName, object[] values ) => 
            await Send( ProtocolExtensions.Call(methodName, values) ).ConfigureAwait(false);
        public async Task Notify(string methodName, string jsonObject ) =>
            await Send( ProtocolExtensions.Call( methodName, jsonObject)).ConfigureAwait(false);

        public async Task<T> Request<T>(string methodName ) {
            var id = Interlocked.Increment(ref _requestId);
            var response =  new ClientResponse<T>(id);
            _tasks.Add( id,response);
            await Send( ProtocolExtensions.Call(id, methodName) ).ConfigureAwait(false);
            return await response.Task.ConfigureAwait(false);
        }
        /*
        public async Task<T> RequestWithValues<T>(string methodName, params object[] values ) {
            var id = Interlocked.Increment(ref _requestId);
            var response =  new ClientResponse<T>(id);
            _tasks.Add( id,response);
            await Send( ProtocolExtensions.Call(id, methodName, values) ).ConfigureAwait(false);
            return await response.Task.ConfigureAwait(false);
        }
         */
        public async Task<T> Request<T>(string methodName, object jsonObject ) {
            var id = Interlocked.Increment(ref _requestId);
            var response =  new ClientResponse<T>(id);
            _tasks.Add( id,response);
            var payload = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObject);
            await Send( ProtocolExtensions.Call(id, methodName, payload)).ConfigureAwait(false);
            return await response.Task.ConfigureAwait(false);
        }
            
        
        public async Task Batch(IEnumerable<string> calls ) =>
            await Send( calls.Array() );
    }

    public static class ProtocolExtensions {
        private static Type[] primitives = new Type[] { typeof(bool), typeof(int),typeof(float), typeof(double), typeof(short), typeof(long), typeof(ushort), typeof(uint), typeof(ulong), typeof(byte),typeof(sbyte)};
        internal static string Quote(this object value) => 
            value == null ? "null":                                              // null values
            primitives.Contains(value.GetType()) ? value.ToString().ToLower() :  // primitive values (number,boolean)
            $"\"{value.ToString()}\"";                                           // everything else.
        
        internal static string Brace(this string text) => 
            $"{{{text}}}";
        internal static string Pair(this string key, string value) => 
            $"{Quote(key)}:{Quote(value)}";
        internal static string Method(this string value) =>
            Pair("method",value);
        internal static string ArrayValues( this IEnumerable<object> values ) => 
            $"[{values.Aggregate((c,e) => $"{c.ToString()},{Quote(e)}")}]";
        internal static string Array(this IEnumerable<string> jsonObjects ) => 
            $"[\n  {jsonObjects.Aggregate((c,e) => $"{c.ToString()},\n  {e}")}\n]";
        internal static string Params(this IEnumerable<object> values) => 
            $"{Quote("params")}:{values.ArrayValues()}";
        internal static string Params(this string jsonObject) => 
            $"{Quote("params")}:{jsonObject}";
        internal static string protocol = 
            Pair("jsonRpc","2.0");

        internal static string Response (int id, string result) => 
            ProtocolExtensions.Brace($"{protocol},{Id(id)},{Result(result)}");
            
        internal static string Error( int id,  int code, string message ) =>
            ProtocolExtensions.Brace($"{protocol},{Id(id)},{Quote("error")}:{Brace($"{"code"}:{code},{"message"}:{Quote(message)}")}");

        internal static string Id(this int id) => 
            $"{Quote("id")}:{id}";
        internal static string Result(this string value) => 
            $"{Quote("result")}:{value}";

        // no parameters
        public static string Call(string methodName )=>
            ProtocolExtensions.Brace($"{protocol},{methodName.Method()}");
        
        public static string Call(int id ,string methodName) => 
            ProtocolExtensions.Brace($"{protocol},{methodName.Method()},{Id(id)}");
          
        // --> {"jsonrpc": "2.0", "method": "update", "params": [1,2,3,4,5]}
        public static string Call(string methodName, object[] values ) => 
            ProtocolExtensions.Brace($"{protocol},{methodName.Method()},{values.Params()}");
          
        public static string Call(int id, string methodName, object[] values ) => 
            ProtocolExtensions.Brace($"{protocol},{methodName.Method()},{values.Params()},{Id(id)}");
        // --> {"jsonrpc": "2.0", "method": "update", "params": {"someArray":[1,2,3,4,5]}}
        public static string Call(string methodName, string jsonObject ) =>
            ProtocolExtensions.Brace($"{protocol},{methodName.Method()},{jsonObject.Params()}");
         
        public static string Call(int id, string methodName, string jsonObject ) =>
            ProtocolExtensions.Brace($"{protocol},{methodName.Method()},{jsonObject.Params()},{Id(id)}");
    }

    public interface IClientResponse {
        bool SetCompleted(JToken result);
        bool SetException(JToken error);
        bool SetCancelled();
    }
    public class ClientResponse<T> : TaskCompletionSource<T>, IClientResponse {
        public int Id { get; private set; }
        private Action<JObject> _setResult;

        public ClientResponse(int id, Action<JObject> setResult ) {
            Id = id;
            _setResult = setResult;
        }
        public ClientResponse(int id) {
            Id = id;
        }

        public bool SetCompleted(JToken result)
        {
            Log.WriteLine($"The jtoken for the result is {result}");
            var value = result.ToObject<T>();
            Log.WriteLine($"Deserialized {value}");
            Log.WriteLine($" try setting response {TrySetResult(value)}");
            return true;
            // return TrySetResult(result.ToObject<T>());
        }

        public bool SetException(JToken error)
        {
            return TrySetException(error.ToObject<Exception>());
        }

        public bool SetCancelled()
        {
            return TrySetCanceled();
        }
    }
}
