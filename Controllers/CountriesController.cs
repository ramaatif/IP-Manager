// Controllers/CountriesController.cs
using Microsoft.AspNetCore.Mvc; // لإضافة ControllerBase و IActionResult (لإرجاع الردود)
using CountriesApi.Models; 
using System.Linq; // لاستخدام LINQ (لعمليات زي الفرز والبحث)
using System.Collections.Concurrent; // لاستخدام ConcurrentDictionary (المخزن بتاعنا)
using static CountriesApi.Models.InMemoryStore;  // لاستخدام المخزن بتاعنا مباشرة بدون كتابة InMemoryStore.
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations; // لإضافة ILogger

namespace CountriesApi.Controllers
{
    [ApiController] // يعرف الكلاس كـ API Controller
    [Route("api/[controller]")] // هذا يحدد المسار الأساسي للـ Endpoints (مثال: /api/countries)
    public class CountriesController : ControllerBase // Controllers يجب أن ترث من ControllerBase
    {
        // هذا جزء خاص بتسجيل الأحداث (Logging)
        private readonly ILogger<CountriesController> _logger;
        

        // هذا هو الـ Constructor للـ Controller
        public CountriesController(ILogger<CountriesController> logger)
        {
            _logger = logger; // يتم حقن الـ logger هنا
        }

        // هنا تبدأ الـ Endpoints الخاصة بإدارة الدول المحظورة

        public class CountryCodeRequest
        {
            [Required(ErrorMessage = "Country code is required.")] // التأكد أن كود الدولة مطلوب
            public string Code { get; set; }
        }

        [HttpPost("block")] // طلب POST لإضافة دولة محظورة
        public IActionResult BlockCountry([FromBody] CountryCodeRequest countryCode)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state is invalid for BlockCountry request.");
                return BadRequest(ModelState); // نرجع 400 Bad Request مع تفاصيل أخطاء التحقق
            }



            string CountryCode = countryCode.Code.ToUpper(); // توحيد كود الدولة لحروف كبيرة

            if (BlockedCountries.TryAdd(CountryCode, new Country { Code = CountryCode,DurationMinutes=-1}))
            {
                _logger.LogInformation($"Country {CountryCode} blocked successfully.");
                return Ok($"Country {countryCode.Code} blocked successfully."); // 200 OK
            }
            else
            {
                _logger.LogWarning($"Attempt to block already blocked country: {countryCode.Code}");
                return Conflict($"Country {countryCode.Code} is already blocked."); // 409 Conflict
            }
        }

        [HttpDelete("block/{countryCode}")] // طلب DELETE لحذف دولة محظورة
        public IActionResult DeleteBlockedCountry(string countryCode)
        {
            countryCode = countryCode.ToUpper(); // توحيد كود الدولة لحروف كبيرة

            if (BlockedCountries.TryRemove(countryCode, out _)) // محاولة حذف الدولة
            {
                _logger.LogInformation($"Country {countryCode} unblocked successfully.");
                return Ok($"Country {countryCode} Deleted successfully."); // 204 No Content
            }
            else
            {
                _logger.LogWarning($"Attempt to unblock non-blocked country: {countryCode}");
                return NotFound($"Country {countryCode} is not currently blocked."); // 404 Not Found
            }
        }

        [HttpGet("blocked")] // طلب GET لجلب كل الدول المحظورة
        public IActionResult GetBlockedCountries()
        {
            var blockedCountriesList = BlockedCountries.Values.ToList(); // جلب الدول من المخزن
            _logger.LogInformation($"Retrieved {blockedCountriesList.Count} blocked countries.");
            return Ok(blockedCountriesList); // 200 OK مع قائمة الدول
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
public IActionResult GetBlockedAttempts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
{
    var allLogs = BlockedAttemptsLog.Values
        .OrderByDescending(log => log.Timestamp)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToList();

    return Ok(new
    {
        Total = BlockedAttemptsLog.Count,
        Page = page,
        PageSize = pageSize,
        Data = allLogs
    });
}


[HttpPost("temporal-block")]
public IActionResult TemporarilyBlockCountry([FromBody] Country request)
{
  if (string.IsNullOrWhiteSpace(request.Code))
        return BadRequest("Country code is required.");


    string code = request.Code.ToUpper();

    if (BlockedCountries.ContainsKey(code))
        return Conflict($"Country {code} is already temporarily blocked.");


  if (request.DurationMinutes < 1 || request.DurationMinutes > 1440)
        return BadRequest("Duration must be between 1 and 1440 minutes.");


            var expirationTime = DateTime.UtcNow.AddMinutes(request.DurationMinutes);



    Country block = new Country
    {
        Code = code,
        DurationMinutes = request.DurationMinutes,
        ExpirationTime = expirationTime
    };

    BlockedCountries.TryAdd(code, block);

    return Ok($"Country {code} is blocked for {request.DurationMinutes} minutes.");
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
