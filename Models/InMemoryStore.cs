    // Models/InMemoryStore.cs
    using System.Collections.Concurrent; // لاستخدام ConcurrentDictionary
    using CountriesApi.Models; // لاستخدام الـ Models (Country, LogEntry, TemporaryBlock)

    namespace CountriesApi.Models 
    {
        public static class InMemoryStore 
        {
            public static ConcurrentDictionary<string, Country> BlockedCountries = new ConcurrentDictionary<string, Country>();
            public static ConcurrentDictionary<string, LogEntry> BlockedAttemptsLog = new ConcurrentDictionary<string, LogEntry>();
            

        }
    }
    