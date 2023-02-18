using SNBS.Licensing.Entities;
using SNBS.Licensing.Entities.Exceptions;
using SNBS.Licensing.Utilities;

namespace SNBS.Licensing
{
    /// <summary>
    /// This class can add, update and delete licenses in the database.
    /// </summary>
    public class LicensingAdmin : IDisposable
    {
        private LicensingDbContext context;
        internal string connectionString;
        internal bool useMySql;
        internal Version? mySqlVersion;
        private bool isDisposed = false;

        /// <summary>
        /// Creates an instance of <see cref="LicensingAdmin"/> and connects to the licenses database.
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
        public LicensingAdmin(string connectionString, bool useMySql, Version? mySqlVersion)
        {
            Check.Null(connectionString, nameof(connectionString));

            this.connectionString = connectionString;
            this.useMySql = useMySql;
            this.mySqlVersion = mySqlVersion;
            context = new(connectionString, useMySql, mySqlVersion);
        }

        /// <summary>
        /// Adds a new license to the database.
        /// </summary>
        /// <param name="expiration">
        /// Expiration date of the new license.
        /// </param>
        /// <param name="type">
        /// Type of the new license.
        /// </param>
        /// <param name="maxDevices">
        /// Maximum number of devices that can use the new license.
        /// </param>
        /// <returns>
        /// Information about the new license, packed into a <see cref="LicenseInfo"/> instance.
        /// </returns>
        /// <exception cref="DatabaseException">
        /// Thrown if there's an issue connecting to the licenses database.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown if the current <see cref="LicensingAdmin"/> instance is disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if a parameter is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="maxDevices"/> is negative or equal to 0.
        /// </exception>
        /// <exception cref="OverflowException">
        /// Thrown if this library cannot generate a unique key for a new license (too many attempts). You can recall this method to fix this (to make more attempts). It isn't recalled automatically to prevent hanging.
        /// </exception>
        public LicenseInfo CreateLicense(DateTime expiration, LicenseType type, short maxDevices)
        {
            Check.Disposed(isDisposed, this);
            Check.Null(expiration, nameof(expiration));
            Check.Null(type, nameof(type));
            Check.Null(maxDevices, nameof(maxDevices));
            Check.Negative(maxDevices);

            var license = new License
            {
                Expiration = expiration,
                MaxDevices = maxDevices,
                Type = type,
                UsingDevices = 0
            };
            license.Key = KeyGen.NewKey(context);

            context.Licenses.Add(license);

            ChangesSaver.SaveChanges(context);

            return new(license, GetUsability(license));
        }

        /// <summary>
        /// Updates a license with the specified key.
        /// </summary>
        /// <param name="key">Key of the license to update.</param>
        /// <param name="expiration">
        /// The new expiration date. Pass <c>null</c> to leave the expiration date as it was.
        /// </param>
        /// <param name="type">
        /// The new type. Pass <c>null</c> to leave the type as it was.
        /// </param>
        /// <param name="maxDevices">
        /// The new maximum number of devices (that can use the license). Pass <c>null</c> to leave it as it was.
        /// </param>
        /// <returns>
        /// Updated information about the license, packed into a <see cref="LicenseInfo"/> instance.
        /// </returns>
        /// <exception cref="DatabaseException">
        /// Thrown if there's an issue connecting to the licenses database.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown if the current <see cref="LicensingAdmin"/> instance is disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="key"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="maxDevices"/> is negative, equal to 0 or less than the number of devices that are currently using the license.
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown if the key provided in parameter <paramref name="key"/> is invalid.
        /// </exception>
        public LicenseInfo UpdateLicense(string key, DateTime? expiration, LicenseType? type, short? maxDevices)
        {
            Check.Disposed(isDisposed, this);
            Check.Null(key, nameof(key));
            if (maxDevices != null) Check.Negative((long)maxDevices);
            Check.Key(key);

            License? license = null;
            try
            {
                license = context.Licenses.Find(key);
            } catch (Exception ex)
            {
                ThrowHelper.DatabaseError(ex);
            }

            if (license == null) return new(null, LicenseUsability.NotFound);

            if (expiration != null) license.Expiration = (DateTime)expiration;
            if (maxDevices != null)
            {
                if (maxDevices >= license.UsingDevices)
                {
                    license.MaxDevices = (short)maxDevices;
                }
                else ThrowHelper.MaxDevicesNotLessThanUsingDevices();
            }
            if (type != null) license.Type = (LicenseType)type;

            ChangesSaver.SaveChanges(context);

            return new(license, GetUsability(license));
        }

        /// <summary>
        /// Deletes a license from the licenses database.
        /// </summary>
        /// <param name="key">Key of the license to delete.</param>
        /// <returns>
        /// Information about the deleted license, packed into a <see cref="LicenseInfo"/> instance.
        /// </returns>
        /// <exception cref="DatabaseException">
        /// Thrown if there's an issue connecting to the licenses database.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown if the current <see cref="LicensingAdmin"/> instance is disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="key"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown if the key provided in parameter <paramref name="key"/> is invalid.
        /// </exception>
        public LicenseInfo DeleteLicense(string key)
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

            if (license == null) return new(null, LicenseUsability.NotFound);

            context.Licenses.Remove(license);

            ChangesSaver.SaveChanges(context);

            return new(license, GetUsability(license));
        }

