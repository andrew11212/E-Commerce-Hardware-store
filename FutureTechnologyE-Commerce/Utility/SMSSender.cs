using Microsoft.Extensions.Configuration;
using RestSharp;
using System;
using System.Threading.Tasks;

namespace FutureTechnologyE_Commerce.Utility
{
    public interface ISMSSender
    {
        Task SendSmsAsync(string phoneNumber, string message);
    }

    public class SMSSender : ISMSSender
    {
        private readonly IConfiguration _configuration;

        public SMSSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                // Get SMS API settings from configuration
                string apiKey = _configuration["SMSSettings:ApiKey"] ?? string.Empty;
                string apiUrl = _configuration["SMSSettings:ApiUrl"] ?? string.Empty;
                string senderId = _configuration["SMSSettings:SenderId"] ?? "FutureTech";

                // Validate configuration
                if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiUrl))
                {
                    Console.WriteLine("SMS API configuration is missing");
                    return;
                }

                // Format phone number if needed
                if (!phoneNumber.StartsWith("+"))
                {
                    phoneNumber = "+2" + phoneNumber; // Default to Egypt code if not specified
                }

                // Create REST client and request
                var client = new RestClient(apiUrl);
                var request = new RestRequest("", Method.Post);

                // Add parameters based on your SMS API provider
                request.AddParameter("apikey", apiKey);
                request.AddParameter("to", phoneNumber);
                request.AddParameter("message", message);
                request.AddParameter("sender", senderId);

                // Execute the request
                var response = await client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                {
                    Console.WriteLine($"SMS API error: {response.Content}");
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw to prevent application failures
                Console.WriteLine($"Error sending SMS: {ex.Message}");
            }
        }
    }
} 