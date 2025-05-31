// Models/Country.cs//

using System.ComponentModel.DataAnnotations;

namespace CountriesApi.Models
{
    public class Country
    {

        [Required(ErrorMessage = "Country code is required.")]
        public required string Code { get; set; }
        public int DurationMinutes { get; set; }  //4min
        public DateTime ExpirationTime { get; set; }  
    }
}