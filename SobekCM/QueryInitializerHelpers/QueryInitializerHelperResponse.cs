using System;

namespace SobekCM.QueryInitializerHelpers
{
    public class QueryInitializerHelperResponse
    {
        public readonly string Message;

        public readonly bool Success;

        public readonly Exception InnerException;

        public string RedirectUrl { get; set; }

        public QueryInitializerHelperResponse(bool success, string message, Exception innerException)
        {
            Message = message;
            Success = success;
            InnerException = innerException;
        }

        public QueryInitializerHelperResponse(bool success, string message)
        {
            Message = message;
            Success = success;
        }

        public QueryInitializerHelperResponse(bool success)
        {
            Success = success;
        }

        public static QueryInitializerHelperResponse Successful
        {
            get { return new QueryInitializerHelperResponse(true);  }
        }
    }
}
