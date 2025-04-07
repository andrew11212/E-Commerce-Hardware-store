using FutureTechnologyE_Commerce.Utility;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FutureTechnologyE_Commerce.Services
{
    public interface INotificationService
    {
        Task SendOrderConfirmationAsync(string email, string phoneNumber, string customerName, int orderId, DateTime estimatedDelivery);
        Task SendOrderStatusUpdateAsync(string email, string phoneNumber, string customerName, int orderId, string newStatus);
        Task SendShipmentNotificationAsync(string email, string phoneNumber, string customerName, int orderId, string trackingNumber);
        Task SendPromotionAsync(string email, string phoneNumber, string customerName, string promotionTitle, string promotionDetails);
    }

    public class NotificationService : INotificationService
    {
        private readonly IEmailSender _emailSender;
        private readonly ISMSSender _smsSender;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IEmailSender emailSender,
            ISMSSender smsSender,
            ILogger<NotificationService> logger)
        {
            _emailSender = emailSender;
            _smsSender = smsSender;
            _logger = logger;
        }

        public async Task SendOrderConfirmationAsync(string email, string phoneNumber, string customerName, int orderId, DateTime estimatedDelivery)
        {
            try
            {
                string subject = $"Order Confirmation - Order #{orderId}";
                string emailBody = GenerateOrderConfirmationEmail(customerName, orderId, estimatedDelivery);
                string smsMessage = $"Dear {customerName}, your order #{orderId} has been confirmed. Estimated delivery: {estimatedDelivery.ToString("MMM dd, yyyy")}. Thank you for shopping with FutureTech!";

                // Send notifications in parallel
                var emailTask = _emailSender.SendEmailAsync(email, subject, emailBody);
                var smsTask = _smsSender.SendSmsAsync(phoneNumber, smsMessage);

                await Task.WhenAll(emailTask, smsTask);
                _logger.LogInformation("Order confirmation notification sent for order {OrderId}", orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order confirmation notification for order {OrderId}", orderId);
            }
        }

        public async Task SendOrderStatusUpdateAsync(string email, string phoneNumber, string customerName, int orderId, string newStatus)
        {
            try
            {
                string subject = $"Order Status Update - Order #{orderId}";
                string emailBody = GenerateOrderStatusUpdateEmail(customerName, orderId, newStatus);
                string smsMessage = $"Dear {customerName}, your order #{orderId} status has been updated to {newStatus}. Visit our website for more details.";

                // Send notifications in parallel
                var emailTask = _emailSender.SendEmailAsync(email, subject, emailBody);
                var smsTask = _smsSender.SendSmsAsync(phoneNumber, smsMessage);

                await Task.WhenAll(emailTask, smsTask);
                _logger.LogInformation("Order status update notification sent for order {OrderId} - Status: {Status}", orderId, newStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order status update notification for order {OrderId}", orderId);
            }
        }

        public async Task SendShipmentNotificationAsync(string email, string phoneNumber, string customerName, int orderId, string trackingNumber)
        {
            try
            {
                string subject = $"Your Order Has Shipped - Order #{orderId}";
                string emailBody = GenerateShipmentEmail(customerName, orderId, trackingNumber);
                string smsMessage = $"Dear {customerName}, your order #{orderId} has shipped! Track it with: {trackingNumber}. Thank you for shopping with FutureTech!";

                // Send notifications in parallel
                var emailTask = _emailSender.SendEmailAsync(email, subject, emailBody);
                var smsTask = _smsSender.SendSmsAsync(phoneNumber, smsMessage);

                await Task.WhenAll(emailTask, smsTask);
                _logger.LogInformation("Shipment notification sent for order {OrderId}", orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending shipment notification for order {OrderId}", orderId);
            }
        }

        public async Task SendPromotionAsync(string email, string phoneNumber, string customerName, string promotionTitle, string promotionDetails)
        {
            try
            {
                string subject = $"Special Offer: {promotionTitle}";
                string emailBody = GeneratePromotionEmail(customerName, promotionTitle, promotionDetails);
                string smsMessage = $"Dear {customerName}, check out our special offer: {promotionTitle}! Visit our website for details.";

                // Send notifications in parallel
                var emailTask = _emailSender.SendEmailAsync(email, subject, emailBody);
                var smsTask = _smsSender.SendSmsAsync(phoneNumber, smsMessage);

                await Task.WhenAll(emailTask, smsTask);
                _logger.LogInformation("Promotion notification sent - {PromotionTitle}", promotionTitle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending promotion notification {PromotionTitle}", promotionTitle);
            }
        }

        #region Email Template Generators

        private string GenerateOrderConfirmationEmail(string customerName, int orderId, DateTime estimatedDelivery)
        {
            return $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #4285f4; color: white; padding: 15px; text-align: center; }}
                    .content {{ padding: 20px; border: 1px solid #ddd; }}
                    .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h2>Order Confirmation</h2>
                    </div>
                    <div class='content'>
                        <p>Dear {customerName},</p>
                        <p>Thank you for your order! We're pleased to confirm that your order #{orderId} has been received and is being processed.</p>
                        <p><strong>Estimated Delivery Date:</strong> {estimatedDelivery.ToString("MMMM dd, yyyy")}</p>
                        <p>You will receive another notification when your order ships.</p>
                        <p>If you have any questions about your order, please visit our website or contact our customer service team.</p>
                        <p>Thank you for shopping with FutureTech!</p>
                    </div>
                    <div class='footer'>
                        <p>&copy; {DateTime.Now.Year} Future Technology. All rights reserved.</p>
                        <p>This is an automated message, please do not reply to this email.</p>
                    </div>
                </div>
            </body>
            </html>";
        }

        private string GenerateOrderStatusUpdateEmail(string customerName, int orderId, string newStatus)
        {
            return $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #4285f4; color: white; padding: 15px; text-align: center; }}
                    .content {{ padding: 20px; border: 1px solid #ddd; }}
                    .status {{ padding: 15px; background-color: #f9f9f9; border-left: 4px solid #4285f4; margin: 15px 0; }}
                    .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h2>Order Status Update</h2>
                    </div>
                    <div class='content'>
                        <p>Dear {customerName},</p>
                        <p>We're writing to inform you that the status of your order #{orderId} has been updated.</p>
                        <div class='status'>
                            <p><strong>New Status:</strong> {newStatus}</p>
                        </div>
                        <p>You can track your order anytime by logging into your account on our website.</p>
                        <p>If you have any questions, please contact our customer service team.</p>
                        <p>Thank you for shopping with FutureTech!</p>
                    </div>
                    <div class='footer'>
                        <p>&copy; {DateTime.Now.Year} Future Technology. All rights reserved.</p>
                        <p>This is an automated message, please do not reply to this email.</p>
                    </div>
                </div>
            </body>
            </html>";
        }

        private string GenerateShipmentEmail(string customerName, int orderId, string trackingNumber)
        {
            return $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #4285f4; color: white; padding: 15px; text-align: center; }}
                    .content {{ padding: 20px; border: 1px solid #ddd; }}
                    .tracking {{ padding: 15px; background-color: #f9f9f9; border-left: 4px solid #4CAF50; margin: 15px 0; }}
                    .button {{ display: inline-block; padding: 10px 20px; background-color: #4285f4; color: white; text-decoration: none; border-radius: 4px; }}
                    .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h2>Your Order Has Shipped!</h2>
                    </div>
                    <div class='content'>
                        <p>Dear {customerName},</p>
                        <p>Great news! Your order #{orderId} has shipped and is on its way to you.</p>
                        <div class='tracking'>
                            <p><strong>Tracking Number:</strong> {trackingNumber}</p>
                        </div>
                        <p>You can track your package by clicking the button below:</p>
                        <p style='text-align: center;'>
                            <a href='https://futuretechcommerce.com/track?number={trackingNumber}' class='button'>Track Your Package</a>
                        </p>
                        <p>If you have any questions, please contact our customer service team.</p>
                        <p>Thank you for shopping with FutureTech!</p>
                    </div>
                    <div class='footer'>
                        <p>&copy; {DateTime.Now.Year} Future Technology. All rights reserved.</p>
                        <p>This is an automated message, please do not reply to this email.</p>
                    </div>
                </div>
            </body>
            </html>";
        }

        private string GeneratePromotionEmail(string customerName, string promotionTitle, string promotionDetails)
        {
            return $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #FF5722; color: white; padding: 15px; text-align: center; }}
                    .content {{ padding: 20px; border: 1px solid #ddd; }}
                    .promo {{ padding: 15px; background-color: #FFF3E0; border-left: 4px solid #FF5722; margin: 15px 0; }}
                    .button {{ display: inline-block; padding: 10px 20px; background-color: #FF5722; color: white; text-decoration: none; border-radius: 4px; }}
                    .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h2>Special Offer Just for You!</h2>
                    </div>
                    <div class='content'>
                        <p>Dear {customerName},</p>
                        <p>We're excited to share a special offer with you:</p>
                        <div class='promo'>
                            <h3>{promotionTitle}</h3>
                            <p>{promotionDetails}</p>
                        </div>
                        <p>Don't miss out on this limited-time offer!</p>
                        <p style='text-align: center;'>
                            <a href='https://futuretechcommerce.com/promotions' class='button'>Shop Now</a>
                        </p>
                        <p>Thank you for being a valued customer of FutureTech!</p>
                    </div>
                    <div class='footer'>
                        <p>&copy; {DateTime.Now.Year} Future Technology. All rights reserved.</p>
                        <p>This is an automated message, please do not reply to this email.</p>
                        <p><small>If you wish to unsubscribe from promotional emails, <a href='https://futuretechcommerce.com/unsubscribe'>click here</a>.</small></p>
                    </div>
                </div>
            </body>
            </html>";
        }

        #endregion
    }
} 