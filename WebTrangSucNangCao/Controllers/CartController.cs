using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;
using WebTrangSucNangCao.Models; // Đổi lại đúng namespace Models của project

namespace WebTrangSucNangCao.Controllers // Đổi tên namespace cho khớp project
{
    public class CartController : Controller
    {
        // 1. Tiêm Dependency Injection cho DbContext và UserManager
        private readonly ShopOnlineContext _database;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(ShopOnlineContext database, UserManager<ApplicationUser> userManager)
        {
            _database = database;
            _userManager = userManager;
        }

        // TRANG CHÍNH CỦA GIỎ HÀNG (GET) Mở trang giỏ hàng
        public ActionResult Index()
        {
            var cartItems = GetCartItems();

            if (User.Identity.IsAuthenticated)
            {
                var userEmail = User.Identity.Name;

                // EF Core dùng ThenInclude thay cho Select bên trong Include
                var recentOrder = _database.OrderPros
                    .Where(o => o.IdcusNavigation.EmailCus == userEmail && o.Status != "Hoàn tất")
                    .OrderByDescending(o => o.DateOrder)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(d => d.IdproductNavigation)
                    .FirstOrDefault();

                if (recentOrder != null)
                {
                    var productNames = recentOrder.OrderDetails
                                        .Select(d => d.IdproductNavigation.NamePro)
                                        .ToArray();

                    ViewBag.OrderProductNames = string.Join(", ", productNames);
                    ViewBag.OrderTotalQty = recentOrder.OrderDetails.Sum(d => d.Quantity);
                    ViewBag.OrderStatus = recentOrder.Status;
                }
                else
                {
                    ViewBag.OrderProductNames = null;
                }
            }

            return View(cartItems);
        }

        // THÊM VÀO GIỎ (POST)
        [HttpPost]
        public ActionResult AddToCart(int VariantId)
        {
            int finalQuantity = 0;

            if (User.Identity.IsAuthenticated)
            {
                string UserId = _userManager.GetUserId(User);
                var dbItem = _database.UserCarts.FirstOrDefault(c => c.UserId == UserId && c.VariantId == VariantId);
                if (dbItem != null)
                {
                    dbItem.Quantity = (dbItem.Quantity ?? 0) + 1;
                    finalQuantity = dbItem.Quantity.Value;
                }
                else
                {
                    _database.UserCarts.Add(new UserCart { UserId = UserId, VariantId = VariantId, Quantity = 1 });
                    finalQuantity = 1;
                }
                _database.SaveChanges();
            }
            else
            {
                List<CartItemViewModel> sessionCart = GetCartFromSession();
                var sessionItem = sessionCart.FirstOrDefault(c => c.VariantId == VariantId);
                if (sessionItem != null)
                {
                    sessionItem.Quantity++;
                    finalQuantity = sessionItem.Quantity;
                }
                else
                {
                    var variant = _database.ProductVariants.Find(VariantId);
                    if (variant != null)
                    {
                        var product = _database.Products.Find(variant.ProductId.Value);
                        sessionCart.Add(new CartItemViewModel
                        {
                            VariantId = variant.VariantId,
                            VariantName = variant.VariantName,
                            ProductId = product.ProductId,
                            NamePro = product.NamePro,
                            ImagePro = product.ImagePro,
                            Price = product.Price ?? 0,
                            Quantity = 1
                        });
                        finalQuantity = 1;
                    }
                }
                SaveCartToSession(sessionCart);
            }

            var variantInfo = _database.ProductVariants.Include(v => v.Product).FirstOrDefault(v => v.VariantId == VariantId);

            if (variantInfo == null)
            {
                return Json(new { success = false });
            }

            return Json(new
            {
                success = true,
                productName = variantInfo.Product.NamePro,
                productImage = variantInfo.Product.ImagePro,
                quantity = finalQuantity
            });
        }

