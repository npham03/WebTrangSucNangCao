using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using WebTrangSucNangCao.Models; // Đảm bảo namespace này khớp với project của bạn

namespace WebTrangSucNangCao.Controllers // Nhớ đổi tên namespace theo project thực tế
{
    [Authorize]
    public class AccountController : Controller
    {
        // 1. Khai báo các dịch vụ dùng chung (Dependency Injection)
        private readonly ShopOnlineContext _database; // Tên context bạn đã scaffold
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(
            ShopOnlineContext database,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _database = database;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // SignInManager trong .NET Core trả về SignInResult
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                // LOGIC GỘP GIỎ HÀNG SESSION VÀO DATABASE
                var sessionCartStr = HttpContext.Session.GetString("Cart");

                if (!string.IsNullOrEmpty(sessionCartStr))
                {
                    var sessionCart = JsonSerializer.Deserialize<List<CartItemViewModel>>(sessionCartStr);

                    if (sessionCart != null && sessionCart.Any())
                    {
                        var user = await _userManager.FindByEmailAsync(model.Email);
                        string UserId = user.Id;

                        var dbCart = _database.UserCarts.Where(c => c.UserId == UserId).ToList();

                        foreach (var sessionItem in sessionCart)
                        {
                            var dbItem = dbCart.FirstOrDefault(c => c.VariantId == sessionItem.VariantId);
                            if (dbItem != null)
                            {
                                dbItem.Quantity = (dbItem.Quantity ?? 0) + sessionItem.Quantity;
                            }
                            else
                            {
                                _database.UserCarts.Add(new UserCart
                                {
                                    UserId = UserId,
                                    VariantId = sessionItem.VariantId,
                                    Quantity = sessionItem.Quantity
                                });
                            }
                        }
                        await _database.SaveChangesAsync();

                        // Xóa giỏ hàng Session
                        HttpContext.Session.Remove("Cart");
                    }
                }
                return RedirectToLocal(returnUrl);
            }
            if (result.RequiresTwoFactor)
            {
                return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
            }
            if (result.IsLockedOut)
            {
                return View("Lockout");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }
        }

        //
        // GET: /Account/VerifyCode
        [AllowAnonymous]
        public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _signInManager.TwoFactorSignInAsync(model.Provider, model.Code, model.RememberMe, model.RememberBrowser);

            if (result.Succeeded)
            {
                return RedirectToLocal(model.ReturnUrl);
            }
            if (result.IsLockedOut)
            {
                return View("Lockout");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid code.");
                return View(model);
            }
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // LƯU Ý: Trong .NET Core mặc định là IdentityUser. 
                // Nếu bạn có thêm Name, LastName, Address thì bạn phải tạo class ApplicationUser : IdentityUser
                // Ở đây mình gán tạm Email và PhoneNumber.
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    Name = model.Name,
                    LastName = model.LastName,
                    Address = model.Address
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // TỰ ĐỘNG XÁC THỰC EMAIL
                    string code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    await _userManager.ConfirmEmailAsync(user, code);

                    // GÁN ROLE "Customer"
                    var roleResult = await _userManager.AddToRoleAsync(user, "Customer");

