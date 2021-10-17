using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test4815162342.Controllers;
using Xunit;
using Moq;
using Test4815162342;
using Test4815162342.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using AutoFixture;

namespace UnitTests
{
    public class SeedDataFixture : Fixture, IDisposable
    {
        public ApiContext ApiContext { get; set; }

        public static User MaxGreen { get; private set;} = new User
        {
            Balance = 1000,
            Currency = Currency.GBP,
            CardData = new CreditCardData
            {
                CardholderName = "Max Green",
                CardNumber = "4000 0000 0000 0001",
                CVV = "123",
                ExpiryDate = "0124",
            }
        };
        public static User JohnBroke { get; private set;} = new User
        {
            Balance = 1,
            Currency = Currency.GBP,
            CardData = new CreditCardData
            {
                CardholderName = "John Broke",
                CardNumber = "4100 0000 0000 0001",
                CVV = "323",
                ExpiryDate = "0124",
            }
        };
        public static User KatePurple { get; private set;} = new User
        {
            Balance = 350,
            Currency = Currency.GBP,
            CardData = new CreditCardData
            {
                CardholderName = "Kate Purple",
                CardNumber = "4200 0000 0000 0001",
                CVV = "323",
                ExpiryDate = "0124",
            }
        };
        public static User AuthFail { get; private set;} = new User
        {
            Balance = 1000,
            Currency = Currency.GBP,
            CardData = new CreditCardData
            {
                CardholderName = "Auth Fail",
                CardNumber = "4000 0000 0000 0119",
                CVV = "222",
                ExpiryDate = "0124",
            }        
        };
        public static User CaptureFail { get; private set;} = new User
        {
            Balance = 1000,
            Currency = Currency.GBP,
            CardData = new CreditCardData
            {
                CardholderName = "Capture Fail",
                CardNumber = "4000 0000 0000 0259",
                CVV = "333",
                ExpiryDate = "0124",
            }
        };
        public static User RefundFail { get; private set;} = new User
        {
            Balance = 1000,
            Currency = Currency.GBP,
            CardData = new CreditCardData
            {
                CardholderName = "Refund Fail",
                CardNumber = "4000 0000 0000 3238",
                CVV = "555",
                ExpiryDate = "0124",
            }
        };

        public SeedDataFixture()
        {
            var options = new DbContextOptionsBuilder<ApiContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

            ApiContext = new ApiContext(options);

            ApiContext.Users.Add(MaxGreen);
            ApiContext.Users.Add(JohnBroke);
            ApiContext.Users.Add(KatePurple);
            ApiContext.Users.Add(AuthFail);
            ApiContext.Users.Add(CaptureFail);
            ApiContext.Users.Add(RefundFail);
            ApiContext.SaveChanges();
        }

        public void Dispose()
        {
            ApiContext.Dispose();
        }
    }
}
