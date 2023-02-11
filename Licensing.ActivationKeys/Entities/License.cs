using System.ComponentModel.DataAnnotations;

namespace SNBS.Licensing.Entities
{
    internal class License
    {
        [Key]
        [Required]
        [RegularExpression(@"(?:[A-Z]|[0-9])+-(?:[A-Z]|[0-9])+-(?:[A-Z]|[0-9])+-(?:[A-Z]|[0-9])+-(?:[A-Z]|[0-9])+")]
        [StringLength(29, MinimumLength = 29)]
        public string? Key { get; set; }

        [Required]
        public DateTime Expiration { get; set; }

        [Required]
        [Range((int)LicenseType.Trial, (int)LicenseType.Professional)]
        public LicenseType Type { get; set; } = LicenseType.Trial;

        [Required]
        [Range(1, short.MaxValue)]
        public short MaxDevices { get; set; } = 3;

        [Required]
        [Range(0, short.MaxValue)]
        public short UsingDevices { get; set; } = 0;
    }
}
