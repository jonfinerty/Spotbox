using System;
using System.Threading;

namespace Spotbox.Player.Spotify
{
    public class Wait
    {        
        public static bool For(Func<bool> isFinishedTest, int timeout)
        {
            var start = DateTime.Now;

            while (DateTime.Now.Subtract(start).Seconds < timeout)
            {
                if (isFinishedTest.Invoke())
                {
                    return true;
                }
                Thread.Sleep(50);
            }

            return false;
        }
    }
}
