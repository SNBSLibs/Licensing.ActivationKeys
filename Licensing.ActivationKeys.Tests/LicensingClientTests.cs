using System.Reflection.Metadata;
using Microsoft.Win32;
using SNBS.Licensing.Entities.Exceptions;

namespace SNBS.Licensing.Tests
{
    [TestClass]
    public class LicensingClientTests
    {
        private const string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Integrated Security=True;MultipleActiveResultSets=true;Initial Catalog=Licensing.ActivationKeys.Tests";
        private RegistryKey regKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\SNBS\Licensing.ActivationKeys\Licensing.ActivationKeys.Tests");

        private LicensingClient CreateClient()
        {
            return new(connectionString, "Licensing.ActivationKeys.Tests", false);
        }

        private LicensingClient CreateInvalidClient()
        {
            return new("ABC", "ABC", false);
        }

        [TestMethod]
        public void CanActivateValidLicense()
        {
            var result = CreateClient().ActivateProduct("AAAAA-AAAAA-AAAAA-AAAAA-AAAAA");

            string? regKey = this.regKey.GetValue("CurrentKey") as string;
            string? regExpiration = this.regKey.GetValue("Expiration") as string;

            Assert.AreEqual(result.Usability, LicenseUsability.Usable, "Incorrect usability");
            Assert.IsFalse(string.IsNullOrEmpty(regKey), "The key in registry is empty");
            Assert.IsFalse(string.IsNullOrEmpty(regExpiration), "The expiration in registry is empty");
            Assert.AreEqual(regKey, "AAAAA-AAAAA-AAAAA-AAAAA-AAAAA", "Invalid key in registry");
            Assert.AreEqual(regExpiration, DateTime.Today.AddYears(1).ToShortDateString(), "Invalid expiration in registry");
        }

        [TestMethod]
        public void CannotActivateInvalidLicense()
        {
            this.regKey.SetValue("CurrentKey", string.Empty);
            this.regKey.SetValue("Expiration", string.Empty);

            var result = CreateClient().ActivateProduct("YYYYY-YYYYY-YYYYY-YYYYY-YYYYY");

            string? regKey = this.regKey.GetValue("CurrentKey") as string;
            string? regExpiration = this.regKey.GetValue("Expiration") as string;

            Assert.AreEqual(result.Usability, LicenseUsability.NotFound, "Incorrect usability");
            Assert.IsTrue(string.IsNullOrEmpty(regKey), "The key in registry is not empty");
            Assert.IsTrue(string.IsNullOrEmpty(regExpiration), "The expiration in registry is not empty");
        }

        [TestMethod]
        public void CanDeactivateLicense()
        {
            CreateClient().DeactivateProduct();

            string? regValue = regKey.GetValue("CurrentKey") as string;

            Assert.IsTrue(string.IsNullOrEmpty(regValue), "The value in registry is not empty");
        }

        [TestMethod]
        public void CanTakeValidLicense()
        {
            regKey.SetValue("CurrentKey", "AAAAA-AAAAA-AAAAA-AAAAA-AAAAA");

            var license = CreateClient().GetCurrentLicense();

            Assert.AreEqual(license.Key, "AAAAA-AAAAA-AAAAA-AAAAA-AAAAA", "Incorrect key");
            Assert.AreEqual(license.Usability, LicenseUsability.Usable, "Incorrect usability");
            Assert.AreEqual(license.Expiration, DateTime.Today.AddYears(1), "Incorrect expiration date");
            Assert.AreEqual(license.Type, LicenseType.Professional, "Incorrect type");
        }

        [TestMethod]
        public void CannotTakeInvalidLicense()
        {
            regKey.SetValue("CurrentKey", "YYYYY-YYYYY-YYYYY-YYYYY-YYYYY");

            var license = CreateClient().GetCurrentLicense();

            Assert.AreEqual(license.Usability, LicenseUsability.NotFound, "Incorrect usability");
            Assert.IsNull(license.Key, "Key is not null");
            Assert.IsNull(license.Expiration, "Expiration date is not null");
            Assert.IsNull(license.Type, "Type is not null");
        }

