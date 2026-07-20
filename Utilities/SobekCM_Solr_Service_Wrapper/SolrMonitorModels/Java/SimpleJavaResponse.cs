
namespace SolrMonitorModels.Java
{
    /// <summary> Class encapsulates the results of executing Java, including the exit code 
    /// and the response string from the Java process </summary>
    public class SimpleJavaResponse
    {
        /// <summary> Exit code returned from the Java process </summary>
        /// <remarks> Generally, a code of zero is success, and anything else is failure </remarks>
        public int ExitCode { get; private set; }

        /// <summary> Complete response from the Java process, including both the standard response and errors </summary>
        public string Response { get; private set; }

        /// <summary> Constructor for a new instance of the <see cref="SimpleJavaResponse"/> class. </summary>
        /// <param name="ExitCode"> Exit code returned from the Java process </param>
        /// <param name="Response"> Generally, a code of zero is success, and anything else is failure </param>
        public SimpleJavaResponse(int ExitCode, string Response)
        {
            this.ExitCode = ExitCode;
            this.Response = Response;
        }
    }
}
