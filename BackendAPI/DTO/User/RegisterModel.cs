using System.ComponentModel.DataAnnotations;

namespace BackendAPI.DTO.User
{
    public class RegisterModel
    {
        [Required]
        public string? Name { get; set; }
        [Required]
        public string? UserName { get; set; }
        [Required]
        public int Dni { get; set; }

        [Required]
        public string? Email { get; set; }

        [Required]
        public string? Password { get; set; }
    }
}