        [HttpPost]
        public ActionResult BuyNow(int VariantId)
        {
            if (User.Identity.IsAuthenticated)
            {
                string UserId = _userManager.GetUserId(User);
                var dbItem = _database.UserCarts.FirstOrDefault(c => c.UserId == UserId && c.VariantId == VariantId);

                if (dbItem != null)
                {
                    dbItem.Quantity = (dbItem.Quantity ?? 0) + 1;
                }
                else
                {
                    _database.UserCarts.Add(new UserCart { UserId = UserId, VariantId = VariantId, Quantity = 1 });
                }
                _database.SaveChanges();
            }
            else
            {
                List<CartItemViewModel> sessionCart = GetCartFromSession();
                var sessionItem = sessionCart.FirstOrDefault(c => c.VariantId == VariantId);

                if (sessionItem != null) { sessionItem.Quantity++; }
                else
                {
                    var variant = _database.ProductVariants.Find(VariantId);
                    if (variant != null)
                    {
                        var product = _database.Products.Find(variant.ProductId.Value);
                        sessionCart.Add(new CartItemViewModel
                        {
                            VariantId = variant.VariantId,
                            VariantName = variant.VariantName,
                            ProductId = product.ProductId,
                            NamePro = product.NamePro,
                            ImagePro = product.ImagePro,
                            Price = product.Price ?? 0,
                            Quantity = 1
                        });
                    }
                }
                SaveCartToSession(sessionCart);
            }

            return RedirectToAction("Checkout", "Cart");
        }

        // XÓA SẢN PHẨM (POST)
        [HttpPost]
        public ActionResult RemoveFromCart(int VariantId)
        {
            if (User.Identity.IsAuthenticated)
            {
                string UserId = _userManager.GetUserId(User);
                var dbItem = _database.UserCarts.FirstOrDefault(c => c.UserId == UserId && c.VariantId == VariantId);
                if (dbItem != null)
                {
                    _database.UserCarts.Remove(dbItem);
                    _database.SaveChanges();
                }
            }
            else
            {
                List<CartItemViewModel> sessionCart = GetCartFromSession();
                var sessionItem = sessionCart.FirstOrDefault(c => c.VariantId == VariantId);
                if (sessionItem != null)
                {
                    sessionCart.Remove(sessionItem);
                    SaveCartToSession(sessionCart);
                }
            }
            return RedirectToAction("Index");
        }

        // TĂNG SỐ LƯỢNG TRONG ĐƠN HÀNG (POST)
        [HttpPost]
        public ActionResult IncreaseQuantity(int VariantId)
        {
            bool deleted = false;
            int newQuantity = 0;

            if (User.Identity.IsAuthenticated)
            {
                string UserId = _userManager.GetUserId(User);
                var dbItem = _database.UserCarts.FirstOrDefault(c => c.UserId == UserId && c.VariantId == VariantId);
                if (dbItem != null)
                {
                    dbItem.Quantity = (dbItem.Quantity ?? 0) + 1;
                    newQuantity = dbItem.Quantity.Value;
                    _database.SaveChanges();
                }
            }
            else
            {
                List<CartItemViewModel> sessionCart = GetCartFromSession();
                var sessionItem = sessionCart.FirstOrDefault(c => c.VariantId == VariantId);
                if (sessionItem != null) { sessionItem.Quantity++; newQuantity = sessionItem.Quantity; }
                SaveCartToSession(sessionCart);
            }

            var cartData = GetCartTotals();
            return Json(new
            {
                newQuantity = newQuantity,
                lineTotal = cartData.LineTotals[VariantId],
                grandTotal = cartData.GrandTotal,
                deleted = deleted
            });
        }

        // GIẢM SỐ LƯỢNG SẢN PHẨM (POST)
        [HttpPost]
        public ActionResult DecreaseQuantity(int VariantId)
        {
            bool deleted = false;
            int newQuantity = 0;

            if (User.Identity.IsAuthenticated)
            {
                string UserId = _userManager.GetUserId(User);
                var dbItem = _database.UserCarts.FirstOrDefault(c => c.UserId == UserId && c.VariantId == VariantId);
                if (dbItem != null)
                {
                    if (dbItem.Quantity > 1)
                    {
                        dbItem.Quantity--;
                    }
                    newQuantity = dbItem.Quantity ?? 1;
                    _database.SaveChanges();
                }
            }
            else
            {
                List<CartItemViewModel> sessionCart = GetCartFromSession();
                var sessionItem = sessionCart.FirstOrDefault(c => c.VariantId == VariantId);
                if (sessionItem != null)
                {
                    if (sessionItem.Quantity > 1) { sessionItem.Quantity--; newQuantity = sessionItem.Quantity; }
                    else { sessionCart.Remove(sessionItem); deleted = true; }
                    SaveCartToSession(sessionCart);
                }
            }

            var cartData = GetCartTotals();
            decimal lineTotal = deleted ? 0 : cartData.LineTotals.ContainsKey(VariantId) ? cartData.LineTotals[VariantId] : 0;

            return Json(new
            {
                newQuantity = newQuantity,
                lineTotal = lineTotal,
                grandTotal = cartData.GrandTotal,
                deleted = deleted
            });
        }

