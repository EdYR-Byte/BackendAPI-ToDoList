using System.ComponentModel.DataAnnotations;
using BackendAPI.Models;

namespace BackendAPI.DTO.Project
{
    public class CreateProjectDTO
    {
        public int? UserId { get; set; }
        public string? Name { get; set; }
        public string? Color { get; set; }
        public bool IsFavorite { get; set; } = false;

    }
}
