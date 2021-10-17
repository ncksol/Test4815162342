using Microsoft.AspNetCore.Mvc;
using System;

namespace Test4815162342.Models
{
    public class AuthoriseResponse : ResponseBase
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
    }
}
