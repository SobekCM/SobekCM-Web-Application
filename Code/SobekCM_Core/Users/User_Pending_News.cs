using System.Collections.Generic;

namespace SobekCM.Core.Users
{
    /// <summary> The news still waiting to be closed by one logged-on user, kept in the session so the
    /// database is only checked on the first page after logging on </summary>
    /// <remarks> Stored via <c>Context.SessionObject()[SessionCache_Keys.PendingNews]</c>.  The user id is
    /// kept so a different user logging on in the same session (or a system admin viewing the site
    /// as another user) triggers a fresh check. </remarks>
    public class User_Pending_News
    {
        /// <summary> User this news was loaded for </summary>
        public int UserID { get; private set; }

        /// <summary> News this user has not closed yet, newest first </summary>
        public List<User_News_Item> Items { get; private set; }

        /// <summary> Constructor for a new instance of the User_Pending_News class </summary>
        /// <param name="UserID"> User this news was loaded for </param>
        /// <param name="Items"> News this user has not closed yet, newest first </param>
        public User_Pending_News(int UserID, List<User_News_Item> Items)
        {
            this.UserID = UserID;
            this.Items = Items ?? new List<User_News_Item>();
        }

        /// <summary> Copy of the pending news, safe to step through while another request closes one </summary>
        public List<User_News_Item> Snapshot()
        {
            lock (Items)
            {
                return new List<User_News_Item>(Items);
            }
        }

        /// <summary> Removes a news item, once the user closes it </summary>
        /// <param name="NewsID"> Primary key for the news item closed </param>
        public void Remove(int NewsID)
        {
            lock (Items)
            {
                Items.RemoveAll(Item => Item.NewsID == NewsID);
            }
        }
    }
}
