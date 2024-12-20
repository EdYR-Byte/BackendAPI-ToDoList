using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace BackendAPI.Models
{
    public class UserModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? UserId { get; set; }

        [Required]
        public string? Name { get; set; }

        [Required]
        public string? UserName { get; set; }
        
        [Required]
        public int Dni { get; set; }

        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        public string? Password { get; set; }

        [Required]
        public int RoleId { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Relacion con Role

        [JsonIgnore]
        [ForeignKey("RoleId")]
        public RoleModel? Role { get; set; }
    }
}
