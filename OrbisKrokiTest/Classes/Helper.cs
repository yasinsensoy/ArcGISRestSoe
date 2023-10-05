using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace OrbisKrokiTest.Classes
{
    public static class Helper
    {
        public static string GetConfigValue(string configValue, string defaultValue)
        {
            if (string.IsNullOrEmpty(configValue))
                return defaultValue;
            return configValue;
        }

        public static Uri CombineUri(string baseUri, string relativeOrAbsoluteUri)
        {
            if (!baseUri.EndsWith("/"))
                baseUri += "/";
            if (relativeOrAbsoluteUri.StartsWith("/"))
                relativeOrAbsoluteUri = relativeOrAbsoluteUri.Substring(1);
            return new Uri(baseUri + relativeOrAbsoluteUri);
        }

        public static string CombineUriToString(string baseUri, string relativeOrAbsoluteUri)
        {
            return CombineUri(baseUri, relativeOrAbsoluteUri).ToString();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetCurrentMethodFullName()
        {
            StackTrace st = new StackTrace();
            StackFrame sf = st.GetFrame(1);
            var method = sf.GetMethod();
            string fullName = method.DeclaringType.FullName + "." + method.Name;
            return fullName;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetCurrentMethodName()
        {
            StackTrace st = new StackTrace();
            StackFrame sf = st.GetFrame(1);
            return sf.GetMethod().Name;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MethodBase GetCurrentMethod()
        {
            StackTrace st = new StackTrace();
            StackFrame sf = st.GetFrame(1);
            return sf.GetMethod();
        }
    }
}
