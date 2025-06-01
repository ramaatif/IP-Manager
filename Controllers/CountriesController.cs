
using Microsoft.AspNetCore.Mvc;
using CountriesApi.Models;
using System.Linq;
using System.Collections.Concurrent;
using static CountriesApi.Models.InMemoryStore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace CountriesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CountriesController : ControllerBase
    {
        private readonly ILogger<CountriesController> _logger;
        

        public CountriesController(ILogger<CountriesController> logger)
        {
            _logger = logger;
        }

        public class CountryCodeRequest
        {
            [Required(ErrorMessage = "Country code is required.")]
            public string Code { get; set; }
        }

        [HttpPost("block")]
        public IActionResult BlockCountry([FromBody] CountryCodeRequest countryCode)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state is invalid for BlockCountry request.");
                return BadRequest(ModelState);
            }

            string CountryCode = countryCode.Code.ToUpper();

            if (BlockedCountries.TryAdd(CountryCode, new Country { Code = CountryCode, DurationMinutes = -1 }))
            {
                _logger.LogInformation($"Country {CountryCode} blocked successfully.");
                return Ok(new { Message = $"Country {countryCode.Code} blocked successfully." });
            }
            else
            {
                _logger.LogWarning($"Attempt to block already blocked country: {countryCode.Code}");
                return Conflict(new { Message = $"Country {countryCode.Code} is already blocked." });
            }
        }

        [HttpDelete("block/{countryCode}")]
        public IActionResult DeleteBlockedCountry(string countryCode)
        {
            countryCode = countryCode.ToUpper();

            if (BlockedCountries.TryRemove(countryCode, out _))
            {
                _logger.LogInformation($"Country {countryCode} unblocked successfully.");
                return Ok(new { Message = $"Country {countryCode} unblocked successfully." });
            }
            else
            {
                _logger.LogWarning($"Attempt to unblock non-blocked country: {countryCode}");
                return NotFound(new { Message = $"Country {countryCode} is not currently blocked." });
            }
        }

        [HttpGet("blocked")]
        public IActionResult GetBlockedCountries([FromQuery] SearchParameters parameters)
        {
            var query = BlockedCountries.Values.AsQueryable();

            // Apply search
            if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
            {
                var searchTerm = parameters.SearchTerm.ToUpper();
                query = query.Where(c => c.Code.Contains(searchTerm));
            }

            // Apply sorting
            if (!string.IsNullOrWhiteSpace(parameters.SortBy))
            {
                query = parameters.SortBy.ToLower() switch
                {
                    "code" => parameters.SortDescending
                        ? query.OrderByDescending(c => c.Code)
                        : query.OrderBy(c => c.Code),
                    "duration" => parameters.SortDescending
                        ? query.OrderByDescending(c => c.DurationMinutes)
                        : query.OrderBy(c => c.DurationMinutes),
                    "expiration" => parameters.SortDescending
                        ? query.OrderByDescending(c => c.ExpirationTime)
                        : query.OrderBy(c => c.ExpirationTime),
                    _ => query
                };
            }

            var totalCount = query.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)parameters.PageSize);

            var items = query
                .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .ToList();

            var response = new PaginatedResponse<Country>
            {
                Data = items,
                TotalCount = totalCount,
                PageNumber = parameters.PageNumber,
                PageSize = parameters.PageSize,
                TotalPages = totalPages
            };

            return Ok(response);
        }

        [HttpGet("ip/lookup")]
        [HttpGet("ip/lookup/{ipAddress}")]
        public async Task<IActionResult> FindCountryByIp(string? ipAddress)
        {
            string baseUrl = "https://ipapi.co/";
            string requestUrl = "";

            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(ipAddress))
                {
                    ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                }
            }

            try
            {
                var client = new HttpClient();
                requestUrl = $"{baseUrl}{ipAddress}/json";
                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Add("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                request.Headers.Add("accept-language", "en-US,en;q=0.9");
                request.Headers.Add("priority", "u=0, i");
                request.Headers.Add("sec-ch-ua", "\"Chromium\";v=\"136\", \"Google Chrome\";v=\"136\", \"Not.A/Brand\";v=\"99\"");
                request.Headers.Add("sec-ch-ua-mobile", "?0");
                request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
                request.Headers.Add("sec-fetch-dest", "document");
                request.Headers.Add("sec-fetch-mode", "navigate");
                request.Headers.Add("sec-fetch-site", "none");
                request.Headers.Add("sec-fetch-user", "?1");
                request.Headers.Add("upgrade-insecure-requests", "1");
                request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36");
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var res = await response.Content.ReadAsStringAsync();
                Console.WriteLine(res);
                return Ok(System.Text.Json.JsonDocument.Parse(res));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"Error calling IP lookup service for IP: {ipAddress}");
                return StatusCode(500, $"Error calling IP lookup service: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred during IP lookup for IP: {ipAddress}");
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        [HttpGet("ip/check-block")]
        public async Task<IActionResult> CheckIfIpBlocked()
        {
            string ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(ipAddress))
                ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            string userAgent = HttpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";

            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://ipapi.co/{ipAddress}/json");
                request.Headers.Add("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                request.Headers.Add("accept-language", "en-US,en;q=0.9");
                request.Headers.Add("priority", "u=0, i");
                request.Headers.Add("sec-ch-ua", "\"Chromium\";v=\"136\", \"Google Chrome\";v=\"136\", \"Not.A/Brand\";v=\"99\"");
                request.Headers.Add("sec-ch-ua-mobile", "?0");
                request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
                request.Headers.Add("sec-fetch-dest", "document");
                request.Headers.Add("sec-fetch-mode", "navigate");
                request.Headers.Add("sec-fetch-site", "none");
                request.Headers.Add("sec-fetch-user", "?1");
                request.Headers.Add("upgrade-insecure-requests", "1");
                request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36");
                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                    return StatusCode(500, "Failed to retrieve IP info");

                var content = await response.Content.ReadAsStringAsync();
                var json = System.Text.Json.JsonDocument.Parse(content);
                string countryCode = json.RootElement.GetProperty("country_code").GetString()?.ToUpper() ?? "??";

                bool isBlocked = BlockedCountries.ContainsKey(countryCode);

                // Log the attempt
                LogBlockedAttempt(ipAddress!, countryCode, isBlocked, userAgent);

                return Ok(new
                {
                    IPAddress = ipAddress,
                    CountryCode = countryCode,
                    IsBlocked = isBlocked
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while checking IP block status");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("logs/blocked-attempts")]
        public IActionResult GetBlockedAttempts([FromQuery] SearchParameters parameters)
        {
            var query = BlockedAttemptsLog.Values.AsQueryable();

            // Apply search
            if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
            {
                var searchTerm = parameters.SearchTerm.ToUpper();
                query = query.Where(l =>
                    l.CountryCode.Contains(searchTerm) ||
                    l.IPAddress.Contains(searchTerm) ||
                    l.UserAgent.Contains(searchTerm));
            }

            // Apply sorting
            if (!string.IsNullOrWhiteSpace(parameters.SortBy))
            {
                query = parameters.SortBy.ToLower() switch
                {
                    "timestamp" => parameters.SortDescending
                        ? query.OrderByDescending(l => l.Timestamp)
                        : query.OrderBy(l => l.Timestamp),
                    "countrycode" => parameters.SortDescending
                        ? query.OrderByDescending(l => l.CountryCode)
                        : query.OrderBy(l => l.CountryCode),
                    "ipaddress" => parameters.SortDescending
                        ? query.OrderByDescending(l => l.IPAddress)
                        : query.OrderBy(l => l.IPAddress),
                    _ => query
                };
            }
            else
            {
                // Default sorting by timestamp descending
                query = query.OrderByDescending(l => l.Timestamp);
            }

            var totalCount = query.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)parameters.PageSize);

            var items = query
                .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .ToList();

            var response = new PaginatedResponse<LogEntry>
            {
                Data = items,
                TotalCount = totalCount,
                PageNumber = parameters.PageNumber,
                PageSize = parameters.PageSize,
                TotalPages = totalPages
            };

            return Ok(response);
        }

        [HttpPost("temporal-block")]
        public IActionResult TemporarilyBlockCountry([FromBody] Country request)
        {
            if (string.IsNullOrWhiteSpace(request.Code))
                return BadRequest(new { Message = "Country code is required." });

            string code = request.Code.ToUpper();

            if (BlockedCountries.ContainsKey(code))
                return Conflict(new { Message = $"Country {code} is already temporarily blocked." });

            if (request.DurationMinutes < 1 || request.DurationMinutes > 1440)
                return BadRequest(new { Message = "Duration must be between 1 and 1440 minutes." });

            var expirationTime = DateTime.UtcNow.AddMinutes(request.DurationMinutes);

            Country block = new Country
            {
                Code = code,
                DurationMinutes = request.DurationMinutes,
                ExpirationTime = expirationTime
            };

            BlockedCountries.TryAdd(code, block);

            return Ok(new { Message = $"Country {code} is blocked for {request.DurationMinutes} minutes." });
        }

        private void LogBlockedAttempt(string ipAddress, string countryCode, bool isBlocked, string userAgent)
        {
            string key = $"{ipAddress}-{DateTime.UtcNow.Ticks}";

            var log = new LogEntry
            {
                IPAddress = ipAddress,
                CountryCode = countryCode,
                Blocked = isBlocked,
                Timestamp = DateTime.UtcNow,
                UserAgent = userAgent
            };

            BlockedAttemptsLog.TryAdd(key, log);
        }
    }
}
