using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BackendAPI.Models
{
    public class CommentModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? CommentId { get; set; }

        public int? TaskId { get; set; }

        public int? UserId { get; set; }

        public string? Content { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Relaciones con Task y User

        [JsonIgnore]
        [ForeignKey("TaskId")]
        public TaskModel? Task { get; set; }

        [JsonIgnore]
        [ForeignKey("UserId")]
        public UserModel? User { get; set; }
    }
}
