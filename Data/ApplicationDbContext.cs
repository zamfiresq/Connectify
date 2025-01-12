using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Connectify.Models;

namespace Connectify.Data
{
    // PASUL 3: useri si roluri
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet
        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<FollowRequest> FollowRequests { get; set; }
        public DbSet<UserGroup> UserGroups { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }


        // on model creating - pentru a seta relatiile 
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // tabelele asociative
            // UserGroup 
            modelBuilder.Entity<UserGroup>()
                .HasKey(ugr => new {
                    ugr.UserId, 
                    ugr.GroupId 
                });

            // many to many cu modelele User si Group
            modelBuilder.Entity<UserGroup>()
                .HasOne(ugr => ugr.User)
                .WithMany(usr => usr.UserGroups)
                .HasForeignKey(ugr => ugr.UserId);

            modelBuilder.Entity<UserGroup>()
                .HasOne(ugr => ugr.Group)
                .WithMany(grp => grp.UserGroups)
                .HasForeignKey(ugr => ugr.GroupId);

            // FollowRequest
            modelBuilder.Entity<FollowRequest>()
                .HasKey(frq => new
                {
                    frq.SenderId,
                    frq.ReceiverId
                });

            // many to many cu modelele User si User
            modelBuilder.Entity<FollowRequest>()
                .HasOne(frq => frq.Sender)
                .WithMany(u => u.Following)
                .HasForeignKey(frq => frq.SenderId)
                .OnDelete(DeleteBehavior.Restrict);     // daca stergem un user, nu vrem sa stergem si cererile de follow

            modelBuilder.Entity<FollowRequest>()
                .HasOne(frq => frq.Receiver)
                .WithMany(u => u.Followers)                             
                .HasForeignKey(frq => frq.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);     // daca stergem un user, nu vrem sa stergem si cererile de follow

        }




    }
}
