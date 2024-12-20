namespace BackendAPI.DTO.Task
{
    public class UpdateUserDTO
    {
        public int? UserId { get; set; }
        public string? Name { get; set; }
        public string? Password { get; set; }
    }
}
