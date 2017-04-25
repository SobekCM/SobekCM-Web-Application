using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using System.Data;
using System;
using SobekCM.Tools;

using SobekCM.Core.MicroservicesClient;

namespace SobekCM.Engine_Library.Endpoints
{
    public class BuilderLogSummaryServices : BuilderServices
    {
        public void BLSS_Results_XML(HttpResponse Response, List<string> UrlSegments, NameValueCollection QueryString, Microservice_Endpoint_Protocol_Enum Protocol, bool IsDebug)
        {
            Custom_Tracer tracer = new Custom_Tracer();

            tracer.Add_Trace("BuilderLogSummaryServices.BLSS_Results_XML");

            DateTime dt_start;
            DateTime dt_end;
            String bibvidflt=null;
            Boolean includeNoWorkFlag = false;
            DataSet ds;
            dt_end = DateTime.Now;
            dt_start = DateTime.Now.AddHours(-6);
            
            ds=Database.Engine_Database.Builder_Log_Search(dt_start, dt_end, bibvidflt, includeNoWorkFlag, tracer);
            String myresults="<results>";

            tracer.Add_Trace("BuilderLogSummaryServices.BLSS_Results_XML","Looping through dataset.");

            foreach (DataTable table in ds.Tables)
            {
                foreach (DataRow row in table.Rows)
                {
                    foreach (object item in row.ItemArray)
                    {
                        myresults+="<result>" + item.ToString() + "</result>";
                    }
                }
            }

            tracer.Add_Trace("BuilderLogSummaryServices.BLSS_Results_XML", "Done with loop.");

            myresults += "</results>";

            Response.Write(myresults);
        }
    }
}