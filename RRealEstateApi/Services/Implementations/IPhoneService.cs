
namespace RRealEstateApi.Services.Implementations
{
    public interface IPhoneService
    {
        Task SendSmsAsync(string phoneNumber, string smsMessage);
    }
}
