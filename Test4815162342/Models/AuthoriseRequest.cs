using Newtonsoft.Json;

namespace Test4815162342.Models
{
    public class AuthoriseRequest
    {
        [JsonProperty("CardholderName")]
        public string CardholderName { get; set; }
        [JsonProperty("CardNumber")]
        public string CardNumber { get; set; }
        [JsonProperty("ExpiryDate")]
        public string ExpiryDate { get; set; }
        [JsonProperty("CVV")]
        public string CVV { get; set; }
        [JsonProperty("Amount")]
        public decimal Amount { get; set; }
        [JsonProperty("Currency")]
        public string Currency { get; set; }
    }
}
