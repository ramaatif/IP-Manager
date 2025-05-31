using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using CountriesApi.Models;
using static CountriesApi.Models.InMemoryStore;

public class TemporaryBlockCleanupService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var expired = BlockedCountries
                .Where(kvp => kvp.Value.ExpirationTime <= now&& kvp.Value.DurationMinutes != -1)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expired)
            {
                BlockedCountries.TryRemove(key, out _);
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
