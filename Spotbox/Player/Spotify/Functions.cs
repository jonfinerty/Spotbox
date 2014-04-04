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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using libspotifydotnet;

namespace Spotbox.Player.Spotify {

    public static class Functions {

        internal static string PtrToString(IntPtr ptr) {

            if (ptr == IntPtr.Zero)
                return String.Empty;

            List<byte> l = new List<byte>();
            byte read = 0;
            do {
                read = Marshal.ReadByte(ptr, l.Count);
                l.Add(read);
            }
            while (read != 0);

            if (l.Count > 0)
                return Encoding.UTF8.GetString(l.ToArray(), 0, l.Count - 1);
            else
                return string.Empty;
        }

        internal static string LinkToString(IntPtr linkPtr) {

            byte[] buffer = new byte[128];
            IntPtr bufferPtr = IntPtr.Zero;

            try {

                bufferPtr = Marshal.AllocHGlobal(buffer.Length);

                int i = libspotify.sp_link_as_string(linkPtr, bufferPtr, buffer.Length);

                if (i == 0)
                    return null;

                Marshal.Copy(bufferPtr, buffer, 0, buffer.Length);

                return Encoding.UTF8.GetString(buffer, 0, i);

            } finally {

                try {

                    if (bufferPtr != IntPtr.Zero)
                        Marshal.FreeHGlobal(bufferPtr);

                } catch { }

            }

        }

        internal static string GetCountryName(int country) {

            string countryCode = Encoding.ASCII.GetString(new byte[] { (byte)(country >> 8), (byte)(country & 0xff) });
            
            switch (countryCode) {

                case "US":

                    return "the United States";

                case "SE":

                    return "Sweden";

                case "FI":

                    return "Finland";

                case "ES":

                    return "Spain";

                case "FR":

                    return "France";

                case "NO":

                    return "Norway";

                case "GB":

                    return "the United Kingdom";

                case "NL":

                    return "the Netherlands";

                case "DK":

                    return "Denmark";

                case "AT":

                    return "Austria";

                case "BE":

                    return "Belgium";

                case "CH":

                    return "Switzerland";

                case "DE":

                    return "Germany";

                case "AU" :

                    return "Australia";

                case "NZ" :

                    return "New Zealand";

                default:

                    return "My Country";

            }

        }

    }

}
