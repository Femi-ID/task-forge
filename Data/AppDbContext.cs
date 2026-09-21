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
    public class AppDbContext(DbContextOptions<AppDbContext> options): IdentityDbContext<AppUser>(options)
    {
        // public DbSet<Portfolio> Portfolios { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // required — configures all the Identity tables

        List<IdentityRole> roles = new()
        {
            new IdentityRole { Name = "Admin", NormalizedName = "ADMIN" },
            new IdentityRole { Name = "User", NormalizedName = "USER"}
        };
        builder.Entity<IdentityRole>().HasData(roles);
        }
    }
}