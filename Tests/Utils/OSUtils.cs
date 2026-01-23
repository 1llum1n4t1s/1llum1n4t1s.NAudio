using System;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NAudioTests.Utils
{
    static class OSUtils
    {
        public static void RequireVista()
        {
            if (Environment.OSVersion.Version.Major < 6)
            {
                ClassicAssert.Ignore("This test requires Windows Vista or newer");
            }
        }

        public static void RequireXP()
        {
            if (Environment.OSVersion.Version.Major >= 6)
            {
                ClassicAssert.Ignore("This test requires Windows XP");
            }
        }
    }
}
