using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CalendarMaker.Models;

namespace CalendarMaker.Services
{
    public sealed class HolidayCacheService
    {
        private const string CacheFileName = "holidays-cache.json";

        private static string GetCachePath()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CalendarMaker");
            return Path.Combine(dir, CacheFileName);
        }

        public HolidayCacheDto? TryLoad()
        {
            try
            {
                var path = GetCachePath();
                if (!File.Exists(path)) return null;
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<HolidayCacheDto>(json);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> TrySaveAsync(HolidayCacheDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var path = GetCachePath();
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public sealed record HolidayCacheDto(
        DateTimeOffset? SourceLastModified,
        DateTimeOffset? LastFetched,
        DateOnly? RangeStart,
        DateOnly? RangeEnd,
        List<HolidayItemDto> Holidays);

    public sealed record HolidayItemDto(DateOnly Date, string Label);
}

