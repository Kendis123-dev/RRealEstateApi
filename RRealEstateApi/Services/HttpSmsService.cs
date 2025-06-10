using RRealEstateApi.Services.Implementations;

public class HttpSmsService : IPhoneService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public HttpSmsService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task SendSmsAsync(string phoneNumber, string message)
    {
        var apiKey = _config["SmsGateway:ApiKey"];
        var sender = _config["SmsGateway:Sender"];

        var requestBody = new
        {
            to = phoneNumber,
            from = sender,
            message = message
        };

        var response = await _httpClient.PostAsJsonAsync("https://your-sms-provider.com/api/send", requestBody);

        if (!response.IsSuccessStatusCode)
            throw new Exception("Failed to send SMS");
    }
}