using BackendAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendAPI.Contexts
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        // Por nombres de tablas
        public DbSet<RoleModel> Roles { get; set; }
        public DbSet<UserModel> Users { get; set; }
        public DbSet<ProjectModel> Projects { get; set; }
        public DbSet<TaskModel> Tasks { get; set; }
        public DbSet<CommentModel> Comments { get; set; }
        public DbSet<ProjectInvitationModel> ProjectInvitations { get; set; }
        public DbSet<TaskAssignmentModel> TaskAssignments { get; set; }

        // Configuración extra para llaves compuestas
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurando Clave Compuesta Project - User
            modelBuilder.Entity<ProjectInvitationModel>()
                .HasKey(pi => new { pi.ProjectId, pi.UserId });

            // Configurando Clave Compuesta Task - User
            modelBuilder.Entity<TaskAssignmentModel>()
                .HasKey(ta => new { ta.TaskId, ta.UserId });

        }
    }
}
