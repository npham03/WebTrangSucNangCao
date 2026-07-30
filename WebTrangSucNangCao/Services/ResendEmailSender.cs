using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WebTrangSucNangCao.Services
{
    public class ResendEmailSender : IEmailSender
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ResendEmailSender> _logger;

        public ResendEmailSender(HttpClient httpClient, IConfiguration configuration, ILogger<ResendEmailSender> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            try
            {
                var apiKey = _configuration["Resend:ApiKey"];
                var fromEmail = _configuration["Resend:FromEmail"] ?? "Rosaria <onboarding@resend.dev>";

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    _logger.LogWarning("Resend API Key is missing in appsettings.json.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    _logger.LogWarning("Recipient email is empty. Cannot send email.");
                    return false;
                }

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var payload = new
                {
                    from = fromEmail,
                    to = new[] { toEmail },
                    subject = subject,
                    html = htmlContent
                };

                string jsonPayload = JsonSerializer.Serialize(payload);
                requestMessage.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(requestMessage);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"[Resend API] Email sent successfully to {toEmail} with subject '{subject}'");
                    return true;
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"[Resend API Error] Failed to send email to {toEmail}. Status: {response.StatusCode}, Details: {errorResponse}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Resend API Exception] Exception sending email to {toEmail}");
                return false;
            }
        }
    }
}
