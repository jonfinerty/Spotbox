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
using libspotifydotnet;

namespace Jamcast.Plugins.Spotify.API {
    
    public class TopList : IDisposable {

        private bool _disposed;
        private IntPtr _browsePtr;

        public delegate void toplistbrowse_complete_cb_delegate(IntPtr result, IntPtr userDataPtr);
        
        public bool IsLoaded { get; private set; }
        public List<IntPtr> Ptrs { get; private set; }
        public libspotify.sp_toplisttype ToplistType { get; private set; }

        #region IDisposable Members

        public void Dispose() {

            dispose(true);
            GC.SuppressFinalize(this);

        }

        ~TopList() {

            dispose(false);

        }

        private void dispose(bool disposing) {

            if (!_disposed) {

                if (disposing) {

                    safeReleaseToplist();

                }

                _disposed = true;

            }

        }

        #endregion

        public static TopList BeginBrowse(libspotify.sp_toplisttype type, int region) {

            try {

                TopList t = new TopList();
                t.ToplistType = type;
                toplistbrowse_complete_cb_delegate d = new toplistbrowse_complete_cb_delegate(t.toplistbrowse_complete);
                IntPtr callbackPtr = Marshal.GetFunctionPointerForDelegate(d);

                t._browsePtr = libspotify.sp_toplistbrowse_create(Session.GetSessionPtr(), type, region, IntPtr.Zero, callbackPtr, IntPtr.Zero);
                return t;

            } catch (Exception ex) {

                Console.WriteLine( "TopList.BeginBrowse() failed: {0}", ex.Message);
                return null;

            }

        }

        private void toplistbrowse_complete(IntPtr result, IntPtr userDataPtr) {

            try {

                libspotify.sp_error error = libspotify.sp_toplistbrowse_error(result);

                if (error != libspotify.sp_error.OK) {

                    Console.WriteLine( "ERROR: Toplist browse failed: {0}", libspotify.sp_error_message(error));
                    return;

                }

                int count = ToplistType == libspotify.sp_toplisttype.SP_TOPLIST_TYPE_ALBUMS ? libspotify.sp_toplistbrowse_num_albums(_browsePtr) : ToplistType == libspotify.sp_toplisttype.SP_TOPLIST_TYPE_ARTISTS ? libspotify.sp_toplistbrowse_num_artists(_browsePtr) : libspotify.sp_toplistbrowse_num_tracks(_browsePtr);

                List<IntPtr> ptrs = new List<IntPtr>();

                IntPtr tmp = IntPtr.Zero;

                for (int i = 0; i < count; i++) {

                    if (ToplistType == libspotify.sp_toplisttype.SP_TOPLIST_TYPE_ALBUMS) {

                        tmp = libspotify.sp_toplistbrowse_album(_browsePtr, i);
                        if(libspotify.sp_album_is_available(tmp))
                            ptrs.Add(tmp);

                    } else if (ToplistType == libspotify.sp_toplisttype.SP_TOPLIST_TYPE_ARTISTS) {

                        tmp = libspotify.sp_toplistbrowse_artist(_browsePtr, i);
                        ptrs.Add(tmp);

                    } else {

                        tmp = libspotify.sp_toplistbrowse_track(_browsePtr, i);
                        ptrs.Add(tmp);

                    }

                }

                Ptrs = ptrs;

                IsLoaded = true;

            } finally {

                safeReleaseToplist();

            }

        }

        private void safeReleaseToplist() {

            if (_browsePtr != IntPtr.Zero) {

                try {

                    //libspotify.sp_toplistbrowse_release(_browsePtr);

                } catch { }

            }

        }
        
    }

}
