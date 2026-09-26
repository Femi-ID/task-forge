using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace api.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options)
        : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
    {
        public DbSet<Workspace> Workspaces => Set<Workspace>();
        public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<TaskItem> Tasks => Set<TaskItem>();    // table name = "Tasks"
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<Label> Labels => Set<Label>();
        public DbSet<TaskLabel> TaskLabels => Set<TaskLabel>();

        // FIXED values: HasData must never use Guid.NewGuid() at model-build time, otherwise
        // every `migrations add` sees "different" seed rows and generates delete+insert churn.
        private static readonly Guid AdminRoleId = Guid.Parse("24af79c7-1d10-43f4-bbe7-5030b5b2f83a");
        private static readonly Guid UserRoleId = Guid.Parse("bd461df2-b05b-4a49-bf4d-2e1ecb5a77a9");

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // required — configures all the Identity tables

            // Global Roles (Identity)
            builder.Entity<IdentityRole<Guid>>().HasData(
            new IdentityRole<Guid>
            {
                Id = AdminRoleId,
                Name = "Admin",
                NormalizedName = "ADMIN",
                ConcurrencyStamp = "4d86a747-4094-4864-8d07-3781b6f5bad6"
            },
            new IdentityRole<Guid>
            {
                Id = UserRoleId,
                Name = "User",
                NormalizedName = "USER",
                ConcurrencyStamp = "61061ffc-834c-4dba-8f82-3ca82ec624f0"
            });

            // Composite keys for the two join entities (EF cannot infer these)
            builder.Entity<WorkspaceMember>().HasKey(m => new { m.AppUserId, m.WorkspaceId });
            builder.Entity<TaskLabel>().HasKey(tl => new { tl.TaskItemId, tl.LabelId });

            //  Workspace
            builder.Entity<Workspace>(e =>
            {
                e.Property(w => w.Name).HasMaxLength(100);
                e.Property(w => w.Description).HasMaxLength(1000);

                // RESTRICT: a user who owns workspaces can't be hard-deleted (they must transfer ownership first).
                // Also avoids SQL Server "multiple cascade paths" errors.
                e.HasOne(w => w.Owner).WithMany()
                 .HasForeignKey(w => w.OwnerId)
                 .OnDelete(DeleteBehavior.Restrict);

                // ensure the Name field is unique for each user
                e.HasIndex(w => new { w.OwnerId, w.Name }).IsUnique();
            });

            // WorkspaceMember
            builder.Entity<WorkspaceMember>(e =>
            {
                e.HasOne(m => m.AppUser).WithMany(u => u.WorkspaceMembers)
                .HasForeignKey(m => m.AppUserId).OnDelete(DeleteBehavior.Cascade);

                e.HasOne(m => m.Workspace).WithMany(w => w.WorkspaceMembers)
                .HasForeignKey(m => m.WorkspaceId).OnDelete(DeleteBehavior.Cascade);

                e.Property(m => m.Role).HasConversion<string>().HasMaxLength(20);
            });

            // Project
            builder.Entity<Project>(e =>
            {
                e.Property(p => p.Name).HasMaxLength(100);
                e.Property(p => p.Description).HasMaxLength(2000);
                e.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

                e.HasOne(p => p.Workspace).WithMany(w => w.Projects)
                    .HasForeignKey(p => p.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
            });

            // TaskItem
            builder.Entity<TaskItem>(e =>
            {
                e.Property(t => t.Title).HasMaxLength(200);
                e.Property(t => t.Description).HasMaxLength(4000);
                e.Property(t => t.Priority).HasConversion<string>().HasMaxLength(20);
                e.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);

                e.HasOne(t => t.Project).WithMany(p => p.Tasks)
                 .HasForeignKey(t => t.ProjectId).OnDelete(DeleteBehavior.Cascade);

                // Optional assignee: if the user is deleted the task becomes unassigned
                e.HasOne(t => t.Assignee).WithMany(u => u.AssignedTasks)
                 .HasForeignKey(t => t.AssigneeId).OnDelete(DeleteBehavior.SetNull);

                // Speeds up "list tasks in project filtered by status"
                e.HasIndex(t => new { t.ProjectId, t.Status });
            });

            // Comment
            builder.Entity<Comment>(e =>
            {
                e.Property(c => c.Content).HasMaxLength(4000);

                e.HasOne(c => c.TaskItem).WithMany(t => t.Comments)
                 .HasForeignKey(c => c.TaskItemId).OnDelete(DeleteBehavior.Cascade);

                e.HasOne(c => c.Author).WithMany(u => u.Comments)
                 .HasForeignKey(c => c.AuthorId).OnDelete(DeleteBehavior.Restrict);

                // Self-reference. MUST NOT cascade (SQL Server forbids cascade cycles).
                // Deleting a parent that still has replies is blocked - soft-delete or delete replies first.
                e.HasOne(c => c.ParentComment).WithMany(c => c.Replies)
                 .HasForeignKey(c => c.ParentCommentId).OnDelete(DeleteBehavior.Restrict);
            });

            // Label
            builder.Entity<Label>(e =>
            {
                e.Property(l => l.Name).HasMaxLength(50);
                e.Property(l => l.Colour).HasMaxLength(9);

                e.HasOne(l => l.Workspace).WithMany(w => w.Labels)
                    .HasForeignKey(l => l.WorkspaceId).OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(l => new { l.WorkspaceId, l.Name }).IsUnique(); // one "Bug" label per workspace
            });

            // TaskLabel
            builder.Entity<TaskLabel>(e =>
            {
                e.HasOne(tl => tl.TaskItem).WithMany(t => t.TaskLabels)
                 .HasForeignKey(tl => tl.TaskItemId).OnDelete(DeleteBehavior.Cascade);

                // NoAction (not Cascade): Workspace -> Label -> TaskLabel and
                // Workspace -> Project -> TaskItem -> TaskLabel would be two cascade paths (SQL Server error 1785).
                // To delete a single label, remove its TaskLabel rows first.
                e.HasOne(tl => tl.Label).WithMany(l => l.TaskLabels)
                 .HasForeignKey(tl => tl.LabelId).OnDelete(DeleteBehavior.NoAction);
            });
        }

        // Automatic UpdatedAt
        // Overriding the two bool overloads covers SaveChanges() and SaveChangesAsync() too,
        // because the parameterless versions call these internally.
        // Note: ExecuteUpdate/ExecuteDelete bypass the change tracker, so they won't stamp UpdatedAt.
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            StampUpdatedAt();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            StampUpdatedAt();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void StampUpdatedAt()
        {
            var now = DateTime.UtcNow;
            foreach (var entry in ChangeTracker.Entries<IUpdatable>().Where(e => e.State == EntityState.Modified))
                entry.Entity.UpdatedAt = now;
        }
    }
}