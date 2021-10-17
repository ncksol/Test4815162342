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
    public class PaymentControllerTests : IClassFixture<SeedDataFixture>
    {
        private readonly SeedDataFixture _dataFixture;

        public PaymentControllerTests(SeedDataFixture seedDataFixture)
        {
            _dataFixture = seedDataFixture;
        }

        [Fact]
        public async Task CallAuthorise_WithValidDetails_SuccessfulResponse()
        {
            var controller = new PaymentController(_dataFixture.ApiContext);

            var user = CreateUser();
            user.Balance = 1000;
            _dataFixture.ApiContext.SaveChanges();

            var request = new AuthoriseRequest
            {
                Amount = 100,
                Currency = "GBP",
                CardholderName = user.CardData.CardholderName,
                CardNumber = user.CardData.CardNumber,
                CVV = user.CardData.CVV,
                ExpiryDate = user.CardData.ExpiryDate,
            };

            var response = await controller.Authorise(request);
            var result = response as OkObjectResult;
            var value = result?.Value as AuthoriseResponse;

            Assert.IsType<OkObjectResult>(response);
            Assert.IsType<AuthoriseResponse>(result?.Value);
            Assert.NotNull(result);
            Assert.NotNull(value);

            Assert.True(value.Success);
            Assert.Null(value.Error);
            Assert.False(String.IsNullOrEmpty(value.Id.ToString()));
            Assert.Equal(1000, value.Amount);
            Assert.Equal("GBP", value.Currency);
        }

        [Fact]
        public async Task CallAuthorise_WithErrorCardNumber_ErrorResponse()
        {
            var controller = new PaymentController(_dataFixture.ApiContext);

            var user = CreateUser();
            user.Balance = 1000;
            user.CardData.CardNumber = "4000 0000 0000 0119";
            _dataFixture.ApiContext.SaveChanges();

            var request = new AuthoriseRequest
            {
                Amount = 100,
                Currency = "GBP",
                CardholderName = user.CardData.CardholderName,
                CardNumber = user.CardData.CardNumber,
                CVV = user.CardData.CVV,
                ExpiryDate = user.CardData.ExpiryDate,
            };

            var response = await controller.Authorise(request);
            var result = response as ObjectResult;
            var value = result?.Value as ProblemDetails;

            Assert.IsType<ObjectResult>(response);
            Assert.IsType<ProblemDetails>(result?.Value);
            Assert.NotNull(result);
            Assert.NotNull(value);

            Assert.Equal(500, value.Status);
        }


        [Theory]
        [InlineData(10000, "GBP", true, true, true, true)]
        [InlineData(100, "USD", true, true, true, true)]
        [InlineData(100, "GBP", false, true, true, true)]
        [InlineData(100, "GBP", true, false, true, true)]
        [InlineData(100, "GBP", true, true, false, true)]
        [InlineData(100, "GBP", true, true, true, false)]
        public async Task CallAuthorise_WithInvalidDetails_BadRequestResponse(decimal amount, string currency, bool isCardholderNameCorrect, bool isCardNumberCorrect, bool isCvvCorrect, bool isExpiryDateCorrect)
        {
            var controller = new PaymentController(_dataFixture.ApiContext);

            var user = CreateUser();
            user.Balance = 1000;
            _dataFixture.ApiContext.SaveChanges();

            var request = new AuthoriseRequest
            {
                Amount = amount,
                Currency = currency,
                CardholderName = isCardholderNameCorrect ? user.CardData.CardholderName : _dataFixture.Create<string>(),
                CardNumber = isCardNumberCorrect ? user.CardData.CardNumber : _dataFixture.Create<string>(),
                CVV = isCvvCorrect ? user.CardData.CVV : _dataFixture.Create<string>(),
                ExpiryDate = isExpiryDateCorrect ? user.CardData.ExpiryDate : _dataFixture.Create<string>(),
            };

            var response = await controller.Authorise(request);
            var result = response as BadRequestObjectResult;
            var value = result?.Value as AuthoriseResponse;

            Assert.IsType<BadRequestObjectResult>(response);
            Assert.NotNull(result);
            Assert.NotNull(value);

            Assert.False(value.Success);
            Assert.False(String.IsNullOrEmpty(value.Error));
        }

        [Fact]
        public async Task CallAuthorise_WithInvalidAmount_BadRequestResponse()
        {
            var controller = new PaymentController(_dataFixture.ApiContext);

            var user = CreateUser();
            user.Balance = 1;
            _dataFixture.ApiContext.SaveChanges();

            var request = new AuthoriseRequest
            {
                Amount = 100,
                Currency = "GBP",
                CardholderName = user.CardData.CardholderName,
                CardNumber = user.CardData.CardNumber,
                CVV = user.CardData.CVV,
                ExpiryDate = user.CardData.ExpiryDate,
            };

            var response = await controller.Authorise(request);
            var result = response as BadRequestObjectResult;
            var value = result?.Value as AuthoriseResponse;

            Assert.IsType<BadRequestObjectResult>(response);
            Assert.IsType<AuthoriseResponse>(result?.Value);

            Assert.NotNull(result);
            Assert.NotNull(value);

            Assert.False(value.Success);
            Assert.False(String.IsNullOrEmpty(value.Error));
        }

        [Fact]
        public async Task CallAuthorise_NewTransactionSavedInDB()
        {
            var controller = new PaymentController(_dataFixture.ApiContext);

            var user = CreateUser();
            user.Balance = 1000;
            _dataFixture.ApiContext.SaveChanges();

            var request = new AuthoriseRequest
            {
                Amount = 100,
                Currency = "GBP",
                CardholderName = user.CardData.CardholderName,
                CardNumber = user.CardData.CardNumber,
                CVV = user.CardData.CVV,
                ExpiryDate = user.CardData.ExpiryDate,
            };

            var response = await controller.Authorise(request);

            var result = response as OkObjectResult;
            var value = result?.Value as AuthoriseResponse;
            Assert.IsType<OkObjectResult>(response);
            Assert.IsType<AuthoriseResponse>(result?.Value);

            var savedTransaction = _dataFixture.ApiContext.Transactions.Find(value.Id);            

            Assert.NotNull(savedTransaction);
            Assert.Equal(request.Amount, savedTransaction.Amount);
            Assert.Equal(request.Currency, savedTransaction.Currency.ToString());
            Assert.Equal(request.CardholderName, savedTransaction.CardData?.CardholderName);
            Assert.Equal(request.CardNumber, savedTransaction.CardData?.CardNumber);
            Assert.Equal(request.CVV, savedTransaction.CardData?.CVV);
            Assert.Equal(request.ExpiryDate, savedTransaction.CardData?.ExpiryDate);
        }
        
        [Fact]
        public async Task CallCapture_WithValidDetails_SuccessfulResponse()
        {
            var controller = new PaymentController(_dataFixture.ApiContext);

            var (user, transaction) = CreateUserAndTransaction();
            user.Balance = 150;
            transaction.Amount = 50;

            _dataFixture.ApiContext.SaveChanges();

            var request = new CaptureRequest
            {
                Id = transaction.Id.ToString(),
                Amount = 50
            };

            var response = await controller.Capture(request);
            var result = response as OkObjectResult;
            var value = result?.Value as CaptureResponse;

            Assert.IsType<OkObjectResult>(response);
            Assert.IsType<CaptureResponse>(result?.Value);
            Assert.NotNull(result);
            Assert.NotNull(value);

            Assert.True(value.Success);
            Assert.Null(value.Error);
            Assert.Equal(100, value.Amount);
            Assert.Equal("GBP", value.Currency);
        }
        
        [Fact]
        public async Task CallCapture_WithErrorCardNumber_ErrorResponse()
        {
            var controller = new PaymentController(_dataFixture.ApiContext);

            var (user, transaction) = CreateUserAndTransaction();
            user.Balance = 150;
            user.CardData.CardNumber = "4000 0000 0000 0259";
            transaction.Amount = 50;

            _dataFixture.ApiContext.SaveChanges();

            var request = new CaptureRequest
            {
                Id = transaction.Id.ToString(),
                Amount = 50
            };

            var response = await controller.Capture(request);
            var result = response as ObjectResult;
            var value = result?.Value as ProblemDetails;

            Assert.IsType<ObjectResult>(response);
            Assert.IsType<ProblemDetails>(result?.Value);
            Assert.NotNull(result);
            Assert.NotNull(value);

            Assert.Equal(500, value.Status);
        }
        
        [Fact]
        public async Task CallCapture_WithInvalidTransactionId_NotFoundResponse()
        {
            var controller = new PaymentController(_dataFixture.ApiContext);

            var request = new CaptureRequest
            {
                Id = Guid.NewGuid().ToString(),
                Amount = 50
            };

            var response = await controller.Capture(request);
            var result = response as NotFoundObjectResult;
            var value = result?.Value as CaptureResponse;

            Assert.IsType<NotFoundObjectResult>(response);
            Assert.IsType<CaptureResponse>(result?.Value);
            Assert.NotNull(result);
            Assert.NotNull(value);

            Assert.False(value.Success);
            Assert.NotNull(value.Error);
        }
        
        [Theory]
        [InlineData(1000, false, false)]
        [InlineData(100, true, false)]
        [InlineData(100, false, true)]
        public async Task CallCapture_WithInvalidDetails_BadRequestResponse(decimal amount, bool isVoid, bool isRefunded)
        {
            var controller = new PaymentController(_dataFixture.ApiContext);

            var (user, transaction) = CreateUserAndTransaction();
            user.Balance = 150;
            transaction.Amount = 100;
            transaction.IsVoid = isVoid;
            transaction.IsRefunded = isRefunded;

            _dataFixture.ApiContext.SaveChanges();

            var request = new CaptureRequest
            {
                Id = transaction.Id.ToString(),
                Amount = amount
            };

            var response = await controller.Capture(request);
            var result = response as BadRequestObjectResult;
            var value = result?.Value as CaptureResponse;

            Assert.IsType<BadRequestObjectResult>(response);
            Assert.IsType<CaptureResponse>(result?.Value);
            Assert.NotNull(result);
            Assert.NotNull(value);

            Assert.False(value.Success);
            Assert.NotNull(value.Error);
        }
        
        [Fact]
        public async Task CallCapture_WithInvalidTransactionId_BadRequestResponse()
        {
            var controller = new PaymentController(_dataFixture.ApiContext);
            var request = new CaptureRequest
            {
                Id = "aaa",
                Amount = 100
            };

            var response = await controller.Capture(request);
            var result = response as BadRequestObjectResult;
            var value = result?.Value as CaptureResponse;

            Assert.IsType<BadRequestObjectResult>(response);
            Assert.IsType<CaptureResponse>(result?.Value);
            Assert.NotNull(result);
            Assert.NotNull(value);

            Assert.False(value.Success);
            Assert.NotNull(value.Error);
        }
        
        [Fact]
        public async Task CallCapture_TransactionAmountCaptured_UserBalanceUpdated()
        {
            var controller = new PaymentController(_dataFixture.ApiContext);

            var (user, transaction) = CreateUserAndTransaction();
            user.Balance = 150;
            transaction.Amount = 50;
            _dataFixture.ApiContext.SaveChanges();

            var request = new CaptureRequest
            {
                Id = transaction.Id.ToString(),
                Amount = 50
            };

            var response = await controller.Capture(request);
            var result = response as OkObjectResult;
            var value = result?.Value as CaptureResponse;

            Assert.IsType<OkObjectResult>(response);
            Assert.IsType<CaptureResponse>(result?.Value);
            Assert.NotNull(result);
            Assert.NotNull(value);

            Assert.True(value.Success);
            Assert.Null(value.Error);
            Assert.Equal(100, user.Balance);
            Assert.Equal(50, transaction.CapturedAmount);
        }

        [Fact]
        public async Task CallVoid_WithValidDetails_SuccessfulResponse()
        {
            var controller = new PaymentController(_dataFixture.ApiContext);

            var (user, transaction) = CreateUserAndTransaction();
            user.Balance = 333;
            transaction.Amount = 50;

            _dataFixture.ApiContext.SaveChanges();

            var response = await controller.Void(transaction.Id.ToString());
            var result = response as OkObjectResult;
            var value = result?.Value as VoidResponse;

            Assert.IsType<OkObjectResult>(response);
            Assert.IsType<VoidResponse>(result?.Value);
            Assert.NotNull(result);
            Assert.NotNull(value);

            Assert.True(value.Success);
            Assert.Null(value.Error);
            Assert.Equal(333, value.Amount);
            Assert.Equal("GBP", value.Currency);
            Assert.True(transaction.IsVoid);
        }
        

        [Fact]
        public async Task CallVoid_WithInvalidTransactionId_BadRequestResponse()
        {
            var controller = new PaymentController(_dataFixture.ApiContext);

            var response = await controller.Void("aaa123");
            var result = response as BadRequestObjectResult;
            var value = result?.Value as VoidResponse;

            Assert.IsType<BadRequestObjectResult>(response);
            Assert.IsType<VoidResponse>(result?.Value);
            Assert.NotNull(result);
            Assert.NotNull(value);

            Assert.False(value.Success);
            Assert.NotNull(value.Error);
        }
        
        [Fact]
        public async Task CallVoid_WithMissingTransactionId_NotFoundResponse()
        {
            var controller = new PaymentController(_dataFixture.ApiContext);

            var response = await controller.Void(Guid.NewGuid().ToString());
            var result = response as NotFoundObjectResult;
            var value = result?.Value as VoidResponse;

            Assert.IsType<NotFoundObjectResult>(response);
            Assert.IsType<VoidResponse>(result?.Value);
            Assert.NotNull(result);
            Assert.NotNull(value);

            Assert.False(value.Success);
            Assert.NotNull(value.Error);
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public async Task CallVoid_WithInvalidTransaction_BadRequestResponse(bool isVoid, bool isRefunded)
        {
            var controller = new PaymentController(_dataFixture.ApiContext);

            var (user, transaction) = CreateUserAndTransaction();
            user.Balance = 333;
            transaction.Amount = 50;
            transaction.IsVoid = isVoid;
            transaction.IsRefunded = isRefunded;

            _dataFixture.ApiContext.SaveChanges();

            var response = await controller.Void(transaction.Id.ToString());
            var result = response as BadRequestObjectResult;
            var value = result?.Value as VoidResponse;

            Assert.IsType<BadRequestObjectResult>(response);
            Assert.IsType<VoidResponse>(result?.Value);
            Assert.NotNull(result);
            Assert.NotNull(value);

            Assert.False(value.Success);
            Assert.NotNull(value.Error);
        }


        [Fact]
        public async Task CallRefund_WithValidDetails_SuccessfulResponse()
        {
            var controller = new PaymentController(_dataFixture.ApiContext);

            var (user, transaction) = CreateUserAndTransaction();
            user.Balance = 333;
            transaction.Amount = 50;
            transaction.CapturedAmount = 50;

            _dataFixture.ApiContext.SaveChanges();

            var request = new RefundRequest
            {
                Id = transaction.Id.ToString(),
                Amount = 50,
            };

            var response = await controller.Refund(request);
            var result = response as OkObjectResult;
            var value = result?.Value as RefundResponse;

            Assert.IsType<OkObjectResult>(response);
            Assert.IsType<RefundResponse>(result?.Value);
            Assert.NotNull(result);
            Assert.NotNull(value);

            Assert.True(value.Success);
            Assert.Null(value.Error);
            Assert.Equal(383, value.Amount);
            Assert.Equal("GBP", value.Currency);
            Assert.True(transaction.IsRefunded);
            Assert.Equal(383, user.Balance);
            Assert.Equal(0, transaction.CapturedAmount);
        }
        

        [Fact]
        public async Task CallRefund_WithErrorCardNumber_ErrorResponse()
        {
            var controller = new PaymentController(_dataFixture.ApiContext);

            var (user, transaction) = CreateUserAndTransaction();
            user.Balance = 333;
            user.CardData.CardNumber = "4000 0000 0000 3238";
            transaction.Amount = 50;
            transaction.CapturedAmount = 50;

            _dataFixture.ApiContext.SaveChanges();

            var request = new RefundRequest
            {
                Id = transaction.Id.ToString(),
                Amount = 50,
            };

            var response = await controller.Refund(request);
            var result = response as ObjectResult;
            var value = result?.Value as ProblemDetails;

            Assert.IsType<ObjectResult>(response);
            Assert.IsType<ProblemDetails>(result?.Value);
            Assert.NotNull(result);
            Assert.NotNull(value);

            Assert.Equal(500, value.Status);
        }


        [Fact]
        public async Task CallRefund_WithInvalidTransactionId_BadRequestResponse()
        {
            var controller = new PaymentController(_dataFixture.ApiContext);

            var request = new RefundRequest
            {
                Id = "aaa123",
                Amount = 50,
            };

            var response = await controller.Refund(request);
            var result = response as BadRequestObjectResult;
            var value = result?.Value as RefundResponse;

            Assert.IsType<BadRequestObjectResult>(response);
            Assert.IsType<RefundResponse>(result?.Value);
            Assert.NotNull(result);
            Assert.NotNull(value);

            Assert.False(value.Success);
            Assert.NotNull(value.Error);
        }

        [Fact]
        public async Task CallRefund_WithMissingTransactionId_NotFoundResponse()
        {
            var controller = new PaymentController(_dataFixture.ApiContext);

            var request = new RefundRequest
            {
                Id = Guid.NewGuid().ToString(),
                Amount = 50,
            };

            var response = await controller.Refund(request);
            var result = response as NotFoundObjectResult;
            var value = result?.Value as RefundResponse;

            Assert.IsType<NotFoundObjectResult>(response);
            Assert.IsType<RefundResponse>(result?.Value);
            Assert.NotNull(result);
            Assert.NotNull(value);

            Assert.False(value.Success);
            Assert.NotNull(value.Error);
        }


        [Fact]
        public async Task CallRefund_ForVoidedTransaction_BadRequestResponse()
        {
            var controller = new PaymentController(_dataFixture.ApiContext);

            var (user, transaction) = CreateUserAndTransaction();
            user.Balance = 333;
            transaction.Amount = 50;
            transaction.CapturedAmount = 50;
            transaction.IsVoid = true;
            _dataFixture.ApiContext.SaveChanges();

            var request = new RefundRequest
            {
                Id = transaction.Id.ToString(),
                Amount = 50,
            };

            var response = await controller.Refund(request);
            var result = response as BadRequestObjectResult;
            var value = result?.Value as RefundResponse;

            Assert.IsType<BadRequestObjectResult>(response);
            Assert.IsType<RefundResponse>(result?.Value);
            Assert.NotNull(result);
            Assert.NotNull(value);

            Assert.False(value.Success);
            Assert.NotNull(value.Error);
        }
        
        [Fact]
        public async Task CallRefund_ForAmountHigherThanCaptured_BadRequestResponse()
        {
            var controller = new PaymentController(_dataFixture.ApiContext);

            var (user, transaction) = CreateUserAndTransaction();
            user.Balance = 333;
            transaction.Amount = 50;
            transaction.CapturedAmount = 50;

            _dataFixture.ApiContext.SaveChanges();

            var request = new RefundRequest
            {
                Id = transaction.Id.ToString(),
                Amount = 75,
            };

            var response = await controller.Refund(request);
            var result = response as BadRequestObjectResult;
            var value = result?.Value as RefundResponse;

            Assert.IsType<BadRequestObjectResult>(response);
            Assert.IsType<RefundResponse>(result?.Value);
            Assert.NotNull(result);
            Assert.NotNull(value);

            Assert.False(value.Success);
            Assert.NotNull(value.Error);
        }


        private User CreateUser()
        {
            var user = new User
            {
                Currency = Currency.GBP,
                CardData = new CreditCardData
                {
                    CardholderName = _dataFixture.Create<string>(),
                    CardNumber = _dataFixture.Create<string>(),
                    CVV = _dataFixture.Create<string>(),
                    ExpiryDate = _dataFixture.Create<string>(),
                }
            };
            _dataFixture.ApiContext.Users.Add(user);

            return user;
        }

        private (User, Transaction) CreateUserAndTransaction()
        {
            var user = CreateUser();
            var transaction = new Transaction
            {
                Currency = Currency.GBP,
                CardData = new CreditCardData
                {
                    CardholderName = user.CardData.CardholderName,
                    CardNumber = user.CardData.CardNumber,
                    CVV = user.CardData.CVV,
                    ExpiryDate = user.CardData.ExpiryDate
                }
            };
            _dataFixture.ApiContext.Transactions.Add(transaction);

            return (user, transaction);
        }
    }
}
