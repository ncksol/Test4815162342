using Microsoft.EntityFrameworkCore;
using Test4815162342.Models;

namespace Test4815162342
{
    public class ApiContext : DbContext
    {
        public ApiContext() { }

        public ApiContext(DbContextOptions<ApiContext> options)
               : base(options)
        {
        }

        public virtual DbSet<Transaction> Transactions { get; set; }
        public virtual DbSet<User> Users { get; set; }
    }
}
