using Microsoft.EntityFrameworkCore;
using LAPM_API.Models;

namespace LAPM_API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : base(options)
        {
        }

        public DbSet<AccessRequest> AccessRequests { get; set; }
    }
}
