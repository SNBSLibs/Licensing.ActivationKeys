using SNBS.Licensing.Entities;
using SNBS.Licensing.Entities.Exceptions;
using SNBS.Licensing.Utilities;

namespace SNBS.Licensing
{
    /// <summary>
    /// This class can fetch information about licenses from the database.
    /// </summary>
    public class LicenseValidator : IDisposable
    {
        private LicensingDbContext context;
        internal string connectionString;
        internal bool useMySql;
        internal Version? mySqlVersion;
        private bool isDisposed = false;

        /// <summary>
        /// Creates an instance of <see cref="LicenseValidator"/> and connects to the licenses database.
        /// </summary>
        /// <param name="connectionString">
        /// The connection string to be used to connect to the database.
        /// </param>
        /// <param name="useMySql">
        /// If <c>false</c>, the DBMS is supposed to be MS SQL Server. If <c>true</c>, the DBMS is supposed to be MySQL.
        /// </param>
        /// <param name="mySqlVersion">
        /// If MySQL is used, this parameter should contain the MySQL version. If MS SQL Server is used, this parameter should be <c>null</c>.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the MySQL version is not specified, but MySQL should be used as the DBMS.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="connectionString"/> is <c>null</c>.
        /// </exception>
        public LicenseValidator(string connectionString, bool useMySql, Version? mySqlVersion)
        {
            Check.Null(connectionString, nameof(connectionString));

            if (useMySql && mySqlVersion == null)
            {
                throw new InvalidOperationException("You should specify the needed MySQL version when using MySQL.");
            }

            this.useMySql = useMySql;
            this.mySqlVersion = mySqlVersion;
            this.connectionString = connectionString;
            context = new(connectionString, useMySql, mySqlVersion);
        }

        /// <summary>
        /// Fetches the information about the license that has the specified key.
        /// </summary>
        /// <param name="key">
        /// Key of the license to fetch information about.
        /// </param>
        /// <returns>
        /// The information about the license with the specified key, stored in a <see cref="LicenseInfo"/> instance.
        /// </returns>
        /// <exception cref="DatabaseException">
        /// Thrown if there's an issue accessing the licenses database.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown if the current <see cref="LicenseValidator"/> instance was disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the <paramref name="key"/> parameter is <c>null</c>.
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown if the key provided in parameter <paramref name="key"/> is invalid.
        /// </exception>
        public LicenseInfo ValidateLicense(string key)
        {
            Check.Disposed(isDisposed, this);
            Check.Null(key, nameof(key));
            Check.Key(key);

            License? license = null;
            try
            {
                license = context.Licenses.Find(key);
            } catch (Exception ex)
            {
                ThrowHelper.DatabaseError(ex);
            }

            if (license == null)
            {
                return new(null, LicenseUsability.NotFound);
            }
            else
            {
                if (license.Expiration < DateTime.Today)
                {
                    return new(null, LicenseUsability.Expired);
                }
                else if (license.UsingDevices >= license.MaxDevices)
                {
                    return new(null, LicenseUsability.TooManyDevices);
                }
                else
                {
                    return new(license, LicenseUsability.Usable);
                }
            }
        }

        /// <summary>
        /// Casts a <see cref="LicenseValidator"/> instance to a <see cref="LicensingAdmin"/> instance.
        /// </summary>
        /// <param name="validator">
        /// The initial <see cref="LicenseValidator"/> instance.
        /// </param>
        public static explicit operator LicensingAdmin(LicenseValidator validator)
        {
            return new(validator.connectionString, validator.useMySql, validator.mySqlVersion);
        }

#region Async
        /// <summary>
        /// Asynchronously fetches the information about the license that has the specified key.
        /// </summary>
        /// <param name="key">
        /// Key of the license to fetch information about.
        /// </param>
        /// <returns>
        /// The information about the license with the specified key, stored in a <see cref="LicenseInfo"/> instance.
        /// </returns>
        /// <exception cref="DatabaseException">
        /// Thrown if there's an issue accessing the licenses database.
        /// </exception>
        public Task<LicenseInfo> ValidateLicenseAsync(string key)
        {
            return Task.Run(() => ValidateLicense(key));
        }
#endregion

        /// <summary>
        /// Releases all resources used by the current <see cref="LicenseValidator"/> instance.
        /// </summary>
        public void Dispose()
        {
            isDisposed = true;

            context.Dispose();
        }
    }
}