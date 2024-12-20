using BackendAPI.Contexts;
using BackendAPI.DTO.Project;
using BackendAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProjectController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("crearProyecto")]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectDTO model)
        {
            if (model == null)
            {
                return BadRequest(new { message = "Error al crear el Proyecto. Los datos no pueden ser nulos." });
            }

            // Validación de la existencia del usuario
            var userExists = await _context.Users.AnyAsync(u => u.UserId == model.UserId);
            if (!userExists)
            {
                return BadRequest(new { message = "El usuario no existe" });
            }

            // Validación para que el proyecto no tenga el mismo nombre para el mismo usuario
            var projectExists = await _context.Projects.AnyAsync(p => p.Name == model.Name && p.UserId == model.UserId);
            if (projectExists)
            {
                return BadRequest(new { message = "Ya existe un proyecto con el mismo nombre para este usuario" });
            }

            // Validación de que el nombre del proyecto no sea nulo o vacío
            if (string.IsNullOrEmpty(model.Name))
            {
                return BadRequest(new { message = "El nombre del proyecto no puede ser nulo o vacío." });
            }

            // Validación del formato HEX del color
            if (!string.IsNullOrEmpty(model.Color))
            {
                var hexColorRegex = @"^#[0-9A-Fa-f]{6}$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(model.Color, hexColorRegex))
                {
                    return BadRequest(new { message = "El código de color debe ser un valor HEX válido (ej. #FFFFFF)." });
                }
            }

            // Creación del proyecto
            var newProject = new ProjectModel
            {
                Name = model.Name,
                Color = model.Color,
                IsFavorite = model.IsFavorite, // Se asigna el valor del DTO
                UserId = model.UserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(newProject);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Proyecto creado exitosamente", newProject });
        }

        // Buscar proyecto por ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var project = await _context.Projects.FindAsync(id);

            if (project == null)
            {
                return NotFound(new { message = "Proyecto no encontrado" });
            }

            return Ok(project);
        }

        [HttpGet("verificar-disponibilidad-nombre")]
        public async Task<IActionResult> BuscarPorNombreYUsuario(string projectName, int userId)
        {
            // Verificar si el proyecto con el mismo nombre ya existe para el mismo usuario
            var existingProject = await _context.Projects
                .Where(p => p.Name == projectName && p.UserId == userId)
                .Select(p => new
                {
                    p.UserId,
                    p.Name,
                    p.Color,
                    p.IsFavorite,
                    p.CreatedAt,
                    p.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (existingProject != null)
            {
                // Si ya existe un proyecto con ese nombre para el usuario, muestra los datos del proyecto encontrado
                return Conflict(new { message = "Ya existe un proyecto con este nombre para este usuario.", project = existingProject });
            }
            else
            {
                // Si no existe un proyecto con ese nombre para el usuario, devolver mensaje NotFound
                return NotFound(new { message = "El nombre del proyecto está disponible para su registro." });
            }
        }

        // Listar proyectos por usuario(ID)
        [HttpGet("listarProyectosPorUsuario/{UserId}")]
        public async Task<IActionResult> ListarProyectosPorUsuario(int UserId)
        {
            // Obtener proyectos creados por UserId
            var proyectosCreados = _context.Projects.Where(p => p.UserId == UserId)
                .Select(p => new
                {
                    p.ProjectId,
                    p.UserId,
                    p.Name,
                    p.Color,
                    p.IsFavorite,
                    p.CreatedAt,
                    p.UpdatedAt,
                    Tipo = "Creador"
                });

            // Obtener proyectos donde el UserId es invitado
            var proyectosInvitado = _context.ProjectInvitations.Where(pi => pi.UserId == UserId)
                .Select(pi => new
                {
                    pi.Project!.ProjectId,
                    pi.Project.UserId,
                    Name = pi.Project.Name + " (Invitado)",
                    pi.Project.Color,
                    pi.Project.IsFavorite,
                    pi.Project.CreatedAt,
                    pi.Project.UpdatedAt,
                    Tipo = "Invitado"
                });

            // Combinar ambos tipos de proyecto
            var projects = await proyectosCreados
                .Union(proyectosInvitado)
                .ToListAsync();

            // Si no hay ninguno, mostrar mensaje
            if (!projects.Any())
            {
                return NotFound(new { message = "No se pudo encontrar proyectos para este usuario" });
            }

            return Ok(projects);
        }
    }
}
