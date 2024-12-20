using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BackendAPI.Models
{
    public class TaskAssignmentModel
    {
        [Key, Column(Order = 0)]
        public int? TaskId { get; set; }

        [Key, Column(Order = 1)]
        public int? UserId { get; set; }

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
