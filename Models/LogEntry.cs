// Models/LogEntry.cs
using System;

namespace CountriesApi.Models
{
    public class LogEntry
    {
        public string IPAddress { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string CountryCode { get; set; } = string.Empty;
        public bool Blocked { get; set; }
        public string UserAgent { get; set; } = string.Empty;
    }
}