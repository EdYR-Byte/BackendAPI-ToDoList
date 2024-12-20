using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BackendAPI.Models
{
    public class RoleModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? RoleId { get; set; }

        [Required]
        public string? Name { get; set; }

    }
}
