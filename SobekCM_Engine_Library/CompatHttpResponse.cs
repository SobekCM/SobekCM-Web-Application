using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace SobekCM.Engine_Library
{
    /// <summary> Wraps ASP.NET Core HttpResponse to expose the TextWriter-based Output API
    /// that the engine endpoint classes expect, avoiding mass changes to endpoint bodies </summary>
    public class CompatHttpResponse
    {
        private readonly HttpResponse _inner;
        private TextWriter _output;

        /// <summary> Constructor </summary>
        public CompatHttpResponse(HttpResponse Inner) { _inner = Inner; }

        /// <summary> HTTP status code </summary>
        public int StatusCode { get => _inner.StatusCode; set => _inner.StatusCode = value; }

        /// <summary> Content type header </summary>
        public string ContentType { get => _inner.ContentType; set => _inner.ContentType = value; }

        /// <summary> Raw response body stream </summary>
        public Stream OutputStream => _inner.Body;

        /// <summary> TextWriter over the response body (lazy-initialized, auto-flush) </summary>
        public TextWriter Output => _output ??= new StreamWriter(_inner.Body, Encoding.UTF8, bufferSize: -1, leaveOpen: true) { AutoFlush = true };

        /// <summary> Write a string to the response body </summary>
        public void Write(string text) => Output.Write(text);

        /// <summary> Flush the response body </summary>
        public void Flush() => _inner.Body.Flush();
    }
}