        // CHECKOUT (GET) 
        public async Task<ActionResult> Checkout()
        {
            var viewModel = new CheckoutViewModel();
            var cartItems = GetCartItems();

            if (cartItems == null || !cartItems.Any())
            {
                return RedirectToAction("Index"); // Giỏ trống thì bắt về giỏ hàng
            }

            viewModel.CartItems = cartItems;
            viewModel.CartSubtotal = cartItems.Sum(item => item.Price * item.Quantity);

            // Kiểm tra và load mã giảm giá đã áp dụng trước đó từ Session (nếu có)
            var sessionCoupon = HttpContext.Session.GetString("AppliedCoupon");
            var sessionDiscountStr = HttpContext.Session.GetString("DiscountAmount");
            if (!string.IsNullOrEmpty(sessionCoupon) && decimal.TryParse(sessionDiscountStr, out decimal discount))
            {
                viewModel.CouponCode = sessionCoupon;
                viewModel.DiscountAmount = discount;
            }

            // Mặc định phí ship ban đầu
            viewModel.ShippingFee = 30000; 
            if (sessionCoupon == "FREESHIP") viewModel.ShippingFee = 0;

            if (User.Identity.IsAuthenticated)
            {
                string UserId = _userManager.GetUserId(User);
                var user = await _userManager.FindByIdAsync(UserId);

                if (user != null)
                {
                    string fullName = $"{user.LastName} {user.Name}".Trim();
                    viewModel.Name = string.IsNullOrEmpty(fullName) ? user.UserName : fullName;
                    viewModel.Phone = user.PhoneNumber;
                    
                    // Nếu user có sẵn địa chỉ từ trước, ta gán tạm vào StreetAddress để khách chỉnh sửa
                    viewModel.StreetAddress = user.Address;
                }
            }

            return View(viewModel);
        }

        // CHỐT ĐƠN (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Checkout(CheckoutViewModel viewModel)
        {
            var cartItems = GetCartItems();
            viewModel.CartItems = cartItems;
            viewModel.CartSubtotal = cartItems.Sum(item => item.Price * item.Quantity);

            // Tái tính toán số tiền giảm giá ở Backend dựa trên Session để bảo mật tuyệt đối
            var sessionCoupon = HttpContext.Session.GetString("AppliedCoupon");
            var sessionDiscountStr = HttpContext.Session.GetString("DiscountAmount");
            if (!string.IsNullOrEmpty(sessionCoupon) && decimal.TryParse(sessionDiscountStr, out decimal discount))
            {
                viewModel.CouponCode = sessionCoupon;
                viewModel.DiscountAmount = discount;
            }

            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            // Ghép chuỗi địa chỉ phân cấp thành địa chỉ đầy đủ trước khi lưu DB
            string fullAddress = $"{viewModel.StreetAddress}, {viewModel.Ward}, {viewModel.District}, {viewModel.Province}";
            viewModel.Address = fullAddress;

            if (User.Identity.IsAuthenticated)
            {
                var currentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
                if (currentUser != null)
                {
                    currentUser.PhoneNumber = viewModel.Phone;
                    currentUser.Address = fullAddress; // Lưu địa chỉ mới nhất ghép từ tỉnh quận phường
                    await _userManager.UpdateAsync(currentUser);
                }
            }

            // 1. TẠO HOẶC LẤY KHÁCH HÀNG
            string emailCus = User.Identity.IsAuthenticated ? User.Identity.Name : "guest@email.com";
            Customer orderCustomer = _database.Customers.FirstOrDefault(c => c.EmailCus == emailCus);
            if (orderCustomer == null)
            {
                orderCustomer = new Customer
                {
                    NameCus = viewModel.Name,
                    PhoneCus = viewModel.Phone,
                    EmailCus = emailCus
                };
                _database.Customers.Add(orderCustomer);
                _database.SaveChanges();
            }
            else
            {
                orderCustomer.NameCus = viewModel.Name;
                orderCustomer.PhoneCus = viewModel.Phone;
                _database.SaveChanges();
            }

            // 2. TẠO ĐƠN HÀNG (OrderPro)
            OrderPro newOrder = new OrderPro
            {
                DateOrder = DateOnly.FromDateTime(DateTime.Now),
                AddressDeliverry = viewModel.Address, // Lưu địa chỉ đầy đủ đã ghép chuỗi
                Status = "Đang xử lý",
                Idcus = orderCustomer.Idcus
            };
            _database.OrderPros.Add(newOrder);
            _database.SaveChanges();

            // 3. Sao chép qua Chi tiết đơn hàng
            foreach (var item in cartItems)
            {
                OrderDetail detail = new OrderDetail
                {
                    Idorder = newOrder.Id,
                    Idproduct = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = (double)item.Price,
                    VariantId = item.VariantId
                };
                _database.OrderDetails.Add(detail);

                // TRỪ TỒN KHO
                var variantInDb = _database.ProductVariants.Find(item.VariantId);
                if (variantInDb != null)
                {
                    variantInDb.Stock = variantInDb.Stock - item.Quantity;
                    if (variantInDb.Stock < 0)
                    {
                        variantInDb.Stock = 0;
                    }
                }
            }
            _database.SaveChanges();

            // 4. Xóa giỏ hàng & Xóa mã giảm giá đã áp dụng trong Session
            ClearCart();
            HttpContext.Session.Remove("AppliedCoupon");
            HttpContext.Session.Remove("DiscountAmount");

            // 5. Chuyển hướng thanh toán
            string paymentMethod = HttpContext.Request.Form["paymentMethod"];
            if (paymentMethod == "QR")
            {
                return RedirectToAction("PaymentQR", new { orderId = newOrder.Id });
            }
            return RedirectToAction("ThankYou", new { id = newOrder.Id });
        }