        /// <summary>
        /// Casts a <see cref="LicensingAdmin"/> instance to a <see cref="LicenseValidator"/> instance. 
        /// </summary>
        /// <param name="admin">
        /// The initial <see cref="LicensingAdmin"/> instance.
        /// </param>
        public static explicit operator LicenseValidator(LicensingAdmin admin)
        {
            return new(admin.connectionString, admin.useMySql, admin.mySqlVersion);
        }

        /// <summary>
        /// Finds licenses with expiration date earlier than specified in parameter <paramref name="backDateTo"/> (usually that's old, expired, no longer needed licenses) and deletes them.
        /// </summary>
        /// <param name="backDateTo">
        /// The maximum expiration date a license needs to have to be deleted.
        /// </param>
        /// <exception cref="ObjectDisposedException">
        /// Thrown if the current <see cref="LicensingClient"/> instance was disposed.
        /// </exception>
        /// <exception cref="DatabaseException">
        /// Thrown if there's an issue connecting to the licenses database.
        /// </exception>
        public void DeleteOldLicenses(DateTime backDateTo)
        {
            Check.Disposed(isDisposed, this);

            context.Licenses.RemoveRange
                (context.Licenses.Where(l => l.Expiration <= backDateTo));


            ChangesSaver.SaveChanges(context);
        }

        /// <summary>
        /// Finds licenses that have expired more than the specified number of days ago and deletes them.
        /// </summary>
        /// <param name="days">
        /// A license must have expired this number of days ago to be deleted.
        /// </param>
        /// <exception cref="ObjectDisposedException">
        /// Thrown if the current <see cref="LicensingClient"/> instance was disposed.
        /// </exception>
        /// <exception cref="DatabaseException">
        /// Thrown if there's an issue connecting to the licenses database.
        /// </exception>
        public void DeleteOldLicenses(short days)
        {
            Check.Disposed(isDisposed, this);

            var backDateTo = DateTime.Today - TimeSpan.FromDays(days);
            DeleteOldLicenses(backDateTo);
        }

#region Async
        /// <summary>
        /// Asynchronously adds a new license to the database.
        /// </summary>
        /// <param name="expiration">
        /// Expiration date of the new license.
        /// </param>
        /// <param name="type">
        /// Type of the new license.
        /// </param>
        /// <param name="maxDevices">
        /// Maximum number of devices that can use the new license.
        /// </param>
        /// <returns>
        /// Information about the new license, packed into a <see cref="LicenseInfo"/> instance.
        /// </returns>
        /// <exception cref="DatabaseException">
        /// Thrown if there's an issue connecting to the licenses database.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown if the current <see cref="LicensingAdmin"/> instance is disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if a parameter is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="maxDevices"/> is negative or equal to 0.
        /// </exception>
        public Task<LicenseInfo> CreateLicenseAsync(DateTime expiration, LicenseType type, short maxDevices)
        {
            return Task.Run(() => CreateLicense(expiration, type, maxDevices));
        }

        /// <summary>
        /// Asynchronously updates a license with the specified key.
        /// </summary>
        /// <param name="key">Key of the license to update.</param>
        /// <param name="expiration">
        /// The new expiration date. Pass <c>null</c> to leave the expiration date as it was.
        /// </param>
        /// <param name="type">
        /// The new type. Pass <c>null</c> to leave the type as it was.
        /// </param>
        /// <param name="maxDevices">
        /// The new maximum number of devices (that can use the license). Pass <c>null</c> to leave it as it was.
        /// </param>
        /// <returns>
        /// Updated information about the license, packed into a <see cref="LicenseInfo"/> instance.
        /// </returns>
        /// <exception cref="DatabaseException">
        /// Thrown if there's an issue connecting to the licenses database.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown if the current <see cref="LicensingAdmin"/> instance is disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="key"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="maxDevices"/> is negative, equal to 0 or less than the number of devices that are currently using the license.
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown if the key provided in parameter <paramref name="key"/> is invalid.
        /// </exception>
        public Task<LicenseInfo> UpdateLicenseAsync(string key, DateTime? expiration, LicenseType? type, short? maxDevices)
        {
            return Task.Run(() => UpdateLicense(key, expiration, type, maxDevices));
        }

        /// <summary>
        /// Asynchronously deletes a license from the licenses database.
        /// </summary>
        /// <param name="key">Key of the license to delete.</param>
        /// <returns>
        /// Information about the deleted license, packed into a <see cref="LicenseInfo"/> instance.
        /// </returns>
        /// <exception cref="DatabaseException">
        /// Thrown if there's an issue connecting to the licenses database.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown if the current <see cref="LicensingAdmin"/> instance is disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="key"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown if the key provided in parameter <paramref name="key"/> is invalid.
        /// </exception>
        public Task<LicenseInfo> DeleteLicenseAsync(string key)
        {
            return Task.Run(() => DeleteLicense(key));
        }
#endregion

        private LicenseUsability GetUsability(License license)
        {
            if (license.Expiration <= DateTime.Today) return LicenseUsability.Expired;
            return LicenseUsability.Usable;
        }

        /// <summary>
        /// Releases all resources used by the current <see cref="LicensingAdmin"/> instance.
        /// </summary>
        public void Dispose()
        {
            isDisposed = true;

            context.Dispose();
        }
    }
}