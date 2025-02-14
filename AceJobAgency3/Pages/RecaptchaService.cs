using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace AceJobAgency3.Services
{
    public interface IRecaptchaService
    {
        Task<bool> ValidateTokenAsync(string token);
    }

    public class RecaptchaService : IRecaptchaService
    {
        private readonly string _secretKey;
        private readonly HttpClient _httpClient;

        public RecaptchaService(IConfiguration configuration, HttpClient httpClient)
        {
            _secretKey = configuration["Recaptcha:SecretKey"];
            _httpClient = httpClient;
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            var response = await _httpClient.PostAsync($"https://www.google.com/recaptcha/api/siteverify?secret={_secretKey}&response={token}", null);
            var jsonString = await response.Content.ReadAsStringAsync();
            var recaptchaResponse = JsonSerializer.Deserialize<RecaptchaResponse>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            // Here, we're using a threshold score of 0.5. Adjust if needed.
            return recaptchaResponse.Success && recaptchaResponse.Score >= 0.5;
        }
    }

    public class RecaptchaResponse
    {
        public bool Success { get; set; }
        public float Score { get; set; }
        public string Action { get; set; }
        public string Challenge_ts { get; set; }
        public string Hostname { get; set; }
        public string[] ErrorCodes { get; set; }
    }
}
