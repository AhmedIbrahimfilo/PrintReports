using Microsoft.EntityFrameworkCore;
using Reports.Entities;

namespace Reports.Data;

public class AppDbContext:DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options):base(options) { }


    public DbSet<Student> Students { get; set; }
}
