using SNBS.Licensing.Entities.Exceptions;

namespace SNBS.Licensing.Tests
{
    [TestClass]
    public class AdditionalTests
    {
        private LicensingClient CreateClient()
        {
            return new(
                "Data Source=(localdb)\\MSSQLLocalDB; Integrated Security=True;Connect Timeout=30; Encrypt=False;TrustServerCertificate=False; ApplicationIntent=ReadWrite; MultiSubnetFailover=False; Initial Catalog=Licensing.ActivationKeys.Tests",
                "Licensing.ActivationKeys.Tests", false);
        }

        // Run without admin permissions
        [TestMethod]
        [ExpectedException(typeof(RegistryAccessException))]
        public void CanThrowOnUnauthorizedAccess() => CreateClient();
    }
}
