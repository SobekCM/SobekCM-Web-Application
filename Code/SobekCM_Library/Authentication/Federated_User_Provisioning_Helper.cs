#region Using directives

using SobekCM.Core.Users;
using SobekCM.Engine_Library.Database;
using SobekCM.Library.Database;
using SobekCM.Tools;
using System;
using System.Collections.Generic;

#endregion

namespace SobekCM.Library.Authentication
{
    /// <summary> Shared "look up an existing linked account, or provision a new one" logic used by every
    /// federated (OIDC/SAML) provider's Complete_SignIn — the live equivalent of what the dormant
    /// Shibboleth code in UserObjectInitializer.check_shibboleth() used to do inline </summary>
    public static class Federated_User_Provisioning_Helper
    {
        /// <summary> Look up the user previously linked to this provider/subject, or provision a new one
        /// from the supplied claims if this is their first sign-in through this provider </summary>
        /// <param name="ProviderCode"> Provider code (matches Oidc_Configuration/Saml_Configuration Provider_Code) </param>
        /// <param name="SubjectId"> Subject identifier issued by the identity provider (OIDC 'sub', SAML NameID) </param>
        /// <param name="AuthType"> Authentication type to tag the user with </param>
        /// <param name="Claims"> Flattened claim name -> value pairs from the validated principal </param>
        /// <param name="GetMapping"> Provider config's Get_User_Object_Mapping method </param>
        /// <param name="Constants"> Provider config's Constants list, applied to newly-provisioned users </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones </param>
        /// <returns> Fully built <see cref="User_Object"/>, or NULL if the subject id was missing or provisioning failed </returns>
        public static User_Object Get_Or_Provision_User(string ProviderCode, string SubjectId, User_Authentication_Type_Enum AuthType,
            Dictionary<string, string> Claims, Func<string, User_Object_Attribute_Mapping_Enum> GetMapping,
            List<SobekCM.Core.Configuration.Authentication.Attribute_Mapping_Entry> Constants, Custom_Tracer Tracer)
        {
            if (String.IsNullOrEmpty(SubjectId))
                return null;

            // Returning user - already linked to this provider from a previous sign-in
            User_Object existingUser = Engine_Database.Get_User_By_External_Login(ProviderCode, SubjectId, Tracer);
            if (existingUser != null)
            {
                existingUser.Authentication_Type = AuthType;
                return existingUser;
            }

            // First sign-in through this provider - provision a new user from the claims
            User_Object newUser = new User_Object
            {
                External_Provider_Code = ProviderCode,
                External_Subject_Id = SubjectId,
                Authentication_Type = AuthType,
                Send_Email_On_Submission = true
            };

            foreach (KeyValuePair<string, string> claim in Claims)
            {
                User_Object_Attribute_Mapping_Enum mapping = GetMapping(claim.Key);
                if (mapping != User_Object_Attribute_Mapping_Enum.NONE)
                    newUser.Set_Value_By_Mapping(mapping, claim.Value);
            }

            foreach (SobekCM.Core.Configuration.Authentication.Attribute_Mapping_Entry constant in Constants)
                newUser.Set_Value_By_Mapping(constant.Mapping, constant.Value);

            if (String.IsNullOrEmpty(newUser.UserName))
                newUser.UserName = !String.IsNullOrEmpty(newUser.Email) ? newUser.Email : ProviderCode + ":" + SubjectId;

            bool saved = SobekCM_Database.Save_User(newUser, String.Empty, AuthType, Tracer);
            if (!saved)
                return null;

            User_Object provisionedUser = Engine_Database.Get_User_By_External_Login(ProviderCode, SubjectId, Tracer);
            if (provisionedUser != null)
                provisionedUser.Is_Just_Registered = true;

            return provisionedUser;
        }
    }
}
