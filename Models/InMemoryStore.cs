    using System.Collections.Concurrent; 
    using CountriesApi.Models; 

    namespace CountriesApi.Models 
    {
        public static class InMemoryStore 
        {
            public static ConcurrentDictionary<string, Country> BlockedCountries = new ConcurrentDictionary<string, Country>();
            public static ConcurrentDictionary<string, LogEntry> BlockedAttemptsLog = new ConcurrentDictionary<string, LogEntry>();
            

        }
    }
    