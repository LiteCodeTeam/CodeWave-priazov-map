using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;

namespace DataBase
{
    public interface IDbContextFactory<TContext> where TContext : DbContext
    {
        DbContext Create();
    }
    public class PriazovDbContextFactory : IDbContextFactory<PriazovContext>
    {
        public PriazovContext CreateDbContext(string connectionString)
        {
            return new PriazovContext(new DbContextOptions<PriazovContext>());
        }

        DbContext IDbContextFactory<PriazovContext>.Create()
        {
            throw new NotImplementedException();
        }
    }
}
