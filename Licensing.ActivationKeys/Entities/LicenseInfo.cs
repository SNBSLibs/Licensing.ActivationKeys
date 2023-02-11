namespace SNBS.Licensing.Entities
{
    /// <summary>
    /// This structure contains detailed information about one license.
    /// </summary>
    public struct LicenseInfo
    {
        /// <summary>
        /// Don't use this constructor. It exists only because structures always have a constructor without parameters.
        /// </summary>
        /// <exception cref="InvalidOperationException">Always throws this exception.</exception>
        public LicenseInfo()
        {
            throw new InvalidOperationException("Instances of this structure should not be created in client code.");
        }

        internal LicenseInfo(License? license, LicenseUsability usability)
        {
            Usability = usability;

            if (usability == LicenseUsability.Usable)
            {
                Type = license?.Type;
                Expiration = license?.Expiration;
                License = license;
                MaxDevices = license?.MaxDevices;
                Key = license?.Key;
            } else
            {
                Type = null;
                Expiration = null;
                License = null;
                MaxDevices = null;
                Key = null;
            }
        }

        /// <summary>
        /// Key of the license that has its information stored in the current instance of <see cref="LicenseInfo"/>.
        /// </summary>
        public string? Key { get; internal set; }

        /// <summary>
        /// Usability of the license that has its information stored in the current instance of <see cref="LicenseInfo"/>.
        /// </summary>
        public LicenseUsability Usability { get; internal set; }

        /// <summary>
        /// Type of the license that has its information stored in the current instance of <see cref="LicenseInfo"/>.
        /// </summary>
        public LicenseType? Type { get; internal set; }

        /// <summary>
        /// Expiration date of the license that has its information stored in the current instance of <see cref="LicenseInfo"/>.
        /// </summary>
        public DateTime? Expiration { get; internal set; }

        /// <summary>
        /// Maximum number of devices which can use the license that has its information stored in the current instance of <see cref="LicenseInfo"/>.
        /// </summary>
        public short? MaxDevices { get; internal set; }

        internal License? License { get; set; }
    }
}
