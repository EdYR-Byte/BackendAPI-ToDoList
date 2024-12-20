using System.ComponentModel.DataAnnotations;

namespace BackendAPI.DTO.TaskAssignment
{
    public class CreateTaskAssignmentDTO
    {
        [Required]
        public int TaskId { get; set; }
        
        [Required]
        public int CreadorProyectoId { get; set; }

        [Required]
        public int AsignadoId { get; set; }
    }
}
