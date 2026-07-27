#region Using directives

using ProtoBuf;
using SobekCM.Core.Configuration.Engine;
using SobekCM.Tools;
using System.Text.Json;
using System.Xml.Serialization;

#endregion

namespace SobekCM.Engine_Library.Endpoints
{
    /// <summary> Base engine endpoint has helper method for serialization of the return object </summary>
    public abstract class EndpointBase
    {
        /// <summary> Serialize the return object, according to the protocol requested </summary>
        /// <param name="ReturnValue"> Return object to serialize </param>
        /// <param name="Response"> HTTP Response to write result to </param>
        /// <param name="Protocol"> Requested protocol type </param>
        /// <param name="CallbackJsonP"> Callback function for JSON-P </param>
        protected void Serialize(object ReturnValue, CompatHttpResponse Response, Microservice_Endpoint_Protocol_Enum Protocol, string CallbackJsonP)
        {
            if (ReturnValue == null)
                return;

            switch (Protocol)
            {
                case Microservice_Endpoint_Protocol_Enum.JSON:
                    Response.Output.Write(JsonSerializer.Serialize(ReturnValue, ReturnValue.GetType(), Json_Options.Default));
                    break;

                case Microservice_Endpoint_Protocol_Enum.PROTOBUF:
                    Serializer.Serialize(Response.OutputStream, ReturnValue);
                    break;

                case Microservice_Endpoint_Protocol_Enum.JSON_P:
                    Response.Output.Write(CallbackJsonP + "(");
                    Response.Output.Write(JsonSerializer.Serialize(ReturnValue, ReturnValue.GetType(), Json_Options.Default));
                    Response.Output.Write(");");
                    break;

                case Microservice_Endpoint_Protocol_Enum.XML:
                    var x = new XmlSerializer(ReturnValue.GetType());
                    x.Serialize(Response.Output, ReturnValue);
                    break;

                case Microservice_Endpoint_Protocol_Enum.TEXT:
                    Response.Output.Write(ReturnValue.ToString());
                    break;

                    // BINARY (BinaryFormatter) was removed in .NET 9 — not supported
            }
        }
    }
}
