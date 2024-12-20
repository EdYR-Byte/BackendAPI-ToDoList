using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BackendAPI.Models
{
    public class ProjectModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? ProjectId { get; set; }
        public int? UserId { get; set; }
        public string? Name { get; set; }
        public string? Color { get; set; }
        public bool? IsFavorite { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Relacion con User

        [JsonIgnore]
        [ForeignKey("UserId")]
        public UserModel? User { get; set; }
    
    }
}

