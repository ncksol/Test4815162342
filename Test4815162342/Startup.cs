using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Test4815162342.Models;

namespace Test4815162342
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddDbContext<ApiContext>(opt => opt.UseInMemoryDatabase(Guid.NewGuid().ToString()), ServiceLifetime.Singleton, ServiceLifetime.Transient);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            var context = app.ApplicationServices.GetService<ApiContext>();
            context.Users.Add(new User
            {
                Balance= 1000,
                Currency = Currency.GBP,
                CardData = new CreditCardData
                {
                    CardholderName = "Max Green",
                    CardNumber = "4000 0000 0000 0001",
                    CVV = "123",
                    ExpiryDate = "0124",
                }
            });
            context.Users.Add(new User
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
            });
            context.Users.Add(new User
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
            });
            context.Users.Add(new User
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
            });
            context.SaveChanges();
        }
    }
}
