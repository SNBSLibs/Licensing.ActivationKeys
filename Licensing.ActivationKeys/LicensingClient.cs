#pragma warning disable CS8618

using Microsoft.Win32;
using SNBS.Licensing.Entities;
using SNBS.Licensing.Entities.Exceptions;
using SNBS.Licensing.Utilities;

namespace SNBS.Licensing
{
    /// <summary>
    /// This class can configure licenses on the local machine.
    /// </summary>
    public class LicensingClient : IDisposable
    {
        private LicensingDbContext context;
        private RegistryKey regKey;
        private LicenseValidator validator;
        internal string connectionString;
        internal bool useMySql;
        internal Version? mySqlVersion;
        private string productName;
        private bool isDisposed = false;

        /// <summary>
        /// Creates an instance of <see cref="LicensingClient"/> and connects to the licenses database.
        /// </summary>
        /// <param name="connectionString">
        /// The connection string to be used to connect to the database.
        /// </param>
        /// <param name="productName">
        /// The name of your product. Used to separate different products using this library on the same machine.
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
        /// <exception cref="RegistryAccessException">
        /// Thrown if there aren't enough permissions to access the registry.
        /// </exception>
        /// <exception cref="DatabaseException">
        /// Thrown if there's an issue connecting to the licenses database.
        /// </exception>
        public LicensingClient(string connectionString, string productName, bool useMySql, Version? mySqlVersion = null)
        {
            Check.Null(connectionString, nameof(connectionString));
            Check.Null(productName, nameof(productName));

            if (useMySql && mySqlVersion == null) {
                throw new InvalidOperationException("You should specify the needed MySQL version when using MySQL.");
            }

            this.connectionString = connectionString;
            this.productName = productName;
            this.useMySql = useMySql;
            this.mySqlVersion = mySqlVersion;
            context = new(connectionString, useMySql, mySqlVersion);
            validator = new(connectionString, useMySql, mySqlVersion);

            try
            {
                this.regKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\SNBS\ActivationKeysLicensing\" + productName);
            } catch (UnauthorizedAccessException ex)
            {
                ThrowHelper.RegistryInaccessible(ex, @"SOFTWARE\SNBS\ActivationKeysLicensing\" + productName);
            }
        }

        /// <summary>
        /// Creates an instance of <see cref="LicensingClient"/> out of a <see cref="LicenseValidator"/> instance.
        /// </summary>
        /// <param name="validator">The <see cref="LicenseValidator"/> instance to use.</param>
        /// <param name="productName">
        /// The name of your product. Used to separate different products using this library on the same machine.
        /// </param>
        /// <exception cref="RegistryAccessException">
        /// Thrown if there aren't enough permissions to access the registry.
        /// </exception>
        /// <exception cref="DatabaseException">
        /// Thrown if there's an issue connecting to the licenses database.
        /// </exception>
        public LicensingClient(LicenseValidator validator, string productName)
            : this(validator.connectionString, productName, validator.useMySql, validator.mySqlVersion) {
            this.validator = validator;
        }

        /// <summary>
        /// Creates an instance of <see cref="LicensingClient"/> out of a <see cref="LicensingAdmin"/> instance.
        /// </summary>
        /// <param name="admin">The <see cref="LicensingAdmin"/> instance to use.</param>
        /// <param name="productName">
        /// The name of your product. Used to separate different products using this library on the same machine.
        /// </param>
        /// <exception cref="RegistryAccessException">
        /// Thrown if there aren't enough permissions to access the registry.
        /// </exception>
        /// <exception cref="DatabaseException">
        /// Thrown if there's an issue connecting to the licenses database.
        /// </exception>
        public LicensingClient(LicensingAdmin admin, string productName)
            : this(admin.connectionString, productName, admin.useMySql, admin.mySqlVersion) { }

        /// <summary>
        /// Fetches information about the license with the key specified. If it is usable, configures it in the registry.
        /// </summary>
        /// <param name="key">Key of the license.</param>
        /// <returns>
        /// Information about the license with the key specified, packed into a <see cref="LicenseInfo"/> instance.
        /// </returns>
        /// <exception cref="DatabaseException">
        /// Thrown if there's an issue accessing the licenses database.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown if the current <see cref="LicensingClient"/> instance was disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the <paramref name="key"/> parameter is <c>null</c>.
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown if the key provided in parameter <paramref name="key"/> is invalid.
        /// </exception>
        public LicenseInfo ActivateProduct(string key)
        {
            Check.Disposed(isDisposed, this);
            Check.Null(key, nameof(key));
            Check.Key(key);

            var validationResult = validator.ValidateLicense(key);

            if (validationResult.Usability == LicenseUsability.Usable)
            {
#pragma warning disable CS8602
                validationResult.License.UsingDevices++;
#pragma warning restore CS8602

                try
                {
                    context.SaveChanges();
                } catch (Exception ex)
                {
                    ThrowHelper.DatabaseError(ex);
                }

                regKey.SetValue("CurrentKey", key);
            }

            return validationResult;
        }

        /// <summary>
        /// Deletes the license configuration from the registry if it exists there.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Thrown if the current <see cref="LicensingClient"/> instance was disposed.
        /// </exception>
        /// <exception cref="DatabaseException">
        /// Thrown if there's an issue connecting to the licenses database.
        /// </exception>
        public void DeactivateProduct()
        {
            Check.Disposed(isDisposed, this);

            string? currentKey = regKey.GetValue("CurrentKey") as string;
            if (currentKey == null) return;

            regKey.SetValue("CurrentKey", string.Empty);

            var currentLicense = context.Licenses.Find(currentKey);
            if (currentLicense == null) return;
            if (currentLicense.UsingDevices > 0)
                currentLicense.UsingDevices--;

            try
            {
                context.SaveChanges();
            } catch (Exception ex)
            {
                ThrowHelper.DatabaseError(ex);
            }
        }

        /// <summary>
        /// Fetches the license configuration from the registry.
        /// </summary>
        /// <returns>
        /// Information about the license configured in the registry, packed into a <see cref="LicenseInfo"/> instance.
        /// </returns>
        /// <exception cref="DatabaseException">
        /// Thrown if there's an issue accessing the licenses database (needed to check if the configured license is usable).
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown if the current <see cref="LicensingClient"/> instance was disposed.
        /// </exception>
        public LicenseInfo GetCurrentLicense()
        {
            Check.Disposed(isDisposed, this);

            string? key = regKey.GetValue("CurrentKey") as string;

            if (string.IsNullOrEmpty(key))
            {
                return new(null, LicenseUsability.NoConfiguredLicense);
            }

            return validator.ValidateLicense(key);
        }

        /// <summary>
        /// Connects to the licenses database, fetches the license configuration from the registry, checks if the configured license is usable and depending on it runs one of the methods passed.
        /// </summary>
        /// <param name="connectionString">
        /// The connection string to be used to connect to the database.
        /// </param>
        /// <param name="productName">
        /// The name of your product. Used to separate different products using this library on the same machine.
        /// </param>
        /// <param name="useMySql">
        /// If <c>false</c>, the DBMS is supposed to be MS SQL Server. If <c>true</c>, the DBMS is supposed to be MySQL.
        /// </param>
        /// <param name="mySqlVersion">
        /// If MySQL is used, this parameter should contain the MySQL version. If MS SQL Server is used, this parameter should be <c>null</c>.
        /// </param>
        /// <param name="onLicensed">
        /// The method that should be ran if a usable license is configured. A <see cref="LicensingClient"/> instance is passed (connected to the database, ready to use).
        /// </param>
        /// <param name="onNotLicensed">
        /// The method that should be ran if there's no configured license (or there is one, but it isn't usable). A <see cref="LicensingClient"/> instance (connected to the database, ready to use) and a <see cref="LicenseUsability"/> value (representing the reason why the configured license isn't usable) are passed.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if a parameter (except <paramref name="mySqlVersion"/>) is <c>null</c>.
        /// </exception>
        /// <exception cref="DatabaseException">
        /// Thrown if there's an issue connecting to the licenses database.
        /// </exception>
        public static void Start(string connectionString, string productName, bool useMySql, Version? mySqlVersion,
            Action<LicensingClient> onLicensed, Action<LicensingClient, LicenseUsability> onNotLicensed)
        {
            Check.Null(connectionString, nameof(connectionString));
            Check.Null(productName, nameof(productName));
            Check.Null(onLicensed, nameof(onLicensed));
            Check.Null(onNotLicensed, nameof(onNotLicensed));

            var client = new LicensingClient(connectionString, productName, useMySql, mySqlVersion);
            var usability = client.GetCurrentLicense().Usability;

            if (usability == LicenseUsability.Usable)
            {
                onLicensed(client);
            } else
            {
                onNotLicensed(client, usability);
            }
        }

        /// <summary>
        /// Gets or sets the product name specified when the current <see cref="LicensingClient"/> instance was created. If it is set, the license configuration in the registry is automatically remapped to the new product name.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Thrown if the current <see cref="LicensingClient"/> instance was disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when setting this property to <c>null</c>.
        /// </exception>
        public string ProductName
        {
            get {
                Check.Disposed(isDisposed, this);

                return productName;
            }

            set
            {
                Check.Disposed(isDisposed, this);
                Check.Null(value, nameof(value));

                string? currentKey = regKey.GetValue("CurrentKey") as string;
                regKey.Close();

                regKey = Registry.LocalMachine.CreateSubKey
                    (@"SOFTWARE\SNBS\Licensing.ActivationKeys");

                if (regKey.GetSubKeyNames().Contains(productName)) regKey.DeleteSubKey(productName);

                var licensingKey = regKey;
                regKey = regKey.CreateSubKey(value);
                licensingKey.Close();

                productName = value;
            }
        }

        /// <summary>
        /// Casts a <see cref="LicensingClient"/> instance to a <see cref="LicenseValidator"/> instance. 
        /// </summary>
        /// <param name="client">
        /// The initial <see cref="LicensingClient"/> instance.
        /// </param>
        public static explicit operator LicenseValidator(LicensingClient client)
        {
            return new(client.connectionString, client.useMySql, client.mySqlVersion);
        }

        /// <summary>
        /// Casts a <see cref="LicensingClient"/> instance to a <see cref="LicensingAdmin"/> instance. 
        /// </summary>
        /// <param name="client">
        /// The initial <see cref="LicensingClient"/> instance.
        /// </param>
        public static explicit operator LicensingAdmin(LicensingClient client)
        {
            return new(client.connectionString, client.useMySql, client.mySqlVersion);
        }

#region Async
        /// <summary>
        /// Asynchronously etches information about the license with the key specified. If it is usable, configures it in the registry.
        /// </summary>
        /// <param name="key">Key of the license.</param>
        /// <returns>
        /// Information about the license with the key specified, packed into a <see cref="LicenseInfo"/> instance.
        /// </returns>
        /// <exception cref="DatabaseException">
        /// Thrown if there's an issue accessing the licenses database.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown if the current <see cref="LicensingClient"/> instance was disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the <paramref name="key"/> parameter is <c>null</c>.
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown if the key provided in parameter <paramref name="key"/> is invalid.
        /// </exception>
        public Task<LicenseInfo> ActivateProductAsync(string key)
        {
            return Task.Run(() => ActivateProduct(key));
        }

        /// <summary>
        /// Asynchronously fetches the license configuration from the registry.
        /// </summary>
        /// <returns>
        /// Information about the license configured in the registry, packed into a <see cref="LicenseInfo"/> instance.
        /// </returns>
        /// <exception cref="DatabaseException">
        /// Thrown if there's an issue accessing the licenses database (needed to check if the configured license is usable).
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown if the current <see cref="LicensingClient"/> instance was disposed.
        /// </exception>
        public Task<LicenseInfo> GetCurrentLicenseAsync()
        {
            return Task.Run(() => GetCurrentLicense());
        }

        /// <summary>
        /// Asynchronously deletes the license configuration from the registry if it exists there.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Thrown if the current <see cref="LicensingClient"/> instance was disposed.
        /// </exception>
        public Task DeactivateProductAsync()
        {
            return Task.Run(() => DeactivateProduct());
        }
        #endregion

        /// <summary>
        /// Releases all resources used by the current <see cref="LicensingClient"/> instance.
        /// </summary>
        public void Dispose()
        {
            isDisposed = true;

            context.Dispose();
            validator.Dispose();
            regKey.Close();
        }
    }
}