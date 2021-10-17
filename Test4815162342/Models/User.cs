using System;

namespace Test4815162342.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public CreditCardData CardData { get; set; }
        public decimal Balance { get; set; }
        public Currency Currency { get; set; }
    }
}
