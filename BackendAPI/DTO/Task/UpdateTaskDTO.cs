namespace BackendAPI.DTO.Task
{
    public class UpdateTaskDTO
    {
        public int? TaskId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public byte? Priority { get; set; }
    }
}

