using System;

namespace Test4815162342.Models
{
    public class Transaction
    {
        public decimal Amount { get; set; }
        public Currency Currency { get; set; }
        public CreditCardData CardData { get; set; }
        public Guid Id { get; set; } = Guid.NewGuid();
        public bool IsVoid { get; set; }
        public bool IsRefunded { get; set; }
        public decimal CapturedAmount { get; set; }

    }

    public enum Currency
    {
        GBP
    }
}
