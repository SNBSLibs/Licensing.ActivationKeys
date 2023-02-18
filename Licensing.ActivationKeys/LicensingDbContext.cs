using Microsoft.EntityFrameworkCore;
using SNBS.Licensing.Utilities;

namespace SNBS.Licensing
{
    internal class LicensingDbContext : DbContext
    {
        private string connectionString;
        private bool useMySql;
        private Version? mySqlVersion;

        public LicensingDbContext(string connectionString, bool useMySql, Version? mySqlVersion = null)
        {
            if (useMySql && mySqlVersion == null)
            {
                throw new InvalidOperationException("You should specify the needed MySQL version when using MySQL.");
            }

            this.connectionString = connectionString;
            this.useMySql = useMySql;
            this.mySqlVersion = mySqlVersion;

            try { _ = Licenses?.Find("AAAAA-AAAAA-AAAAA-AAAAA-AAAAA"); }
            catch (ArgumentException ex) { ThrowHelper.DatabaseError(ex); }

            Database.EnsureCreated();
        }

        public DbSet<Entities.License> Licenses { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (useMySql)
            {
                optionsBuilder.UseMySql(connectionString,
                    new MySqlServerVersion(mySqlVersion));
            } else
            {
                optionsBuilder.UseSqlServer(connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<Entities.License>()
                .Property(l => l.Key)
                .HasColumnType("char(29)");

            modelBuilder.Entity<Entities.License>()
                .Property(l => l.Type)
                .HasColumnType("varchar(12)");

            modelBuilder.Entity<Entities.License>()
                .Property(l => l.Expiration)
                .HasColumnType("date");
        }
    }
}
