using System;
using libspotifydotnet;
using Spotbox.Player.Libspotifydotnet;

namespace Spotbox.Player.Spotify
{
    public class User
    {
        public IntPtr UserPtr { get; private set; }
        public string CanonicalName { get; private set; }
        public string DisplayName { get; private set; }
        public string FullName { get; private set; }        

        public User(IntPtr userPtr)
        {
            UserPtr = userPtr;
            Wait.For(() => libspotify.sp_user_is_loaded(userPtr), 10);

            CanonicalName = Functions.PtrToString(libspotify.sp_user_canonical_name(userPtr));
            DisplayName = Functions.PtrToString(libspotify.sp_user_display_name(userPtr));

            try
            {
                FullName = Functions.PtrToString(libspotify.sp_user_full_name(userPtr));
            }
            catch (Exception)
            {
                Console.WriteLine("User does not have a full name set");
            }

        }
    }
}
