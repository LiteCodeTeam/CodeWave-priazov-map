using DataBase;
using DataBase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace Backend
{
    // Сервис для очистки токенов паролей
    public abstract class BaseCleanupService<TEntity> : BackgroundService
    {
        private readonly IDbContextFactory<PriazovContext> _dbContextFactory;
        private readonly TimeSpan _interval;

        protected BaseCleanupService(IDbContextFactory<PriazovContext> dbContextFactory, TimeSpan interval)
        {
            _dbContextFactory = dbContextFactory;
            _interval = interval;
        }

        protected abstract IQueryable<TEntity> GetExpiredQuery(PriazovContext db, DateTime now);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync(stoppingToken);
                var query = GetExpiredQuery(db, DateTime.UtcNow);
                await query.ExecuteDeleteAsync(stoppingToken);

                await Task.Delay(_interval, stoppingToken);
            }
        }
    }

    // Реализации
    public class PasswordTokensCleanupService : BaseCleanupService<PasswordResetToken>
    {
        public PasswordTokensCleanupService(IDbContextFactory<PriazovContext> dbContextFactory)
            : base(dbContextFactory, TimeSpan.FromHours(1)) { }

        protected override IQueryable<PasswordResetToken> GetExpiredQuery(PriazovContext db, DateTime now)
            => db.PasswordResetTokens.Where(p => p.ExpiresAt <= now);
    }

    public class SessionsCleanupService : BaseCleanupService<UserSession>
    {
        public SessionsCleanupService(IDbContextFactory<PriazovContext> dbContextFactory)
            : base(dbContextFactory, TimeSpan.FromHours(24)) { }

        protected override IQueryable<UserSession> GetExpiredQuery(PriazovContext db, DateTime now)
            => db.Sessions.Where(s => s.ExpiresAt <= now);
    }
}
