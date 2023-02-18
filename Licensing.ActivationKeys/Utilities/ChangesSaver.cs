using Microsoft.EntityFrameworkCore;
using SNBS.Licensing.Entities;

namespace SNBS.Licensing.Utilities
{
    internal static class ChangesSaver
    {
        internal static void SaveChanges(LicensingDbContext context)
        {
            try { context.SaveChanges(); }
            catch (Exception ex) { ThrowHelper.DatabaseError(ex); }
        }
    }
}