using FutureTechnologyE_Commerce.Models;
using System.Text.RegularExpressions;

namespace FutureTechnologyE_Commerce.Utility
{
    public class PaymentDataValidator
    {
        /// <summary>
        /// Validates billing data for Paymob payment processing
        /// </summary>
        /// <param name="user">The user providing billing information</param>
        /// <returns>A tuple with validation result and error message if applicable</returns>
        public static (bool IsValid, string ErrorMessage) ValidateUserForPayment(ApplicationUser user)
        {
            if (user == null)
            {
                return (false, "User data is missing");
            }

            // Check required fields
            if (string.IsNullOrWhiteSpace(user.first_name))
            {
                return (false, "First name is required");
            }

            if (string.IsNullOrWhiteSpace(user.last_name))
            {
                return (false, "Last name is required");
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return (false, "Email is required");
            }

            if (!Regex.IsMatch(user.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                return (false, "Email format is invalid");
            }

            if (string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                return (false, "Phone number is required");
            }

            // Validate phone number for Egypt format
            string phoneNumber = user.PhoneNumber.Trim();
            if (!phoneNumber.StartsWith("+") && !phoneNumber.StartsWith("00"))
            {
                phoneNumber = "+20" + phoneNumber;
            }
            else if (phoneNumber.StartsWith("00"))
            {
                phoneNumber = "+" + phoneNumber.Substring(2);
            }

            if (!Regex.IsMatch(phoneNumber, @"^\+[0-9]{10,15}$"))
            {
                return (false, "Phone number format is invalid");
            }

            // Address validation
            if (string.IsNullOrWhiteSpace(user.street))
            {
                return (false, "Street address is required");
            }

            if (string.IsNullOrWhiteSpace(user.building))
            {
                return (false, "Building number/name is required");
            }

            if (string.IsNullOrWhiteSpace(user.state))
            {
                return (false, "City/State is required");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Validates order data for Paymob payment processing
        /// </summary>
        /// <param name="orderHeader">The order to be processed</param>
        /// <returns>A tuple with validation result and error message if applicable</returns>
        public static (bool IsValid, string ErrorMessage) ValidateOrderForPayment(OrderHeader orderHeader)
        {
            if (orderHeader == null)
            {
                return (false, "Order data is missing");
            }

            // Validate amount
            if (orderHeader.OrderTotal <= 0)
            {
                return (false, "Order amount must be greater than zero");
            }

            // Validate order billing information
            if (string.IsNullOrWhiteSpace(orderHeader.first_name))
            {
                return (false, "First name is required");
            }

            if (string.IsNullOrWhiteSpace(orderHeader.last_name))
            {
                return (false, "Last name is required");
            }

            if (string.IsNullOrWhiteSpace(orderHeader.phone_number))
            {
                return (false, "Phone number is required");
            }

            if (string.IsNullOrWhiteSpace(orderHeader.email))
            {
                return (false, "Email is required");
            }

            // Validate the amount is in EGP format (no more than 2 decimal places)
            double roundedAmount = Math.Round(orderHeader.OrderTotal, 2);
            if (Math.Abs(roundedAmount - orderHeader.OrderTotal) > 0.000001)
            {
                return (false, "Order amount has more than 2 decimal places");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Formats a phone number for Paymob API, ensuring it has the proper country code
        /// </summary>
        public static string FormatPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return string.Empty;
            }

            // Clean the input first
            phoneNumber = Regex.Replace(phoneNumber.Trim(), @"[^\d+]", "");

            // Add Egypt country code if missing
            if (!phoneNumber.StartsWith("+") && !phoneNumber.StartsWith("00"))
            {
                phoneNumber = "+20" + phoneNumber;
            }
            else if (phoneNumber.StartsWith("00"))
            {
                phoneNumber = "+" + phoneNumber.Substring(2);
            }

            // Ensure max length
            return phoneNumber.Length > 20 ? phoneNumber.Substring(0, 20) : phoneNumber;
        }

        /// <summary>
        /// Safely prepares text data for API submission by sanitizing inputs
        /// </summary>
        public static string SanitizeText(string input, int maxLength = 100)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "NA";
            }

            // Remove potentially dangerous characters
            string sanitized = Regex.Replace(input.Trim(), @"[<>&'""\\/]", "");
            
            // Truncate if needed and return
            return sanitized.Length > maxLength ? sanitized.Substring(0, maxLength) : sanitized;
        }
    }
} 