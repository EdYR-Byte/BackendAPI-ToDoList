using BackendAPI.Contexts;
using BackendAPI.DTO.Comment;
using BackendAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace BackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CommentController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("agregar-comentario")]
        public async Task<IActionResult> AgregarComentario([FromBody] CreateCommentDTO model)
        {
            var task = await _context.Tasks.Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.TaskId == model.TaskId);

            if (task == null)
            {
                return NotFound(new { message = "No se encontró tarea" });
            }

            var project = await _context.Projects.FindAsync(task.ProjectId);

            if (project == null)
            {
                return NotFound(new { message = "No se encontró proyecto" });
            }

            // Solo deben comentar el creador del proyecto y los invitados
            // Si el UserId ingresado es diferente al del creador del proyecto
            if (project.UserId != model.UserId)
            {

                // Determina si es invitado
                var isInvited = await _context.ProjectInvitations
                    .AnyAsync(pi => pi.ProjectId == project.ProjectId && pi.UserId == model.UserId);

                // Si no lo es, se envía mensaje de error
                if (!isInvited)
                {
                    return StatusCode(403, new { message = "Usuario no autorizado para agregar comentarios." });
                }
            }

            // Comentario no debe ser vacío, debe tener entre 10 - 100 caracteres
            if (string.IsNullOrWhiteSpace(model.Content) || model.Content.Length < 10 || model.Content.Length > 100)
            {
                return BadRequest(new { message = "El contenido debe tener entre 10 y 100 caracteres." });
            }

            // Si el UserId es del creador, continúa con normalidad al registro del comentario
            var nuevoComentario = new CommentModel
            {
                TaskId = model.TaskId,
                UserId = model.UserId,
                Content = model.Content,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            try
            {
                _context.Comments.Add(nuevoComentario);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Comentario agregado correctamente", nuevoComentario });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message.ToString() });
            }
        }

        [HttpGet("listar-comentarios/{taskId}")]
        public async Task<IActionResult> ListarComentarios(int taskId)
        {
            var task = await _context.Tasks.FindAsync(taskId);

            if (task == null)
            {
                return NotFound(new { message = "No se encontró tarea" });
            }

            var comments = await _context.Comments.Where(c => c.TaskId == taskId)
                .Include(c => c.User)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.CommentId,
                    c.User!.Name,
                    c.Content,
                    createdAt = string.Format("{0:dd MMM yyyy 'a las' HH:mm}", c.CreatedAt),
                    updatedAt = string.Format("{0:dd MMM yyyy 'a las' HH:mm}", c.UpdatedAt)
                })
                .ToListAsync();


            // Si no hay ningun comentario
            if (!comments.Any())
            {
                return Ok(new { message = "No hay comentarios para esta tarea." });
            }

            return Ok(comments);
        }
    }
}
