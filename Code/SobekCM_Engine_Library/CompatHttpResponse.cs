using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SobekCM.Engine_Library
{
    /// <summary> Wraps ASP.NET Core HttpResponse to expose the TextWriter-based Output API
    /// that the engine endpoint classes expect, avoiding mass changes to endpoint bodies </summary>
    /// <remarks> Endpoint methods (e.g. ProtoBuf-net's Serializer.Serialize, StreamWriter.WriteLine)
    /// write synchronously, which Kestrel disallows directly against HttpResponse.Body. Writes here
    /// go to an in-memory buffer instead; call <see cref="FlushToResponseAsync"/> once, after all
    /// synchronous writes are done, to copy that buffer to the real response body asynchronously. </remarks>
    public class CompatHttpResponse
    {
        private readonly HttpResponse _inner;
        private readonly MemoryStream _buffer = new MemoryStream();
        private TextWriter _output;

        /// <summary> Constructor </summary>
        public CompatHttpResponse(HttpResponse Inner) { _inner = Inner; }

        /// <summary> HTTP status code </summary>
        public int StatusCode { get => _inner.StatusCode; set => _inner.StatusCode = value; }

        /// <summary> Content type header </summary>
        public string ContentType { get => _inner.ContentType; set => _inner.ContentType = value; }

        /// <summary> Buffered response body stream — safe to write to synchronously </summary>
        public Stream OutputStream => _buffer;

        /// <summary> TextWriter over the buffered response body (lazy-initialized, auto-flush) </summary>
        public TextWriter Output => _output ??= new StreamWriter(_buffer, Encoding.UTF8, bufferSize: -1, leaveOpen: true) { AutoFlush = true };

        /// <summary> Write a string to the response body </summary>
        public void Write(string text) => Output.Write(text);

        /// <summary> Flush the buffered writer (does not touch the real response) </summary>
        public void Flush() => _output?.Flush();

        /// <summary> Copies the buffered response body to the real response stream. Must be called
        /// exactly once, after all synchronous writes to this instance are complete. </summary>
        public async Task FlushToResponseAsync()
        {
            _output?.Flush();
            _buffer.Position = 0;
            await _buffer.CopyToAsync(_inner.Body);
        }
    }
}
