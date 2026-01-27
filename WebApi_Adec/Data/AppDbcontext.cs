using Microsoft.EntityFrameworkCore;
using WebApi_Adec.Models.Entity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;


namespace WebApi_Adec.Data
{
    public class AppDbcontext : IdentityDbContext<IdentityUser>
    {
        public AppDbcontext(DbContextOptions<AppDbcontext> options) : base(options) { }
        public DbSet<Student> Students { get; set; }


    }

}
