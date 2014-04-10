using System;
using libspotifydotnet;

namespace SpotSharp
{
    public class User
    {
        public User(IntPtr userPtr)
        {
            UserPtr = userPtr;
            Wait.For(() => libspotify.sp_user_is_loaded(userPtr));
            SetUserData(userPtr);
        }

        internal IntPtr UserPtr { get; private set; }

        public string CanonicalName { get; private set; }

        public string DisplayName { get; private set; }

        public string FullName { get; private set; }        


        private void SetUserData(IntPtr userPtr)
        {
            CanonicalName = Extensions.PtrToString(libspotify.sp_user_canonical_name(userPtr));
            DisplayName = Extensions.PtrToString(libspotify.sp_user_display_name(userPtr));
        }
    }
}
