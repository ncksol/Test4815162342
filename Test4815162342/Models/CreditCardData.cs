using Microsoft.EntityFrameworkCore;
using System;

namespace Test4815162342.Models
{
    public class CreditCardData
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string CardholderName { get; set; }
        public string CardNumber {  get; set; }
        public string CVV { get; set; }
        public string ExpiryDate { get; set; }
    }
}
