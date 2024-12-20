using BackendAPI.Contexts;
using BackendAPI.DTO;
using BackendAPI.DTO.Task;
using BackendAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendAPI.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TaskController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("crearTarea")]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskDTO model)
        {
            if (model == null)
            {
                return BadRequest(new { message = "Error al crear la tarea. Los datos no pueden ser nulos." });
            }

            // Validando existencia del proyecto
            var projectExists = await _context.Projects.AnyAsync(p => p.ProjectId == model.ProjectId);
            if (!projectExists)
            {
                return BadRequest(new { message = "El proyecto no existe." });
            }

            // Validación para que el nombre de la tarea no sea nula o vacía
            if (string.IsNullOrEmpty(model.Name))
            {
                return BadRequest(new { message = "El nombre del proyecto no puede ser nulo o vacío." });
            }

            // Validando que no exista una tarea con el mismo nombre en el mismo proyecto
            var taskExists = await _context.Tasks
                .AnyAsync(t => t.Name == model.Name && t.ProjectId == model.ProjectId);
            if (taskExists)
            {
                return BadRequest(new { message = "Ya existe una tarea con el mismo nombre en este proyecto." });
            }

            // Validación para que la descripción no sea nula (aunque es opcional)
            if (model.Description != null && string.IsNullOrEmpty(model.Description))
            {
                return BadRequest(new { message = "La descripción no puede estar vacía si se proporciona." });
            }

            // Validando fecha no nula y en el futuro
            if (!model.DueDate.HasValue || model.DueDate.Value <= DateTime.UtcNow)
            {
                return BadRequest(new { message = "La fecha de vencimiento debe ser una fecha en el futuro." });
            }

            // Validando rango de prioridad (debe estar entre 1 y 4)
            if (model.Priority < 1 || model.Priority > 4)
            {
                return BadRequest(new { message = "La prioridad debe estar entre 1 y 4. La prioridad 1 es la más urgente." });
            }

            // Creación de la tarea
            var newTask = new TaskModel
            {
                ProjectId = model.ProjectId,
                Name = model.Name,
                Description = model.Description,
                DueDate = model.DueDate.Value,
                Priority = model.Priority,
                IsCompleted = false, // Al crearse, no están completadas
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Tasks.Add(newTask);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Tarea creada exitosamente." });
        }

        // Buscar tarea por ID
        [HttpGet("tarea/{id}")]
        public async Task<IActionResult> GetTaskById(int id)
        {
            var task = await _context.Tasks.FindAsync(id);

            if (task == null)
            {
                return NotFound(new { message = "Tarea no encontrada" });
            }

            return Ok(task);
        }

        // Verificar disponibilidad de nombre de tarea en el proyecto
        [HttpGet("verificar-disponibilidad-nombre-tarea")]
        public async Task<IActionResult> CheckTaskNameAvailability(string taskName, int projectId)
        {
            // Verificar si existe una tarea con el mismo nombre para el mismo proyecto
            var existingTask = await _context.Tasks
                .Where(t => t.Name == taskName && t.ProjectId == projectId)
                .Select(t => new
                {
                    t.TaskId,
                    t.Name,
                    t.Description,
                    t.DueDate,
                    t.Priority,
                    t.IsCompleted,
                    t.CreatedAt,
                    t.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (existingTask != null)
            {
                // Si ya existe una tarea con ese nombre en el proyecto, devuelve un conflicto
                return Conflict(new { message = "Ya existe una tarea con este nombre en el proyecto.", task = existingTask });
            }
            else
            {
                // Si no existe una tarea con ese nombre en el proyecto, devolver mensaje NotFound
                return NotFound(new { message = "El nombre de la tarea está disponible para su registro." });
            }
        }

        // Listar tareas por proyecto y ordenado por prioridad
        [HttpGet("listarTareasPorProyecto/{ProjectId}")]
        public async Task<IActionResult> ListarTareasPorProyecto(int ProjectId)
        {
            var tasks = await _context.Tasks
                        .Where(t => t.ProjectId == ProjectId)
                        .OrderBy(t => t.Priority) //ordenar por prioridad
                        .ToListAsync();

            // Verificar si hay tareas dentro del proyecto
            if (!tasks.Any())
            {
                return NotFound(new { message = "No se pudo encontrar tareas para este proyecto" });
            }

            return Ok(tasks);
        }

        [HttpPut("actualizar-tarea")]
        public async Task<IActionResult> ActualizarInfoTarea([FromBody] UpdateTaskDTO model)
        {
            var task = await _context.Tasks.FindAsync(model.TaskId);

            if (task == null)
            {
                return NotFound(new { Message = "No se encontró la tarea" });
            }

            if (model.Name != null)
            {
                task.Name = model.Name;
            }

            if (model.Description != null)
            {
                task.Description = model.Description;
            }

            if (model.DueDate != null)
            {
                task.DueDate = model.DueDate;
            }

            if (model.Priority.HasValue && (model.Priority < 1 || model.Priority > 4))
            {
                return BadRequest(new { Message = "La prioridad debe estar entre 1 y 4." });
            }

            if (model.Priority.HasValue)
            {
                task.Priority = model.Priority;
            }

            task.UpdatedAt = DateTime.UtcNow;

            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Tarea actualizada correctamente", task });
        }

        [HttpDelete("eliminar-tarea/{id}")]
        public async Task<IActionResult> EliminarTarea(int id)
        {
            var task = await _context.Tasks.FindAsync(id);

            if (task == null)
            {
                return NotFound(new { Message = "Tarea no encontrada" });
            }

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("actualizar-estado-tarea/{id}")]
        public async Task<IActionResult> CambiarEstadoTarea(int id)
        {
            var task = await _context.Tasks.FindAsync(id);

            if (task == null)
            {
                return NotFound(new { Message = "Tarea no encontrada" });
            }

            task.IsCompleted = !task.IsCompleted; // Al negarlo siempre, no hace falta más logica xD

            task.UpdatedAt = DateTime.UtcNow;

            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Se actualizó IsCompleted a {task.IsCompleted}" });
        }
    }

}