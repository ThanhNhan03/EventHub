using Stripe.Checkout;
namespace EventHub.Services 
{

    public interface IStripeService
    {
        Task<Session> CreateCheckoutSessionAsync(decimal amount, 
            string eventName, string ticketType, string successUrl, string cancelUrl);
    }

}
