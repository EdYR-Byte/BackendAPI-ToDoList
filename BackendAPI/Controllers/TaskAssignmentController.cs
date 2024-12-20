using BackendAPI.Contexts;
using BackendAPI.DTO.TaskAssignment;
using BackendAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using System.Threading.Tasks;

namespace BackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskAssignmentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TaskAssignmentController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("asignar-tarea")]
        public async Task<IActionResult> AsignarTareas([FromBody] CreateTaskAssignmentDTO model)
        {
            var task = await _context.Tasks
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.TaskId == model.TaskId);

            if (task == null)
            {
                return NotFound(new { message = "Tarea no encontrada" });
            }

            var project = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectId == task.ProjectId);

            if (project == null)
            {
                return NotFound(new { message = "No se encontró proyecto asociado a esta tarea" });
            }

            if (project.UserId != model.CreadorProyectoId)
            {
                return BadRequest(new { message = "Solo el creador del proyecto puede asignar esta tarea" });
            }

            if (project.UserId == model.AsignadoId)
            {
                return BadRequest(new { message = "El creador del proyecto no puede asignarse tareas a si mismo" });
            }

            // Busca si el posible asignado ya está invitado al proyecto
            var esInvitado = await _context.ProjectInvitations
                .AnyAsync(pi => pi.ProjectId == project.ProjectId && pi.UserId == model.AsignadoId);

            // Si no lo está, no se puede asignar
            if (esInvitado == false)
            {
                return BadRequest(new { message = "Solo se puede asignar tareas a usuarios invitados al proyecto" });
            }

            // Si es que ya se asignó esta tarea a un usuario
            var taskAssignment = await _context.TaskAssignments.FirstOrDefaultAsync(ta => ta.TaskId == model.TaskId && ta.UserId == model.AsignadoId);

            if (taskAssignment != null)
            {
                return Conflict(new { message = $"Ya se asignó la tarea ({model.TaskId}) al usuario ({model.AsignadoId}) " });
            }

            // Crea objeto para inserción
            var nuevaAsignacion = new TaskAssignmentModel
            {
                TaskId = model.TaskId,
                UserId = model.AsignadoId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            try
            {
                _context.TaskAssignments.Add(nuevaAsignacion);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Tarea asignada con éxito" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message.ToString() });
            }
        }

        [HttpGet("listar-tareas-asignadas/{userId}")]
        public async Task<IActionResult> ListarTareasAsignadas(int userId)
        {
            // Buscar las asignaciones de tareas de acuerdo a UserId
            var taskAssignments = await _context.TaskAssignments
                .Where(ta => ta.UserId == userId)
                .Include(ta => ta.Task)
                .ToListAsync();

            // Si el usuario no tiene asignaciones
            if (taskAssignments == null || !taskAssignments.Any())
            {
                return Ok(new { message = "No hay tareas asignadas a este usuario." });
            }

            // Lista objetos Task según asignaciones
            var tareas = taskAssignments.Select(ta => ta.Task).ToList();

            return Ok(tareas);
        }
    }
}
