using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SNBS.Licensing.Tests
{
    [TestClass]
    public class LicenseValidatorTests
    {
        private LicenseValidator CreateValidator()
        {
            return new(@"Data Source=(localdb)\MSSQLLocalDB;Integrated Security=True;MultipleActiveResultSets=true;Initial Catalog=Licensing.ActivationKeys.Tests", false, null);
        }

        [TestMethod]
        public void CanValidateValidLicense()
        {
            var result = CreateValidator().ValidateLicense("AAAAA-AAAAA-AAAAA-AAAAA-AAAAA");

            Assert.AreEqual(result.Usability, LicenseUsability.Usable, "Incorrect usability");
            Assert.AreEqual(result.Type, LicenseType.Professional, "Incorrect type");
            Assert.AreEqual(result.Expiration, DateTime.Today.AddYears(1), "Incorrect expiration date");
        }

        [TestMethod]
        public void CannotValidateInvalidLicense()
        {
            var result = CreateValidator().ValidateLicense("YYYYY-YYYYY-YYYYY-YYYYY-YYYYY");

            Assert.AreEqual(result.Usability, LicenseUsability.NotFound, "Incorrect usability");
            Assert.IsNull(result.Type, "Type is not null");
            Assert.IsNull(result.Expiration, "Expiration date is not null");
        }
    }
}
