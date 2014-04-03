﻿/*-
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
using System.Runtime.InteropServices;

namespace Jamcast.Plugins.Spotify.API {

    internal class Image : IDisposable {

        private image_loaded_cb_delegate _d;
        private IntPtr _callbackPtr;
        private bool _disposed;

        internal bool IsLoaded { get; private set; }
        internal IntPtr ImagePtr { get; private set; }

        internal static Image Load(IntPtr ptr) {

            Image image = new Image();
            image.ImagePtr = ptr;
            image.beginLoad();

            return image;

        }

        private Image() { }

        #region IDisposable Members

        public void Dispose() {

            dispose(true);
            GC.SuppressFinalize(this);

        }

        ~Image() {

            dispose(false);

        }

        private void dispose(bool disposing) {

            if (!_disposed) {

                if (disposing) {

                    safeReleaseImage();

                }

                _disposed = true;

            }

        }

        #endregion

        private void safeReleaseImage() {

            if (ImagePtr != IntPtr.Zero) {

                try {

                    libspotify.sp_image_release(ImagePtr);

                } catch { }

            }

        }


        private void beginLoad() {

            _d = new image_loaded_cb_delegate(onImageLoaded);
            _callbackPtr = Marshal.GetFunctionPointerForDelegate(_d);
            libspotify.sp_image_add_load_callback(ImagePtr, _callbackPtr, IntPtr.Zero);


        }

        private void onImageLoaded(IntPtr imagePtr, IntPtr userDataPtr) {
            
            libspotify.sp_image_remove_load_callback(ImagePtr, _callbackPtr, IntPtr.Zero);
            IsLoaded = true;

        }

    }
}
