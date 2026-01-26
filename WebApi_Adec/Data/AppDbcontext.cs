using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebApi_Adec.Models.Entity;

namespace WebApi_Adec.Data
{
    public class AppDbcontext : DbContext
    {
        public AppDbcontext(DbContextOptions<AppDbcontext> options) : base(options) { }
        public DbSet<Student> Students { get; set; }
    }
}
