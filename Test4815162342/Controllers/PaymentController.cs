using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Test4815162342.Models;

namespace Test4815162342.Controllers
{
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly ApiContext _dbContext;

        public PaymentController(ApiContext dbContext)
        {
            this._dbContext = dbContext;
        }

        [HttpPost("payment/authorise")]
        public async Task<IActionResult> Authorise([FromBody] AuthoriseRequest request)
        {
            if(request.CardNumber == "4000 0000 0000 0119")
                return Problem();

            var transaction = new Transaction();
            transaction.Amount = request.Amount;
            if (Enum.TryParse(typeof(Currency), request.Currency, out var currency))
            {
                transaction.Currency = (Currency)currency;
            }
            else
            {
                return new BadRequestObjectResult(new AuthoriseResponse { Success = false, Error = "Invalid currency" });
            }

            var user = _dbContext.Users.FirstOrDefault(
                x => x.CardData.CardholderName == request.CardholderName && x.CardData.CardNumber == request.CardNumber && x.CardData.ExpiryDate == request.ExpiryDate && x.CardData.CVV == request.CVV);
            if (user == null)
                return new BadRequestObjectResult(new AuthoriseResponse { Success = false, Error = "Invalid card details" });

            if (user.Balance < request.Amount)
                return new BadRequestObjectResult(new AuthoriseResponse { Success = false, Error = "Insufficient balance" });

            var cardData = new CreditCardData
            {

                CardNumber = request.CardNumber,
                CardholderName = request.CardholderName,
                CVV = request.CVV,
                ExpiryDate = request.ExpiryDate,
            };
            transaction.CardData = cardData;

            _dbContext.Transactions.Add(transaction);
            await _dbContext.SaveChangesAsync();

            var response = new AuthoriseResponse
            {
                Id = transaction.Id,
                Amount = user.Balance,
                Currency = user.Currency.ToString(),
                Success = true
            };

            return new OkObjectResult(response);
        }

        [HttpPost("payment/capture")]
        public async Task<IActionResult> Capture([FromBody] CaptureRequest request)
        {            
            if (Guid.TryParse(request.Id, out var transactionId) == false)
                return new BadRequestObjectResult(new CaptureResponse { Success = false, Error = "Invalid Id" });

            var transaction = await _dbContext.Transactions.FindAsync(transactionId);

            if (transaction == null)
                return new NotFoundObjectResult(new CaptureResponse { Success = false, Error = $"Transaction with id { request.Id } not found." });

            if(transaction.CardData.CardNumber == "4000 0000 0000 0259")
                return Problem();

            if (transaction.Amount - transaction.CapturedAmount < request.Amount)
                return new BadRequestObjectResult(new CaptureResponse { Success = false, Error = "Invalid amount" });

            if(transaction.IsVoid)
                return new BadRequestObjectResult(new CaptureResponse { Success = false, Error = "Transaction is void" });

            if (transaction.IsRefunded)
                return new BadRequestObjectResult(new CaptureResponse { Success = false, Error = "Transaction is refunded" });

            var user = _dbContext.Users.FirstOrDefault(
                x => x.CardData.CardNumber == transaction.CardData.CardNumber && x.CardData.ExpiryDate == transaction.CardData.ExpiryDate && x.CardData.CVV == transaction.CardData.CVV);
            if (user == null)
                return Problem(detail: "User not found");

            user.Balance -= request.Amount;
            transaction.CapturedAmount += request.Amount;
            await _dbContext.SaveChangesAsync();

            var response = new CaptureResponse
            {
                Success = true,
                Amount = user.Balance,
                Currency = user.Currency.ToString(),
            };

            return new OkObjectResult(response);
        }

        [HttpPost("payment/void/{id}")]
        public async Task<IActionResult> Void([FromRoute()] string id)
        {
            if (Guid.TryParse(id, out var transactionId) == false)
                return new BadRequestObjectResult(new VoidResponse { Success = false, Error = "Invalid Id" });

            var transaction = await _dbContext.Transactions.FindAsync(transactionId);

            if (transaction == null)
                return new NotFoundObjectResult(new VoidResponse { Success = false, Error = $"Transaction with id {id} not found." });

            if (transaction.IsVoid)
                return new BadRequestObjectResult(new VoidResponse { Success = false, Error = "Transaction is void" });

            if (transaction.IsRefunded)
                return new BadRequestObjectResult(new VoidResponse { Success = false, Error = "Transaction is refunded" });

            var user = _dbContext.Users.FirstOrDefault(
                x => x.CardData.CardNumber == transaction.CardData.CardNumber && x.CardData.ExpiryDate == transaction.CardData.ExpiryDate && x.CardData.CVV == transaction.CardData.CVV);
            if (user == null)
                return Problem(detail: "User not found");

            transaction.IsVoid = true;
            await _dbContext.SaveChangesAsync();

            return new OkObjectResult(new VoidResponse
            {
                Success = true,
                Amount = user.Balance,
                Currency = user.Currency.ToString(),
            });
        }

        [HttpPost("payment/refund")]
        public async Task<IActionResult> Refund([FromBody] RefundRequest request)
        {
            if (Guid.TryParse(request.Id, out var transactionId) == false)
                return new BadRequestObjectResult(new RefundResponse { Success = false, Error = "Invalid Id" });

            var transaction = await _dbContext.Transactions.FindAsync(transactionId);

            if (transaction == null)
                return new NotFoundObjectResult(new RefundResponse { Success = false, Error = $"Transaction with id { request.Id } not found." });

            if (transaction.CardData.CardNumber == "4000 0000 0000 3238")
                return Problem();

            if (transaction.IsVoid)
                return new BadRequestObjectResult(new RefundResponse { Success = false, Error = "Transaction is void" });

            if(transaction.CapturedAmount < request.Amount)
                return new BadRequestObjectResult(new RefundResponse { Success = false, Error = "Invalid amount" });

            var user = _dbContext.Users.FirstOrDefault(
                x => x.CardData.CardNumber == transaction.CardData.CardNumber && x.CardData.ExpiryDate == transaction.CardData.ExpiryDate && x.CardData.CVV == transaction.CardData.CVV);
            if (user == null)
                return Problem(detail: "User not found");

            transaction.IsRefunded = true;
            transaction.CapturedAmount -= request.Amount;
            user.Balance += request.Amount;

            return new OkObjectResult(new RefundResponse
            {
                Success = true,
                Amount = user.Balance,
                Currency = user.Currency.ToString(),
            });
        }
    }
}