        [TestMethod]
        public void CannotTakeNotConfiguredLicense()
        {
            regKey.SetValue("CurrentKey", string.Empty);

            var license = CreateClient().GetCurrentLicense();

            Assert.AreEqual(license.Usability, LicenseUsability.NoConfiguredLicense, "Incorrect usability");
            Assert.IsNull(license.Key, "Key is not null");
            Assert.IsNull(license.Expiration, "Expiration date is not null");
            Assert.IsNull(license.Type, "Type is not null");
        }


        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void CannotWorkWhenDisposed()
        {
            var client = CreateClient();

            client.Dispose();
            client.ActivateProduct("AAAAA-AAAAA-AAAAA-AAAAA-AAAAA");
        }

        [TestMethod]
        [ExpectedException(typeof(DatabaseException))]
        public void CanThrowOnInvalidConnectionString()
        {
            CreateInvalidClient().ActivateProduct("AAAAA-AAAAA-AAAAA-AAAAA-AAAAA");
        }

        [TestMethod]
        public void CanStartWithValidLicense()
        {
            regKey.SetValue("CurrentKey", "AAAAA-AAAAA-AAAAA-AAAAA-AAAAA");

            LicensingClient.Start(
                connectionString,
                "Licensing.ActivationKeys.Tests", false, null,
                client =>
                {
                    Assert.AreEqual(client.GetCurrentLicense().Key, "AAAAA-AAAAA-AAAAA-AAAAA-AAAAA", "Incorrect key");
                },
                (client, usability) =>
                {
                    Assert.Fail("Ran onNotLicensed, even though there's a license");
                });
        }

        [TestMethod]
        public void CanStartWithInvalidLicense()
        {
            regKey.SetValue("CurrentKey", "YYYYY-YYYYY-YYYYY-YYYYY-YYYYY");

            LicensingClient.Start(
                connectionString,
                "Licensing.ActivationKeys.Tests", false, null,
                client =>
                {
                    Assert.Fail("Ran onLicensed, even though the license is invalid");
                },
                (client, usability) =>
                {
                    Assert.AreEqual(usability, LicenseUsability.NotFound, "Incorrect usability");
                });
        }

        [TestMethod]
        public void CanChangeProductName()
        {
            var client = CreateClient();

            regKey.Close(); // Will be deleted
            client.ProductName = "Test";

            Assert.IsNull(Registry.LocalMachine.OpenSubKey
                (@"SOFTWARE\SNBS\Licensing.ActivationKeys\Licensing.ActivationKeys.Tests"));
            Assert.IsNotNull(Registry.LocalMachine.OpenSubKey
                (@"SOFTWARE\SNBS\Licensing.ActivationKeys\Test"));

            // Other tests expect this product name
            client.ProductName = "Licensing.ActivationKeys.Tests";
            // Other tests expect it to be open
            regKey = Registry.LocalMachine.CreateSubKey
                (@"SOFTWARE\SNBS\Licensing.ActivationKeys\Licensing.ActivationKeys.Tests");
        }

        [TestMethod]
        public void CanUseMySql()
        {
            var client = new LicensingClient("Server=sql7.freesqldatabase.com; Uid=sql7594998; Pwd=l2TZZAQ5hB; Database=sql7594998",
                "Licensing.ActivationKeys.Tests", true, new(5, 0, 12));

            var license = client.ActivateProduct("AAAAA-AAAAA-AAAAA-AAAAA-AAAAA");

            Assert.AreEqual(license.Usability, LicenseUsability.NotFound, "Incorrect usability");
        }

        [TestMethod]
        public void CanPreserveExpirationDate()
        {
            regKey.SetValue("CurrentKey", "YYYYY-YYYYY-YYYYY-YYYYY-YYYYY");
            regKey.SetValue("Expiration", new DateTime(0).ToShortDateString());

            var result = CreateClient().GetCurrentLicense();

            Assert.AreEqual(result.Usability, LicenseUsability.Expired, "Incorrect usability");
        }
        
        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void CanValidateKey()
        {
            CreateClient().ActivateProduct("AAAAA-AAA%A-AAAAA-AAAAA-AAAAA");
        }
    }
}