using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using back.domain;
using Microsoft.EntityFrameworkCore;


namespace back;

public class DBContext : DbContext
{
    public DBContext(DbContextOptions<DBContext> options, IConfiguration appsettings) : base(options);


    public DbSet<User> Users { get; set; }

}
