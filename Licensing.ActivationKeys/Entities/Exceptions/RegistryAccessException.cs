namespace SNBS.Licensing.Entities.Exceptions
{
	/// <summary>
	/// This exception is thrown when <see cref="LicensingClient"/> tries to access the registry to fetch/write the current activation key, but cannot do it because hasn't got enough permissions. The inaccessible registry key is stored in the exception data under key <c>InaccessibleRegistryKey</c>.
	/// </summary>
	[Serializable]
	public class RegistryAccessException : Exception
	{
		private static string message = "Cannot open the registry key where the current " +
			"license is stored due to low permissions.";

        /// <summary>
        /// This constructor creates an instance of <see cref="RegistryAccessException"/>,
        /// setting the inner exception.
        /// </summary>
        /// <param name="inner">The inner exception.</param>
        public RegistryAccessException(Exception inner) : base(message, inner) { }
	}
}
