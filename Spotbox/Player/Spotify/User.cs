/*-
 * Copyright (c) 2012 Software Development Solutions, Inc.
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR AND CONTRIBUTORS ``AS IS'' AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
 * OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
 */

using System;
using libspotifydotnet;

namespace Spotbox.Player.Spotify {

    public class User {

        public string CanonicalName { get; private set; }
        public string DisplayName { get; private set; }
        public string FullName { get; private set; }
        public IntPtr UserPtr { get; private set; }

        public User(IntPtr userPtr) {

            if(!libspotify.sp_user_is_loaded(userPtr))
                throw new InvalidOperationException("User is not loaded.");

            UserPtr = userPtr;
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
