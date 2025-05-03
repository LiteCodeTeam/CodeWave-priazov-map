using DataBase;
using Microsoft.EntityFrameworkCore;

namespace Backend
{
    public class CleanupService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Проверка каждые 24 часа

        public CleanupService(IServiceProvider services)
        {
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _services.CreateScope()) 
                {
                    var db = scope.ServiceProvider.GetRequiredService<PriazovContext>();

                    // Удаляем устаревшие записи
                    await db.PasswordResetTokens
                        .Where(p => p.ExpiresAt <= DateTime.UtcNow)
                        .ExecuteDeleteAsync();
                    await db.Sessions
                        .Where(s => s.ExpiresAt <= DateTime.UtcNow)
                        .ExecuteDeleteAsync();

                    await db.SaveChangesAsync();
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }
    }
}
