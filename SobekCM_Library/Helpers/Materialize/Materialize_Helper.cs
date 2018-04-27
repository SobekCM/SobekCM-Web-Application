
using SobekCM.Library.UI;

namespace SobekCM.Library.Helpers.Materialize
{
    /// <summary>
    /// This is a helper to perform the lookup the requested materialize class
    /// </summary>
    public class Materialize_Helper
    {
        public string Mc(string keyname)
        {
            if (UI_ApplicationCache_Gateway.Settings.Get_Additional_Setting("Use Materialize framework").Equals("true"))
            {
                return " " + UI_ApplicationCache_Gateway.Configuration.MaterializeClasses.Materialize_Classes[keyname].ToString();
            }
            else
            {
                return "";
            }
        }
    }
}