        // API ÁP DỤNG MÃ GIẢM GIÁ (Ajax gọi từ Frontend)
        [HttpPost]
        public IActionResult ApplyCoupon(string couponCode)
        {
            if (string.IsNullOrEmpty(couponCode))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã giảm giá" });
            }

            // Lấy tổng tiền giỏ hàng hiện tại
            var cartItems = GetCartItems();
            decimal totalCartAmount = cartItems.Sum(item => item.Price * item.Quantity);

            // Giả lập logic kiểm tra Coupon từ database (Bạn có thể map với table Coupon trong DB sau này)
            // Ở đây định nghĩa mẫu một số mã giảm giá thông dụng
            decimal discount = 0;
            string message = "";
            bool success = false;

            string normalizedCode = couponCode.Trim().ToUpper();

            if (normalizedCode == "ROSARIA10") // Giảm 10%
            {
                discount = totalCartAmount * 0.10m;
                message = "Áp dụng mã ROSARIA10 thành công (Giảm 10%)";
                success = true;
            }
            else if (normalizedCode == "GIAM50K") // Giảm thẳng 50k cho đơn từ 500k
            {
                if (totalCartAmount >= 500000)
                {
                    discount = 50000;
                    message = "Áp dụng mã GIAM50K thành công (Giảm 50.000đ)";
                    success = true;
                }
                else
                {
                    message = "Mã GIAM50K chỉ áp dụng cho đơn hàng từ 500.000đ";
                }
            }
            else if (normalizedCode == "FREESHIP") // Ví dụ mã freeship
            {
                // Logic freeship sẽ được tính toán trực tiếp vào phí vận chuyển ở bước Checkout
                discount = 0; 
                message = "Áp dụng mã miễn phí vận chuyển thành công!";
                success = true;
            }
            else
            {
                message = "Mã giảm giá không tồn tại hoặc đã hết hạn";
            }

            // Lưu mã giảm giá đã áp dụng thành công vào Session để Checkout sử dụng lại (tránh hack từ front-end)
            if (success)
            {
                HttpContext.Session.SetString("AppliedCoupon", normalizedCode);
                HttpContext.Session.SetString("DiscountAmount", discount.ToString());
            }
            else
            {
                HttpContext.Session.Remove("AppliedCoupon");
                HttpContext.Session.Remove("DiscountAmount");
            }

            return Json(new { 
                success = success, 
                message = message, 
                discountAmount = discount,
                newTotal = Math.Max(0, totalCartAmount - discount)
            });
        }

