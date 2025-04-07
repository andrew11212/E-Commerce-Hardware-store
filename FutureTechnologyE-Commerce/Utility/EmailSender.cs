using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace FutureTechnologyE_Commerce.Utility
{
	public class EmailSender : IEmailSender
	{
		private readonly IConfiguration _configuration;

		public EmailSender(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public async Task SendEmailAsync(string email, string subject, string htmlMessage)
		{
			try
			{
				string fromMail = _configuration["EmailSettings:FromEmail"] ?? "noreply@futuretechcommerce.com";
				string fromName = _configuration["EmailSettings:FromName"] ?? "Future Technology";
				string smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
				int smtpPort = Convert.ToInt32(_configuration["EmailSettings:SmtpPort"] ?? "587");
				string smtpUsername = _configuration["EmailSettings:SmtpUsername"] ?? string.Empty;
				string smtpPassword = _configuration["EmailSettings:SmtpPassword"] ?? string.Empty;
				bool enableSsl = Convert.ToBoolean(_configuration["EmailSettings:EnableSsl"] ?? "true");

				MailMessage message = new MailMessage
				{
					From = new MailAddress(fromMail, fromName),
					Subject = subject,
					Body = htmlMessage,
					IsBodyHtml = true
				};
				message.To.Add(new MailAddress(email));

				SmtpClient client = new SmtpClient(smtpHost, smtpPort)
				{
					Credentials = new NetworkCredential(smtpUsername, smtpPassword),
					EnableSsl = enableSsl
				};

				await client.SendMailAsync(message);
			}
			catch (Exception ex)
			{
				// Log the error but don't throw to prevent application failures due to email issues
				Console.WriteLine($"Error sending email: {ex.Message}");
			}
		}
	}
}
