using System.Text.RegularExpressions;

namespace SNBS.Licensing.Utilities
{
    internal static class Check
    {
        private const string keyPattern = @"([A-Z]|[a-z]|[0-9])+-([A-Z]|[a-z]|[0-9])+-([A-Z]|[a-z]|[0-9])+-([A-Z]|[a-z]|[0-9])+-([A-Z]|[a-z]|[0-9])+";

        internal static void Disposed(bool isDisposed, object obj)
        {
            ObjectDisposedException.ThrowIf(isDisposed, obj);
        }

        internal static void Null(object val, string name)
        {
            ArgumentNullException.ThrowIfNull(val, name);
        }

        internal static void Negative(long val)
        {
            if (val <= 0) ThrowHelper.DevicesNumberOutOfRange();
        }

        internal static void Key(string key)
        {
            if (key.Length != 29) ThrowHelper.InvalidKey();

            var match = Regex.Match(key, keyPattern,
                RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant);

            if (!match.Success || match.Index != 0 || match.Length != 29) ThrowHelper.InvalidKey();
        }
    }
}
