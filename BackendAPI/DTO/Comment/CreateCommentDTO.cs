using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BackendAPI.DTO.Comment
{
    public class CreateCommentDTO
    {
        public int? TaskId { get; set; }
        public int? UserId { get; set; }
        public string? Content { get; set; }
    }
}
