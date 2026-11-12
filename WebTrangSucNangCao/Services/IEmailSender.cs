namespace WebTrangSucNangCao.Services
{
    public interface IEmailSender
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent);
    }
}
