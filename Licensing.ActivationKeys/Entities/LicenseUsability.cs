namespace SNBS.Licensing.Entities
{
    /// <summary>
    /// Represents the reason why a license is not usable (also can represent the fact that it is usable).
    /// </summary>
    public enum LicenseUsability
    {
        /// <summary>
        /// License can be used.
        /// </summary>
        Usable,

        /// <summary>
        /// License doesn't exist in the database and thus cannot be used.
        /// </summary>
        NotFound,

        /// <summary>
        /// License has expired and thus cannot be used.
        /// </summary>
        Expired,

        /// <summary>
        /// License is used by as many devices as it can be used by, thus a new device cannot use it.
        /// </summary>
        TooManyDevices,

        /// <summary>
        /// No license configuration in the registry.
        /// </summary>
        NoConfiguredLicense
    }
}
