using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Spotbox.Spotify
{
    static class Extensions
    {
        public static string PtrToString(this IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return string.Empty;
            }

            var bytes = new List<byte>();
            byte byteRead;
            do
            {
                byteRead = Marshal.ReadByte(ptr, bytes.Count);
                bytes.Add(byteRead);
            }
            while (byteRead != 0);

            if (bytes.Count > 0)
            {
                return Encoding.UTF8.GetString(bytes.ToArray(), 0, bytes.Count - 1);
            }

            return string.Empty;
        }

        public static IntPtr GetFunctionPtr(this Delegate inputDelegate)
        {
            return Marshal.GetFunctionPointerForDelegate(inputDelegate);
        }
    }
}
