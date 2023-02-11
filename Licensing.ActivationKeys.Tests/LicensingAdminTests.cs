using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SNBS.Licensing.Tests
{
    // BEFORE RUNNING THESE TESTS, RUN LicenseValidatorTests.
    // BECAUSE THIS CLASS USES IT TO CHECK THE DATABASE CONTENTS.
    [TestClass]
    public class LicensingAdminTests
    {
        private const string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Integrated Security=True;MultipleActiveResultSets=true;Initial Catalog=Licensing.ActivationKeys.Tests";

        private LicensingAdmin CreateAdmin()
        {
            return new(connectionString, false, null);
        }

        private LicenseValidator CreateValidator()
        {
            return new(connectionString, false, null);
        }

        [TestMethod]
        public void CanCreateLicense()
        {
            var info = CreateAdmin().CreateLicense(new(2024, 9, 6), LicenseType.Professional, 10);
#pragma warning disable CS8604
            var validatorInfo = CreateValidator().ValidateLicense(info.Key);
#pragma warning restore CS8604

            Assert.AreEqual(validatorInfo.Expiration, new(2024, 9, 6), "Incorrect expiration");
            Assert.AreEqual(validatorInfo.Type, LicenseType.Professional, "Incorrect type");
            // For some unknown reasons the compiler asks to specify type arguments explicitly
            Assert.AreEqual<short?>(validatorInfo.MaxDevices, 10, "Incorrect maximum number of devices");
        }

        [TestMethod]
        public void CanUpdateLicense()
        {
            CreateAdmin().UpdateLicense
                ("AAAAA-AAAAA-AAAAA-AAAAA-AAAAA",
                new(2023, 4, 5), LicenseType.General, null);
            var validatorInfo = CreateValidator().ValidateLicense("AAAAA-AAAAA-AAAAA-AAAAA-AAAAA");

            Assert.AreEqual(validatorInfo.Expiration, new(2023, 4, 5), "Incorrect expiration");
            Assert.AreEqual(validatorInfo.Type, LicenseType.General, "Incorrect type");
            // For some unknown reasons the compiler asks to specify type arguments explicitly
            Assert.AreEqual<short?>(validatorInfo.MaxDevices, 3, "Incorrect maximum number of devices");
        }

        [TestMethod]
        public void CanDeleteLicense()
        {
            CreateAdmin().DeleteLicense("AAAAA-AAAAA-AAAAA-AAAAA-AAAAA");
            var validatorInfo = CreateValidator().ValidateLicense("AAAAA-AAAAA-AAAAA-AAAAA-AAAAA");

            Assert.AreEqual(validatorInfo.Usability, LicenseUsability.NotFound, "The license still exists");
        }
    }
}
