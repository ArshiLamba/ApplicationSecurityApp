using ApplicationSecurityApp.Models;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace ApplicationSecurityApp.Services
{
    public class ReCaptchaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _secretKey;

        public ReCaptchaService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _secretKey = configuration["GoogleReCaptcha:SecretKey"]; // Store in appsettings.json
        }

        public async Task<bool> VerifyTokenAsync(string token)
        {
            var response = await _httpClient.GetStringAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={_secretKey}&response={token}"
            );

            var reCaptchaResponse = JsonSerializer.Deserialize<ReCaptchaResponse>(response);

            return reCaptchaResponse?.success == true && reCaptchaResponse.score >= 0.5; // Adjust threshold if needed
        }
    }
}
