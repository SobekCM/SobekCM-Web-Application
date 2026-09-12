using SobekCM.Core.Users;

namespace SobekCM.Core.MemoryMgmt
{
    /// <summary> The one question every subnet rate limiter asks about the user: is this request logged on,
    /// and so compared against the more permissive ceiling? </summary>
    /// <remarks> The subnet key itself is read with <see cref="ClientSubnetKey.From"/>. </remarks>
    public static class AnonymousRequest
    {
        /// <summary> Whether there is a user on this request and they are actually logged on </summary>
        /// <remarks> Only worth trusting as "this is a person with an account", never as "this is a person
        /// who won't hammer us": a logged-on session is carried by a cookie, and a cookie can be exported
        /// out of a browser and handed to a scraper. That's why both the JP2 and sustained budgets give a
        /// logged-on request a higher ceiling rather than exempting it. </remarks>
        public static bool Is_Logged_On(User_Object CurrentUser)
        {
            return (CurrentUser != null) && (CurrentUser.LoggedOn);
        }
    }
}
