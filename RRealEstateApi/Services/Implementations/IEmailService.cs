using System.Threading.Tasks;
namespace RRealEstateApi.Services.Implementations
{
    public interface IEmailService
    {
        Task SendEmailAsync (string toEmail, string subject, string message );
       // Task SendEmailAsync(string email, string v, string emailBody);
    }
}
