using System;
using System.Reflection;

using libspotifydotnet;

using log4net;

namespace Spotbox.Player.Spotify
{
    public class User
    {
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public User(IntPtr userPtr)
        {
            UserPtr = userPtr;
            Wait.For(() => libspotify.sp_user_is_loaded(userPtr), 10);
            SetUserData(userPtr);
        }

        public IntPtr UserPtr { get; private set; }

        public string CanonicalName { get; private set; }

        public string DisplayName { get; private set; }

        public string FullName { get; private set; }        


        private void SetUserData(IntPtr userPtr)
        {
            CanonicalName = libspotify.sp_user_canonical_name(userPtr).PtrToString();
            DisplayName = libspotify.sp_user_display_name(userPtr).PtrToString();
        }
    }
}
