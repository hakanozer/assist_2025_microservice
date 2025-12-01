using System.ComponentModel.DataAnnotations;

namespace Product.Application.Dtos
{
    public class ProductAddDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [Range(1, 1000000)]
        public float Price { get; set; }

        public bool IsActive { get; set; } = true;
    }
}