#region Using directives

using SobekCM.Core.Users;
using SobekCM.Engine_Library.Database;
using SobekCM.Tools;

#endregion

namespace SobekCM.Library.Authentication
{
    /// <summary> Validates a username/password directly against the SobekCM database
    /// (the "integrated" authentication that has always existed) </summary>
    /// <remarks> Pure extraction of the call <c>Logon_MySobekViewer</c> used to make directly to
    /// <see cref="Engine_Database.Get_User(string, string, Custom_Tracer)"/> — no behavior change </remarks>
    public class Sobek_Authentication_Provider : ICredential_Authentication_Provider
    {
        /// <summary> Unique code for this provider </summary>
        public string Provider_Code => "sobek";

        /// <summary> Label displayed to the user on the logon page </summary>
        public string Display_Label => "SobekCM";

        /// <summary> This provider is always enabled — whether the standard logon form itself is
        /// shown/usable is a separate, pre-existing concern (System.Disable_Standard_User_Logon_Flag),
        /// unrelated to provider selection </summary>
        public bool Enabled => true;

        /// <summary> Users validated by this provider are tagged as Sobek-authenticated </summary>
        public User_Authentication_Type_Enum Authentication_Type => User_Authentication_Type_Enum.Sobek;

        /// <summary> Validate a username/password against the SobekCM database </summary>
        /// <param name="Username"> Username (or email) as submitted on the logon form </param>
        /// <param name="Password"> Plain-text password as submitted on the logon form </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones </param>
        /// <returns> Fully built <see cref="User_Object"/> if valid, otherwise NULL </returns>
        public User_Object Authenticate(string Username, string Password, Custom_Tracer Tracer)
        {
            return Engine_Database.Get_User(Username, Password, Tracer);
        }
    }
}
