using SNBS.Licensing.Entities.Exceptions;

namespace SNBS.Licensing.Utilities
{
    internal class ThrowHelper
    {
        private const string formatExMessage = "The key is not valid. Valid key format is:\n" +
            "XXXXX-XXXXX-XXXXX-XXXXX-XXXXX,\nwhere X is an uppercase English letter or a digit.";
        private const string overflowExMessage = "Cannot generate a unique key. Too many attempts. " +
            "Try re-performing the operation.";
        private const string devNumOutOfRngExMessgae = "Number of devices cannot be negative " +
            "or equal to zero.";
        private const string maxDevNotLessThanUsgDevExMessage = "The maximum number of devices " +
            "should not be less than the number of currently using devices.";

        internal static void DatabaseError(Exception inner)
        {
            var exception = new DatabaseException(inner);

            throw exception;
        }

        internal static void RegistryInaccessible(Exception inner, string inaccessibleKey)
        {
            var exception = new RegistryAccessException(inner);
            exception.Data["InaccessibleRegistryKey"] = inaccessibleKey;

            throw exception;
        }

        internal static void InvalidKey()
        {
            throw new FormatException(formatExMessage);
        }

        internal static void Overflow()
        {
            throw new OverflowException(overflowExMessage);
        }

        internal static void DevicesNumberOutOfRange()
        {
            throw new ArgumentOutOfRangeException(devNumOutOfRngExMessgae);
        }

        internal static void MaxDevicesNotLessThanUsingDevices()
        {
            throw new ArgumentOutOfRangeException(maxDevNotLessThanUsgDevExMessage);
        }
    }
}
