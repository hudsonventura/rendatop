using api.domain;
using Microsoft.EntityFrameworkCore;


namespace api.repositories;

public class DBContext : DbContext
{
    public DBContext(DbContextOptions<DBContext> options, IConfiguration appsettings) : base(options)
    {
        
    }


    public DbSet<User> Users { get; set; }

}
