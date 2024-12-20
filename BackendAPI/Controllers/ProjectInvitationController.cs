using BackendAPI.Contexts;
using BackendAPI.DTO.ProjectInvitation;
using BackendAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectInvitationController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProjectInvitationController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("invitar-usuario")]
        public async Task<IActionResult> InvitarUsuarios([FromBody] CreateProjectInvitationDTO model)
        {
            // Busca proyecto
            var project = await _context.Projects.FindAsync(model.ProjectId);

            // Si no existe proyecto
            if (project == null)
            {
                return NotFound(new { message = "No se encontró este proyecto" });
            }

            // Solo el creador debe poder invitar
            if (project.UserId != model.UsuarioCreadorId)
            {
                return BadRequest(new { message = "Solo el creador de proyecto puede invitar usuarios." });
            }

            // El creador del proyecto no puede invitarse a si mismo
            if (project.UserId == model.UsuarioInvitadoId)
            {
                return Conflict(new { message = "El creador del proyecto no puede ser invitado" });
            }

            // Contar los invitados actuales del proyecto
            var cantidadInvitados = await _context.ProjectInvitations
                .CountAsync(pi => pi.ProjectId == model.ProjectId);

            // Validar que no supere el límite de 10 invitados
            if (cantidadInvitados >= 10)
            {
                return BadRequest(new { message = "El proyecto ya tiene el número máximo permitido de invitados (10)." });
            }

            // Busca invitado según Id
            var invitado = await _context.Users.FindAsync(model.UsuarioInvitadoId);

            // Si no existe el invitado
            if (invitado == null)
            {
                return BadRequest(new { message = $"No se encontró al invitado con id {model.UsuarioInvitadoId}." });
            }

            // Buscar invitación
            var invitacion = await _context.ProjectInvitations.FirstOrDefaultAsync(pi => pi.ProjectId == model.ProjectId && pi.UserId == model.UsuarioInvitadoId);

            // Si la invitación existe
            if (invitacion != null)
            {
                return BadRequest(new { message = $"Ya se invitó al usuario {model.UsuarioInvitadoId} al proyecto {model.ProjectId}" });
            }

            // Si no existe la invitación, continua normalmente
            var nuevaInvitacion = new ProjectInvitationModel
            {
                ProjectId = model.ProjectId,
                UserId = model.UsuarioInvitadoId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            try
            {
                _context.ProjectInvitations.Add(nuevaInvitacion);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Se invitó al usuario '{invitado.Name}' (ID: {invitado.UserId}) a proyecto '{project.Name}' (ID: {project.ProjectId})" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message.ToString() });
            }
        }

        [HttpGet("listar-posibles-invitados")]
        public async Task<IActionResult> ListarPosiblesInvitados([FromQuery] string identifier, [FromQuery] int projectId)
        {
            // Validar que el identifier tenga al menos 3 caracteres
            if (string.IsNullOrEmpty(identifier) || identifier.Length < 3)
            {
                return BadRequest(new { message = "Ingrese al menos 3 caracteres para hacer una búsqueda." });
            }

            // Validar si el projectId existe
            var proyecto = await _context.Projects.FindAsync(projectId);

            if (proyecto == null)
            {
                return NotFound(new { message = "El proyecto no fue encontrado." });
            }

            // Bool para determinar si es dni (Intenta convertir a entero)
            bool isDni = int.TryParse(identifier, out int dni);

            // Buscar al usuario dependiendo si el Identifier es DNI o no
            var userQuery = _context.Users.AsQueryable();

            // Busca coincidencias parciales con el término identifier
            userQuery = userQuery.Where(u =>
                (u.Email!.Contains(identifier) || u.UserName!.Contains(identifier) || u.Name!.Contains(identifier)) ||
                (isDni && u.Dni.ToString().Contains(identifier)));

            // Excluir usuarios que ya han sido invitados al proyecto
            var invitadosExistentes = _context.ProjectInvitations
                .Where(pi => pi.ProjectId == projectId)
                .Select(pi => pi.UserId);

            // Excluir al creador del proyecto
            var creadorProyectoId = proyecto.UserId;

            // Que en la consulta no se incluyan los usuarios invitados, ni el creador
            userQuery = userQuery.Where(u =>
                !invitadosExistentes.Contains(u.UserId) && u.UserId != creadorProyectoId);

            // Traer lista de invitados disponibles
            var listaPosiblesInvitados = await userQuery
                .Select(u => new { u.UserId, u.Name })
                .ToListAsync();

            // Si no se encuentra ningún usuario
            if (!listaPosiblesInvitados.Any())
            {
                return NotFound(new { message = "No se encontró ningún usuario." });
            }

            return Ok(listaPosiblesInvitados);
        }

        [HttpGet("listar-invitados")]
        public async Task<IActionResult> ListarInvitados([FromQuery] int projectId)
        {
            var projectInvitations = await _context.ProjectInvitations
                .Include(pi => pi.User)
                .Where(pi => pi.ProjectId == projectId)
                .ToListAsync();

            var invitados = projectInvitations
                .Select(pi => new
                {
                    pi.User!.UserId,
                    pi.User.Name
                })
                .ToList();

            return Ok(invitados);
        }
    }
}
