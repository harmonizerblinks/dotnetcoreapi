using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TemplateApi.Models
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext()
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Sequence> Sequence { get; set; }
        public DbSet<SmsApi> SmsApi { get; set; }
        public DbSet<Sms> Sms { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }
        
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Sequence>().HasData(
                new Sequence { SequenceId = 1, Name = "Wallet", Prefix = "W", Counter = 1, Length = 4, Date = DateTime.Now, UserId = "807ba6c0-e845-4695-847e-92edca9d66db" });
            
            builder.Entity<AppUser>().HasData(
                new AppUser
                {
                    UserName = "Acyst", FullName = "Harmony Alabi", Image = "2019-02-02 T 02 22 11.jpg",
                    NormalizedUserName = "Acyst", PhoneNumber = "0238288675", Email = "info@acyst.tech",
                    NormalizedEmail = "INFO@ACYST.TECH", EmailConfirmed = true, UserType = "Admin",
                    Login = DateTime.UtcNow, LogOut = DateTime.UtcNow, MDate = DateTime.UtcNow,
                    IsLoggedIn = false, AccessFailedCount = 0,
                    MUserId = "807ba6c0-e845-4695-847e-92edca9d66db",
                    SecurityStamp = "CJ2EVAYDU6HKQPMFCY7A3ROLDIQNWNRM",
                    ConcurrencyStamp = "073d68a5-ad7d-4312-9b25-d86d1a604329",
                    PasswordHash = "AQAAAAEAACcQAAAAEOrzhtqT+9ZuBKNmqNY/4xVf8ruHsdTHMfE8KCVsipUBA9CjcZMVjYRaR0Nzl8jgKQ=="
                });
            
            base.OnModelCreating(builder);
        }
    }
}