                    if (roleResult.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return RedirectToAction("Index", "Home");
                    }
                    AddErrors(roleResult);
                }
                AddErrors(result);
            }

            return View(model);
        }

        //
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string UserId, string code)
        {
            if (UserId == null || code == null)
            {
                return View("Error");
            }
            var user = await _userManager.FindByIdAsync(UserId);
            if (user == null)
            {
                return View("Error");
            }
            var result = await _userManager.ConfirmEmailAsync(user, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    return View("ForgotPasswordConfirmation");
                }
                // Xử lý gửi mail reset password ở đây
            }
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
                return View("Login");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction("Login");
            }

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                // THÊM VÀO: Đồng bộ giỏ hàng từ Session vào DB khi đăng nhập thành công qua Google/Facebook
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                if (!string.IsNullOrEmpty(email))
                {
                    await MergeSessionCartToDbAsync(email);
                }

                return RedirectToLocal(returnUrl);
            }
            if (result.IsLockedOut)
            {
                return View("Lockout");
            }
            else
            {
                // Nếu người dùng lần đầu đăng nhập bằng Google/Facebook -> Tự động đăng ký & liên kết luôn
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);

                if (!string.IsNullOrEmpty(email))
                {
                    // 1. Kiểm tra xem email này đã tồn tại trong DB chưa
                    var user = await _userManager.FindByEmailAsync(email);

                    if (user == null)
                    {
                        // 2. Nếu chưa có -> Tạo tài khoản mới với thông tin từ Google/Facebook
                        var name = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? info.Principal.FindFirstValue(ClaimTypes.Name);
                        var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname);

                        user = new ApplicationUser 
                        { 
                            UserName = email, 
                            Email = email,
                            // Thêm Name/LastName nếu ApplicationUser của bạn có các trường này:
                            // Name = name,
                            // LastName = lastName
                        };

                        var createResult = await _userManager.CreateAsync(user);
                        if (!createResult.Succeeded)
                        {
                            return View("Login");
                        }
                    }

                    // 3. Liên kết phương thức đăng nhập (Google/Facebook) vào tài khoản
                    await _userManager.AddLoginAsync(user, info);

                    // 4. Đăng nhập người dùng vào hệ thống
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    // 5. Đồng bộ giỏ hàng từ Session vào DB
                    await MergeSessionCartToDbAsync(email);

                    return RedirectToLocal(returnUrl);
                }

                // Trường hợp hy hữu không lấy được Email từ Google/Facebook thì mới hiện View xác nhận cũ
                ViewBag.ReturnUrl = returnUrl;
                ViewBag.LoginProvider = info.LoginProvider;
                return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = email });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Manage");
            }

            if (ModelState.IsValid)
            {
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email, Name = model.Name, LastName = model.LastName };
                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        // THÊM VÀO: Tự động xác thực email và gán quyền Customer đồng bộ với luồng Register
                        string code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        await _userManager.ConfirmEmailAsync(user, code);
                        await _userManager.AddToRoleAsync(user, "Customer");

                        await _signInManager.SignInAsync(user, isPersistent: false);

                        // Đồng bộ giỏ hàng
                        await MergeSessionCartToDbAsync(model.Email);

                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> LogOff()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                // .NET Core Identity trả về object Error có trường Description
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }
        #endregion

        // Hàm phụ dùng chung để tối ưu hóa việc đồng bộ giỏ hàng, tránh lặp code
        private async Task MergeSessionCartToDbAsync(string email)
        {
            var sessionCartStr = HttpContext.Session.GetString("Cart");
            if (!string.IsNullOrEmpty(sessionCartStr))
            {
                var sessionCart = JsonSerializer.Deserialize<List<CartItemViewModel>>(sessionCartStr);
                if (sessionCart != null && sessionCart.Any())
                {
                    var user = await _userManager.FindByEmailAsync(email);
                    if (user != null)
                    {
                        string userId = user.Id;
                        var dbCart = _database.UserCarts.Where(c => c.UserId == userId).ToList();

                        foreach (var sessionItem in sessionCart)
                        {
                            var dbItem = dbCart.FirstOrDefault(c => c.VariantId == sessionItem.VariantId);
                            if (dbItem != null)
                            {
                                dbItem.Quantity = (dbItem.Quantity ?? 0) + sessionItem.Quantity;
                            }
                            else
                            {
                                _database.UserCarts.Add(new UserCart
                                {
                                    UserId = userId,
                                    VariantId = sessionItem.VariantId,
                                    Quantity = sessionItem.Quantity
                                });
                            }
                        }
                        await _database.SaveChangesAsync();
                        HttpContext.Session.Remove("Cart");
                    }
                }
            }
        }
    }
}