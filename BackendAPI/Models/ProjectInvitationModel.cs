using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BackendAPI.Models
{
    public class ProjectInvitationModel
    {
        [Key, Column(Order = 0)]
        public int? ProjectId { get; set; }

        [Key, Column(Order = 1)]
        public int? UserId { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Relaciones con Project y User

        [JsonIgnore]
        [ForeignKey("ProjectId")]
        public ProjectModel? Project { get; set; }

        [JsonIgnore]
        [ForeignKey("UserId")]
        public UserModel? User { get; set; }
    }
}
