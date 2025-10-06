using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CalendarMaker.Services
{
    public sealed class NationalHolidayService
    {
        private static readonly Uri DataUri = new("https://holidays-jp.github.io/api/v1/date.json");
        private static readonly HttpClient Http = CreateClient();

        private static HttpClient CreateClient()
        {
            return new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        public async Task<HolidayFetchResult> FetchHolidaysAsync(
            int startYear,
            int endYear,
            DateTimeOffset? lastModified = null,
            CancellationToken cancellationToken = default)
        {
            if (startYear > endYear)
            {
                (startYear, endYear) = (endYear, startYear);
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, DataUri);
            if (lastModified.HasValue)
            {
                request.Headers.IfModifiedSince = lastModified.Value.UtcDateTime;
            }

            using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                return new HolidayFetchResult(Array.Empty<NationalHoliday>(), lastModified, true);
            }

            response.EnsureSuccessStatusCode();

            var newLastModified = response.Content.Headers.LastModified ?? response.Headers.Date;

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            var results = new List<NationalHoliday>();

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!DateTime.TryParseExact(property.Name, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    continue;
                }

                if (dt.Year < startYear || dt.Year > endYear) continue;

                var name = property.Value.GetString();
                if (string.IsNullOrWhiteSpace(name)) continue;

                results.Add(new NationalHoliday(DateOnly.FromDateTime(dt), name.Trim()));
            }

            results.Sort(NationalHolidayComparer.Instance);
            return new HolidayFetchResult(results, newLastModified, false);
        }

        private sealed class NationalHolidayComparer : IComparer<NationalHoliday>
        {
            public static NationalHolidayComparer Instance { get; } = new();

            public int Compare(NationalHoliday? x, NationalHoliday? y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (x is null) return -1;
                if (y is null) return 1;

                int cmp = x.Date.CompareTo(y.Date);
                if (cmp != 0) return cmp;
                return string.CompareOrdinal(x.Name, y.Name);
            }
        }
    }

    public sealed record NationalHoliday(DateOnly Date, string Name);

    public sealed record HolidayFetchResult(IReadOnlyList<NationalHoliday> Holidays, DateTimeOffset? LastModified, bool NotModified);
}

