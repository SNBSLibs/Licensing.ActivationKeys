using System.Text;

namespace SNBS.Licensing.Utilities
{
    internal class KeyGen
    {
        private static readonly char[] chars = new char[] {
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N',
            'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b',
            'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p',
            'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '0', '1', '2', '3',
            '4', '5', '6', '7', '8', '9'
        };
        private static Random generator = new();

        internal static string NewKey(LicensingDbContext context)
        {
            int attempts = 0;

            while (true)
            {
                var keyBuilder = new StringBuilder(29);

                for (int i = 0; i < 5; i++)
                {
                    for (int j = 0; j < 5; j++)
                    {
                        char ch = chars[generator.Next(chars.Length)];
                        keyBuilder.Append(ch);
                    }

                    keyBuilder.Append('-');
                }

                string keyRaw = keyBuilder.ToString();
                string key = keyRaw.Substring(0, keyRaw.Length - 1);

                if (context.Licenses.Find(key) == null) return key;
                else attempts++;

                if (attempts >= 5) ThrowHelper.Overflow();
            }
        }
    }
}
