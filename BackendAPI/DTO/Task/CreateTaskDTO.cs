using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackendAPI.DTO.Task;

public class CreateTaskDTO
{
    public int? ProjectId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public byte Priority { get; set; }

}