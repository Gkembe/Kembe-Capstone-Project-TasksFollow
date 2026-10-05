using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TasksFollow.Models;
using TasksFollow.Data;

namespace TasksFollow.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectTask> ProjectTasks { get; set; }
        public DbSet<ProjectMember> ProjectMembers { get; set; }
        public DbSet<TaskAssignment> TaskAssignments { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<GroupChat> GroupChats { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // configuration goes here

            // ApplicationUser 1 ---- 0..1 Employee
            builder.Entity<Employee>()
                .HasOne(e => e.ApplicationUser)
                .WithOne(u => u.Employee)
                .HasForeignKey<Employee>(e => e.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Project>()
                .HasOne(p => p.CreatedByUser)
                .WithMany()
                .HasForeignKey(p => p.CreatedByUserId)
    .OnDelete(DeleteBehavior.Restrict);
            // ProjectMember -> Project
            builder.Entity<ProjectMember>()
                .HasOne(pm => pm.Project)
                .WithMany(p => p.ProjectMembers)
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // ProjectMember -> Employee
            builder.Entity<ProjectMember>()
                .HasOne(pm => pm.Employee)
                .WithMany(e => e.ProjectMemberships)
                .HasForeignKey(pm => pm.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent same employee from joining project twice
            builder.Entity<ProjectMember>()
                .HasIndex(pm => new
                {
                    pm.ProjectId,
                    pm.EmployeeId
                })
                .IsUnique();

            // ProjectTask -> Project
            builder.Entity<ProjectTask>()
                .HasOne(pt => pt.Project)
                .WithMany(p => p.ProjectTask)
                .HasForeignKey(pt => pt.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<ProjectTask>()
                    .HasOne(pt => pt.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(pt => pt.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

            // GroupMember relationships
            builder.Entity<GroupMember>()
                .HasOne(gm => gm.GroupChat)
                .WithMany(gc => gc.GroupMembers)
                .HasForeignKey(gm => gm.GroupChatId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GroupMember>()
                .HasOne(gm => gm.Employee)
                .WithMany(e => e.GroupMemberships)
                .HasForeignKey(gm => gm.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent duplicate group members
            builder.Entity<GroupMember>()
                .HasIndex(gm => new
                {
                    gm.GroupChatId,
                    gm.EmployeeId
                })
                .IsUnique();
            builder.Entity<GroupChat>()
                    .HasOne(g => g.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(g => g.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

            // TaskAssignment -> ProjectTask
            builder.Entity<TaskAssignment>()
                .HasOne(ta => ta.ProjectTask)
                .WithMany(t => t.TaskAssignments)
                .HasForeignKey(ta => ta.ProjectTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            // TaskAssignment -> Employee
            builder.Entity<TaskAssignment>()
                .HasOne(ta => ta.Employee)
                .WithMany(e => e.TaskAssignments)
                .HasForeignKey(ta => ta.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent same task from being assigned twice to same employee
            builder.Entity<TaskAssignment>()
                .HasIndex(ta => new
                {
                    ta.ProjectTaskId,
                    ta.EmployeeId
                })
                .IsUnique();

            // Comment -> ProjectTask
            builder.Entity<Comment>()
                .HasOne(c => c.ProjectTask)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.ProjectTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            // Comment replies
            builder.Entity<Comment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Message -> GroupChat
            builder.Entity<Message>()
                .HasOne(m => m.GroupChat)
                .WithMany(gc => gc.Messages)
                .HasForeignKey(m => m.GroupChatId)
                .OnDelete(DeleteBehavior.Restrict);
            // Message replies
            builder.Entity<Message>()
                .HasOne(m => m.ParentMessage)
                .WithMany(m => m.Replies)
                .HasForeignKey(m => m.ParentMessageId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Payment>()
                .HasOne(p => p.Company)
                .WithMany(c => c.Payments)
                .HasForeignKey(p => p.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Payment>()
                .HasOne(p => p.SubscriptionPlan)
                .WithMany(s => s.Payments)
                .HasForeignKey(p => p.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
