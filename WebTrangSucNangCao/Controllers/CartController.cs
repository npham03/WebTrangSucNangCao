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

            viewModel.CartItems = cartItems;
            viewModel.GrandTotal = cartItems.Sum(item => item.Price * item.Quantity);

            if (User.Identity.IsAuthenticated)
            {
                string UserId = _userManager.GetUserId(User);
                var user = await _userManager.FindByIdAsync(UserId);

                if (user != null)
                {
                    string fullName = $"{user.LastName} {user.Name}".Trim();
                    viewModel.FullName = string.IsNullOrEmpty(fullName) ? user.UserName : fullName;
                    viewModel.Phone = user.PhoneNumber;
                    viewModel.Address = user.Address;
                }
            }

            return View(viewModel);
        }

        // CHỐT ĐƠN (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Checkout(CheckoutViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                viewModel.CartItems = GetCartItems();
                viewModel.GrandTotal = viewModel.CartItems.Sum(item => item.Price * item.Quantity);
                return View(viewModel);
            }

            if (User.Identity.IsAuthenticated)
            {
                var currentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

                if (currentUser != null)
                {
                    currentUser.PhoneNumber = viewModel.Phone;
                    currentUser.Address = viewModel.Address;
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
                    NameCus = viewModel.FullName,
                    PhoneCus = viewModel.Phone,
                    EmailCus = emailCus
                };
                _database.Customers.Add(orderCustomer);
                _database.SaveChanges();
            }
            else
            {
                orderCustomer.NameCus = viewModel.FullName;
                orderCustomer.PhoneCus = viewModel.Phone;
                _database.SaveChanges();
            }

            // 2. TẠO ĐƠN HÀNG (OrderPro)
            OrderPro newOrder = new OrderPro
            {
                DateOrder = DateOnly.FromDateTime(DateTime.Now),
                AddressDeliverry = viewModel.Address,
                Status = "Đang xử lý",
                Idcus = orderCustomer.Idcus
            };
            _database.OrderPros.Add(newOrder);
            _database.SaveChanges();

            // 3. Lấy giỏ hàng
            var cartItems = GetCartItems();

            // 4. Copy qua Chi tiết đơn hàng
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

            // 5. Xóa giỏ hàng
            ClearCart();

            // 6. Chuyển qua trang Cảm ơn hoặc trang QR
            string paymentMethod = HttpContext.Request.Form["paymentMethod"];
            if (paymentMethod == "QR")
            {
                return RedirectToAction("PaymentQR", new { orderId = newOrder.Id });
            }
            return RedirectToAction("ThankYou");
        }

        public ActionResult ThankYou()
        {
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