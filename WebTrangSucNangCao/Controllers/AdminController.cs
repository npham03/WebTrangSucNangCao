using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebTrangSucNangCao.Models;
using WebTrangSucNangCao.Services;
using System.Diagnostics;

namespace WebTrangSucNangCao.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        // 1. Khai báo các dịch vụ (Dependency Injection)
        private readonly ShopOnlineContext _database;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailSender _emailSender;

        public AdminController(
            ShopOnlineContext database,
            IWebHostEnvironment env,
            IEmailSender emailSender)
        {
            _database = database;
            _env = env;
            _emailSender = emailSender;
        }

        // HÀM 1: (GET) Mở trang Dashboard (Index)
        public ActionResult Index()
        {
            var viewModel = new AdminDashboardViewModel();

            // Lưu ý: EF Core thường đổi "Category1" thành "CategoryNavigation" hoặc "IdcateNavigation"
            viewModel.Products = _database.Products.Include(p => p.CategoryNavigation).ToList();
            viewModel.Sliders = _database.Sliders.ToList();
            viewModel.NewSlider = new Slider();

            // A. TÍNH DOANH SỐ 
            var revenueDouble = _database.OrderDetails
                .Where(d => d.IdorderNavigation.Status == "Hoàn tất")
                .Sum(d => (double?)d.Quantity * d.UnitPrice) ?? 0;

            viewModel.TotalRevenue = (decimal)revenueDouble;

            // B. ĐẾM ĐƠN HÀNG CHƯA XỬ LÝ
            viewModel.PendingOrdersCount = _database.OrderPros
                .Count(o => o.Status == "Đang xử lý");

            // C. TỔNG SỐ SẢN PHẨM ĐÃ BÁN (Chỉ tính đơn "Hoàn tất")
            var soldCount = _database.OrderDetails
                .Where(d => d.IdorderNavigation.Status == "Hoàn tất")
                .Sum(d => d.Quantity) ?? 0;
            viewModel.TotalProductsSold = soldCount;

            // D. LẤY SẢN PHẨM SẮP HẾT HÀNG (Tồn kho <= 5)
            viewModel.LowStockVariants = _database.ProductVariants
                .Include(v => v.Product)
                .Where(v => v.Stock <= 5)
                .OrderBy(v => v.Stock)
                .Take(10)
                .ToList();

            return View(viewModel);
        }

        // (POST) Xử lý form "Thêm Slider"
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSlider(AdminDashboardViewModel viewModel, IFormFile ImageFile)
        {
            var newSlider = viewModel.NewSlider;

            // Xử lý ảnh với IFormFile và IWebHostEnvironment
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var fileName = Path.GetFileName(ImageFile.FileName);
                var path = Path.Combine(_env.WebRootPath, "images", "banners", fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                newSlider.ImagePath = "~/images/banners/" + fileName;

                _database.Sliders.Add(newSlider);
                await _database.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        // (POST) Xử lý nút "Xóa Slider"
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteSlider(int id)
        {
            Slider slider = _database.Sliders.Find(id);
            if (slider != null)
            {
                _database.Sliders.Remove(slider);
                _database.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // GET: MỞ TRANG DANH SÁCH SẢN PHẨM
        public ActionResult ProductList()
        {
            // Sửa Category1 thành CategoryNavigation
            var products = _database.Products.Include(p => p.CategoryNavigation).OrderByDescending(p => p.ProductId).ToList();
            return View(products);
        }

        // HÀM 1: (GET) Mở trang AddProduct
        public ActionResult AddProduct()
        {
            var categories = _database.Categories.ToList();
            ViewBag.CategoryList = new SelectList(categories, "Idcate", "NameCate");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(Product p, IEnumerable<IFormFile> ImageFiles, int? InitialStock)
        {
            ViewBag.CategoryList = new SelectList(_database.Categories.ToList(), "Idcate", "NameCate");

            try
            {
                if (ModelState.IsValid)
                {
                    if (ImageFiles == null || !ImageFiles.Any() || ImageFiles.First() == null)
                    {
                        ModelState.AddModelError("", "Vui lòng thêm ít nhất 1 ảnh");
                        return View(p);
                    }

                    // LẤY ẢNH BÌA
                    var firstFile = ImageFiles.First();
                    var firstFileName = Path.GetFileName(firstFile.FileName);
                    var firstFilePath = Path.Combine(_env.WebRootPath, "images", "products", firstFileName);

                    using (var stream = new FileStream(firstFilePath, FileMode.Create))
                    {
                        await firstFile.CopyToAsync(stream);
                    }

                    p.ImagePro = "~/images/products/" + firstFileName;

                    _database.Products.Add(p);
                    await _database.SaveChangesAsync();

                    // TỰ TẠO SIZE MẶC ĐỊNH
                    if (InitialStock.HasValue && InitialStock.Value > 0)
                    {
                        ProductVariant defaultVariant = new ProductVariant
                        {
                            ProductId = p.ProductId,
                            VariantName = "FreeSize",
                            Stock = InitialStock.Value
                        };
                        _database.ProductVariants.Add(defaultVariant);
                    }

                    // LƯU ẢNH GALLERY
                    foreach (var file in ImageFiles)
                    {
                        if (file != null && file.Length > 0)
                        {
                            var fileName = Path.GetFileName(file.FileName);
                            var path = Path.Combine(_env.WebRootPath, "images", "products", fileName);

                            using (var stream = new FileStream(path, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            var imagePathDb = "~/images/products/" + fileName;

                            ProductImage galleryImage = new ProductImage
                            {
                                ProductId = p.ProductId,
                                ImagePath = imagePathDb
                            };
                            _database.ProductImages.Add(galleryImage);
                        }
                    }

                    await _database.SaveChangesAsync();
                    return RedirectToAction("Edit", new { id = p.ProductId });
                }

                return View(p);
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "Đã xảy ra lỗi khi lưu vào cơ sở dữ liệu: " + ex.Message);
                return View(p);
            }
        }

        // XÓA SẢN PHẨM (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            Product product = _database.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            var orderDetails = _database.OrderDetails.Where(o => o.Idproduct == id);
            if (orderDetails.Any())
            {
                _database.OrderDetails.RemoveRange(orderDetails);
            }

            var variants = _database.ProductVariants.Where(v => v.ProductId == id);
            if (variants.Any())
            {
                _database.ProductVariants.RemoveRange(variants);
            }

            var images = _database.ProductImages.Where(i => i.ProductId == id);
            if (images.Any())
            {
                _database.ProductImages.RemoveRange(images);
            }

            var userCarts = _database.UserCarts.Where(c => c.Variant.ProductId == id);
            if (userCarts.Any())
            {
                _database.UserCarts.RemoveRange(userCarts);
            }

            _database.Products.Remove(product);
            _database.SaveChanges();

            return RedirectToAction("ProductList");
        }

        // SỬA SẢN PHẨM (GET)
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            Product product = _database.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            var categories = _database.Categories.ToList();
            var images = _database.ProductImages.Where(img => img.ProductId == id).ToList();
            var variants = _database.ProductVariants.Where(v => v.ProductId == id).ToList();

            var viewModel = new ProductEditViewModel
            {
                Product = product,
                Images = images,
                Variants = variants,
                // Chú ý: Cột lưu trữ ID category cũ của bạn tên là Category
                CategoryList = new SelectList(categories, "Idcate", "NameCate", product.Category)
            };

            return View(viewModel);
        }

        // (POST) Xử lý khi nhấn "Lưu Thay Đổi"
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ProductEditViewModel viewModel)
        {
            var productFromForm = viewModel.Product;

            if (ModelState.IsValid)
            {
                var productInDb = _database.Products.Find(productFromForm.ProductId);
                if (productInDb == null)
                {
                    return NotFound();
                }

                productInDb.NamePro = productFromForm.NamePro;
                productInDb.DecriptionPro = productFromForm.DecriptionPro;
                productInDb.Material = productFromForm.Material;
                productInDb.Price = productFromForm.Price;
                productInDb.Category = productFromForm.Category;

                _database.SaveChanges();
                return RedirectToAction("ProductList");
            }

            viewModel.Images = _database.ProductImages.Where(img => img.ProductId == productFromForm.ProductId).ToList();
            viewModel.Variants = _database.ProductVariants.Where(v => v.ProductId == productFromForm.ProductId).ToList();
            viewModel.CategoryList = new SelectList(_database.Categories.ToList(), "Idcate", "NameCate", productFromForm.Category);

            return View(viewModel);
        }

        // XEM ĐƠN HÀNG (GET)
        public ActionResult Orders(string statusFilter)
        {
            IQueryable<OrderPro> ordersQuery = _database.OrderPros
                .Include(o => o.IdcusNavigation)
                .Include(o => o.OrderDetails);

            if (statusFilter == "Hoàn tất")
            {
                ViewBag.CurrentFilter = "Hoàn tất";
                ordersQuery = ordersQuery.Where(o => o.Status == "Hoàn tất");
            }
            else if (statusFilter == "Đang xử lý" || statusFilter == "Đang giao")
            {
                ViewBag.CurrentFilter = statusFilter;
                ordersQuery = ordersQuery.Where(o => o.Status == statusFilter);
            }
            else
            {
                ViewBag.CurrentFilter = "Tất cả";
                ordersQuery = ordersQuery.Where(o => o.Status != "Hoàn tất");
            }

            var orders = ordersQuery.OrderByDescending(o => o.DateOrder).ToList();
            return View(orders);
        }

        // CẬP NHẬT TRẠNG THÁI NHANH QUA AJAX (POST)
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status)
        {
            var dbOrder = await _database.OrderPros.FindAsync(id);
            if (dbOrder == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
            }

            dbOrder.Status = status;
            await _database.SaveChangesAsync();

            // Gửi email thông báo cập nhật trạng thái đơn hàng tới khách hàng (kể cả khách vãng lai)
            try
            {
                var customer = await _database.Customers.FirstOrDefaultAsync(c => c.Idcus == dbOrder.Idcus);
                if (customer != null && !string.IsNullOrEmpty(customer.EmailCus))
                {
                    string subject = $"[Rosaria] Cập nhật trạng thái đơn hàng #{dbOrder.Id}";
                    
                    string logoHtml = "";
                    try
                    {
                        string logoPath = System.IO.Path.Combine(_env.WebRootPath, "images", "rosa4.png");
                        if (System.IO.File.Exists(logoPath))
                        {
                            byte[] logoBytes = System.IO.File.ReadAllBytes(logoPath);
                            string logoBase64 = Convert.ToBase64String(logoBytes);
                            logoHtml = $"<img src='data:image/png;base64,{logoBase64}' alt='Rosaria Logo' style='max-height: 45px; width: auto; margin-bottom: 8px; filter: brightness(0) invert(1); display: inline-block;' /><br/>";
                        }
                    }
                    catch { }

                    string emailBody = $@"
                        <div style='font-family: ""Helvetica Neue"", Helvetica, Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 0; background-color: #ffffff; border: 1px solid #111111;'>
                            <!-- BANNER ĐẦU TRANG - TÔNG ĐEN SANG TRỌNG CÓ LOGO NAVBAR -->
                            <div style='background-color: #000000; padding: 25px 20px; text-align: center;'>
                                {logoHtml}
                                <h1 style='color: #ffffff; margin: 0; font-size: 24px; font-weight: 700; letter-spacing: 4px; text-transform: uppercase;'>ROSARIA</h1>
                                <p style='color: #888888; margin: 4px 0 0 0; font-size: 10px; letter-spacing: 2px; text-transform: uppercase;'>Jewelry & Accessories</p>
                            </div>
                            
                            <!-- NỘI DUNG CHÍNH -->
                            <div style='padding: 30px 25px;'>
                                <h2 style='color: #111111; margin-top: 0; font-size: 18px; font-weight: 600; letter-spacing: 0.5px;'>CẬP NHẬT ĐƠN HÀNG #{dbOrder.Id}</h2>
                                <p style='color: #444444; font-size: 14px; line-height: 1.6;'>Kính chào <strong>{customer.NameCus}</strong>,</p>
                                <p style='color: #444444; font-size: 14px; line-height: 1.6;'>Trạng thái đơn hàng <strong>#{dbOrder.Id}</strong> của bạn đã được cập nhật mới nhất:</p>
                                
                                <div style='margin: 25px 0; padding: 18px; background-color: #000000; color: #ffffff; text-align: center; font-size: 15px; font-weight: 700; letter-spacing: 2px; text-transform: uppercase;'>
                                    TRẠNG THÁI: {status.ToUpper()}
                                </div>
                                
                                <p style='color: #666666; font-size: 13px; line-height: 1.5;'>Cảm ơn bạn đã luôn tin tưởng và lựa chọn sản phẩm từ Rosaria.</p>
                            </div>
                            
                            <!-- FOOTER -->
                            <div style='background-color: #f4f4f4; padding: 20px; text-align: center; border-top: 1px solid #e5e5e5; font-size: 12px; color: #777777;'>
                                <p style='margin: 0 0 4px 0; font-weight: 600; color: #111111; letter-spacing: 1px;'>ROSARIA JEWELRY</p>
                                <p style='margin: 0;'>Email này được gửi tự động từ hệ thống.</p>
                            </div>
                        </div>";

                    _ = Task.Run(() => _emailSender.SendEmailAsync(customer.EmailCus, subject, emailBody));
                }
            }
            catch
            {
                // Bỏ qua lỗi gửi mail để tránh ảnh hưởng phản hồi AJAX
            }

            return Json(new { success = true, newStatus = status });
        }

        // CẬP NHẬT TỒN KHO NHANH QUA AJAX (POST)
        [HttpPost]
        public async Task<IActionResult> QuickUpdateStock(int variantId, int newStock)
        {
            var variant = await _database.ProductVariants.FindAsync(variantId);
            if (variant == null)
            {
                return Json(new { success = false, message = "Không tìm thấy phiên bản sản phẩm!" });
            }

            variant.Stock = newStock;
            await _database.SaveChangesAsync();

            return Json(new { success = true, variantId = variantId, newStock = newStock });
        }

        // LẤY DANH SÁCH CÁC SIZE CỦA SẢN PHẨM QUA AJAX (GET)
        [HttpGet]
        public async Task<IActionResult> GetProductVariants(int productId)
        {
            var product = await _database.Products
                .Include(p => p.ProductVariants)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm!" });
            }

            var variants = product.ProductVariants.OrderBy(v => v.VariantId).Select(v => new
            {
                variantId = v.VariantId,
                variantName = v.VariantName,
                stock = v.Stock ?? 0
            }).ToList();

            return Json(new
            {
                success = true,
                productId = product.ProductId,
                productName = product.NamePro,
                variants = variants
            });
        }

        // CỘNG/TRỪ THÊM SỐ LƯỢNG CHO TỪNG SIZE QUA AJAX (POST)
        [HttpPost]
        public async Task<IActionResult> AddVariantStock(int variantId, int delta)
        {
            var variant = await _database.ProductVariants.FindAsync(variantId);
            if (variant == null)
            {
                return Json(new { success = false, message = "Không tìm thấy size sản phẩm!" });
            }

            int current = variant.Stock ?? 0;
            int updated = Math.Max(0, current + delta);
            variant.Stock = updated;
            await _database.SaveChangesAsync();

            return Json(new
            {
                success = true,
                variantId = variant.VariantId,
                newStock = updated,
                delta = delta
            });
        }

        // SỬA ĐƠN HÀNG (GET)
        public ActionResult EditOrder(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            OrderPro order = _database.OrderPros.Find(id);
            if (order == null)
            {
                return NotFound();
            }

            List<string> statusList = new List<string> { "Đang xử lý", "Đang giao", "Hoàn tất" };
            ViewBag.StatusList = new SelectList(statusList, order.Status);

            return View(order);
        }

        // SỬA ĐƠN HÀNG (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditOrder(int id, string Status)
        {
            var dbOrder = _database.OrderPros.Find(id);
            if (dbOrder == null)
            {
                return NotFound();
            }

            dbOrder.Status = Status;
            _database.SaveChanges();

            return RedirectToAction("Orders");
        }

        // LẤY USER (GET)
        public ActionResult Users()
        {
            var users = _database.Users.ToList();
            return View(users);
        }

        // SỬA USER (GET)
        public ActionResult EditUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            var user = _database.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // SỬA USER (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditUser(string id, string? Name, string? LastName, string? PhoneNumber, string? Address)
        {
            var dbUser = _database.Users.FirstOrDefault(u => u.Id == id);
            if (dbUser == null)
            {
                return NotFound();
            }

            dbUser.Name = Name;
            dbUser.LastName = LastName;
            dbUser.PhoneNumber = PhoneNumber;
            dbUser.Address = Address;

            _database.SaveChanges();

            return RedirectToAction("Users");
        }

        // XÓA USER (GET / POST)
        public ActionResult DeleteUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            var user = _database.Users.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                if (user.Email != "admin@rosaria.com") // Bảo vệ tài khoản admin chính
                {
                    _database.Users.Remove(user);
                    _database.SaveChanges();
                }
            }

            return RedirectToAction("Users");
        }

        // LẤY CHI TIẾT ĐƠN HÀNG(GET)
        public ActionResult OrderDetails(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            OrderPro order = _database.OrderPros
                .Include(o => o.IdcusNavigation)
                .Include(o => o.OrderDetails)
                    .ThenInclude(detail => detail.IdproductNavigation)
                .Include(o => o.OrderDetails)
                    .ThenInclude(detail => detail.Variant)
                .FirstOrDefault(o => o.Id == id); // Đã sửa ID thành Id

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // THÊM SIZE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddVariant(int ProductId, string VariantName, int Stock)
        {
            ProductVariant newVariant = new ProductVariant
            {
                ProductId = ProductId,
                VariantName = VariantName,
                Stock = Stock
            };

            _database.ProductVariants.Add(newVariant);
            _database.SaveChanges();

            return RedirectToAction("Edit", new { id = ProductId });
        }

        // XÓA SIZE (POST)
        [HttpPost]
        public ActionResult DeleteVariant(int VariantId)
        {
            var variant = _database.ProductVariants.Find(VariantId);
            if (variant != null)
            {
                int ProductId = variant.ProductId.Value;

                _database.ProductVariants.Remove(variant);
                _database.SaveChanges();

                return RedirectToAction("Edit", new { id = ProductId });
            }
            return RedirectToAction("Index");
        }

        // HÀM 9: (POST) Xóa 1 Ảnh (Image)
        [HttpPost]
        public ActionResult DeleteImage(int ImageId)
        {
            var image = _database.ProductImages.Find(ImageId);
            if (image != null)
            {
                int ProductId = image.ProductId.Value;

                _database.ProductImages.Remove(image);
                _database.SaveChanges();

                return RedirectToAction("Edit", new { id = ProductId });
            }
            return RedirectToAction("Index");
        }
    }
}