        // API TÍNH PHÍ SHIP THEO TỈNH/THÀNH PHỐ (Ajax gọi từ Frontend)
        [HttpGet]
        public IActionResult GetShippingFee(string province)
        {
            decimal shippingFee = 30000; // Phí ship mặc định 30k

            if (!string.IsNullOrEmpty(province))
            {
                string cleanProvince = province.ToLower();
                // Ví dụ: Nội thành TP.HCM hoặc Hà Nội phí ship rẻ hơn, các tỉnh khác đắt hơn
                if (cleanProvince.Contains("hồ chí minh") || cleanProvince.Contains("hcm") || cleanProvince.Contains("hà nội"))
                {
                    shippingFee = 15000; // 15k
                }
                else if (cleanProvince.Contains("đà nẵng") || cleanProvince.Contains("cần thơ") || cleanProvince.Contains("hải phòng"))
                {
                    shippingFee = 25000; // 25k
                }
                else
                {
                    shippingFee = 35000; // Các tỉnh xa 35k
                }
            }

            // Kiểm tra nếu đang có mã giảm giá "FREESHIP" thì đặt ship về 0
            var appliedCoupon = HttpContext.Session.GetString("AppliedCoupon");
            if (appliedCoupon == "FREESHIP")
            {
                shippingFee = 0;
            }

            return Json(new { shippingFee = shippingFee });
        }
        // GET: Cart/ThankYou
        public IActionResult ThankYou(int id)
        {
            // Lấy thông tin đơn hàng kèm chi tiết sản phẩm và thông tin khách hàng
            var order = _database.OrderPros
                .Where(o => o.Id == id)
                .Select(o => new {
                    OrderId = o.Id,
                    Date = o.DateOrder,
                    Address = o.AddressDeliverry,
                    Status = o.Status,
                    CustomerName = _database.Customers.Where(c => c.Idcus == o.Idcus).Select(c => c.NameCus).FirstOrDefault(),
                    CustomerPhone = _database.Customers.Where(c => c.Idcus == o.Idcus).Select(c => c.PhoneCus).FirstOrDefault(),
                    Details = _database.OrderDetails
                        .Where(d => d.Idorder == o.Id)
                        .Select(d => new {
                            ProductName = _database.Products.Where(p => p.ProductId == d.Idproduct).Select(p => p.NamePro).FirstOrDefault(),
                            Quantity = d.Quantity,
                            Price = d.UnitPrice
                        }).ToList()
                }).FirstOrDefault();

            if (order == null)
            {
                // Nếu không tìm thấy đơn hàng, trả về view trống hoặc trang chủ
                return RedirectToAction("Index", "Home");
            }

            // Truyền dữ liệu qua ViewBag để hiển thị ngoài giao diện View
            ViewBag.Order = order;
            return View();
        }

        // =============================================
        // === CÁC HÀM PRIVATE ===
        // =============================================
        private void ClearCart()
        {
            if (User.Identity.IsAuthenticated)
            {
                string UserId = _userManager.GetUserId(User);
                var dbCart = _database.UserCarts.Where(c => c.UserId == UserId);
                _database.UserCarts.RemoveRange(dbCart);
                _database.SaveChanges();
            }
            else
            {
                HttpContext.Session.Remove("Cart");
            }
        }

        private List<CartItemViewModel> GetCartFromSession()
        {
            var sessionCartStr = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(sessionCartStr))
            {
                return new List<CartItemViewModel>();
            }
            return JsonSerializer.Deserialize<List<CartItemViewModel>>(sessionCartStr);
        }

        private void SaveCartToSession(List<CartItemViewModel> cart)
        {
            var jsonString = JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString("Cart", jsonString);
        }

        private List<CartItemViewModel> GetCartItems()
        {
            var cartItems = new List<CartItemViewModel>();
            if (User.Identity.IsAuthenticated)
            {
                string UserId = _userManager.GetUserId(User);
                var dbCart = _database.UserCarts.Where(c => c.UserId == UserId).ToList();

                foreach (var item in dbCart)
                {
                    var variant = _database.ProductVariants.Find(item.VariantId);
                    if (variant != null)
                    {
                        var product = _database.Products.Find(variant.ProductId);
                        if (product != null)
                        {
                            cartItems.Add(new CartItemViewModel
                            {
                                VariantId = variant.VariantId,
                                VariantName = variant.VariantName,
                                ProductId = product.ProductId,
                                NamePro = product.NamePro,
                                ImagePro = product.ImagePro,
                                Price = product.Price ?? 0,
                                Quantity = item.Quantity ?? 0
                            });
                        }
                    }
                }
            }
            else
            {
                cartItems = GetCartFromSession();
            }
            return cartItems;
        }

        private dynamic GetCartTotals()
        {
            decimal grandTotal = 0;
            var lineTotals = new Dictionary<int, decimal>();

            var cartItems = GetCartItems();

            foreach (var item in cartItems)
            {
                var lineTotal = item.Price * item.Quantity;
                lineTotals[item.VariantId] = lineTotal;
                grandTotal += lineTotal;
            }
            return new { GrandTotal = grandTotal, LineTotals = lineTotals };
        }
    }
}