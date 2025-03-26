using Stripe;
using Stripe.Checkout;

namespace EventHub.Services
{
    public class StripeService : IStripeService
    {
        private readonly string _secretKey;
        private readonly string _publishableKey;

        public StripeService(IConfiguration configuration)
        {
            _secretKey = configuration["Authentication:Stripe:SecretKey"];
            _publishableKey = configuration["Authentication:Stripe:PublishableKey"];
            StripeConfiguration.ApiKey = _secretKey;
        }

        public async Task<Session> CreateCheckoutSessionAsync(decimal amount, string eventName, string ticketType, string successUrl, string cancelUrl)
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            UnitAmount = (long)(amount * 100), // Stripe uses cents
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"{eventName} - {ticketType}",
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
            };

            var service = new SessionService();
            return await service.CreateAsync(options);
        }
    }
}