using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Product.Domain.Entities
{
    public class ProductEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long PId { get; private set; }
        [MaxLength(100)]
        public string Name { get; private set; } = string.Empty;
        [MaxLength(500)]
        public string? Description { get; private set; }
        [Range(1, 1000000)]
        public float Price { get; private set; }
        public bool IsActive { get; private set; } = true;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; private set; } = DateTime.UtcNow;
    
        
    }
}