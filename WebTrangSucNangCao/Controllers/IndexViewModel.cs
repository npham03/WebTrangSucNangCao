using Microsoft.AspNetCore.Identity;

namespace WebTrangSucNangCao.Controllers
{
    internal class IndexViewModel
    {
        public bool HasPassword { get; set; }
        public string PhoneNumber { get; set; }
        public bool TwoFactor { get; set; }
        public IList<UserLoginInfo> Logins { get; set; }
        public bool BrowserRemembered { get; set; }
    }
}