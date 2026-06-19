using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using WebTrangSucNangCao.Models;

namespace WebTrangSucNangCao.Controllers
{
    public class HomeController : Controller
    {
        /// <summary>
        ///  **Dependency Injection**
        /// </summary>
        // 1. Khai báo biến database
        private readonly ShopOnlineContext database;

        // 2. Tạo Constructor để tiêm ShopOnlineContext vào
        public HomeController(ShopOnlineContext context)
        {
            database = context;
        }

        // HÀM 1: Trang chủ
        // Sửa lại hàm trangchu để nhận tham số
        // Thêm tham số int? page vào hàm
        public ActionResult Index(string priceRange, string sortOrder, int? page)
        {
            // 1. CẤU HÌNH PHÂN TRANG
            int pageSize = 12; // 12 sản phẩm mỗi trang
            int pageNumber = (page ?? 1); // Nếu null thì mặc định là trang 1

            // 2. Query dữ liệu (Giữ nguyên logic lọc cũ của bạn)
            var productsQuery = database.Products.AsQueryable();

            // --- (ĐOẠN CODE LỌC GIÁ GIỮ NGUYÊN) ---
            if (!string.IsNullOrEmpty(priceRange))
            {
                switch (priceRange)
                {
                    case "1": productsQuery = productsQuery.Where(p => p.Price < 500000); break;
                    case "2": productsQuery = productsQuery.Where(p => p.Price >= 500000 && p.Price < 1000000); break;
                    case "3": productsQuery = productsQuery.Where(p => p.Price >= 1000000 && p.Price < 1500000); break;
                    case "4": productsQuery = productsQuery.Where(p => p.Price >= 1500000 && p.Price < 2000000); break;
                    case "5": productsQuery = productsQuery.Where(p => p.Price >= 2000000); break;
                }
            }

            // --- (ĐOẠN CODE SẮP XẾP GIỮ NGUYÊN) ---
            switch (sortOrder)
            {
                case "price_asc": productsQuery = productsQuery.OrderBy(p => p.Price); break;
                case "price_desc": productsQuery = productsQuery.OrderByDescending(p => p.Price); break;
                default: productsQuery = productsQuery.OrderByDescending(p => p.ProductId); break;
            }

            // 3. TÍNH TOÁN PHÂN TRANG (MỚI)
            int totalItems = productsQuery.Count(); // Tổng số sản phẩm sau khi lọc
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize); // Tổng số trang

            // Cắt dữ liệu: Bỏ qua (Trang hiện tại - 1) * 12, Lấy 12 cái
            var pagedProducts = productsQuery.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            // 4. Lấy ảnh Hover (Giữ nguyên logic cũ nhưng áp dụng cho pagedProducts)
            var ProductIds = pagedProducts.Select(p => p.ProductId).ToList();
            var allImages = database.ProductImages.Where(img => ProductIds.Contains(img.ProductId ?? 0)).ToList();

            var productViewModels = pagedProducts.Select(p => {
                var hoverImg = allImages.FirstOrDefault(img => img.ProductId == p.ProductId && img.ImagePath != p.ImagePro);
                return new ProductHomeViewModel { Product = p, HoverImage = hoverImg?.ImagePath };
            }).ToList();

            // 5. Gửi dữ liệu ra View
            var viewModel = new HomeViewModel
            {
                Sliders = database.Sliders.ToList(),
                Products = productViewModels
            };

            // Gửi thêm thông tin phân trang để View vẽ nút
            ViewBag.CurrentPrice = priceRange;
            ViewBag.CurrentSort = sortOrder;
            ViewBag.PageNumber = pageNumber;
            ViewBag.TotalPages = totalPages;

            return View(viewModel);
        }

        // HÀM 2: Trang Nhẫn (RINGS)
        public ActionResult Rings()
        {
            var products = GetProductsWithHover("C03"); // Lọc theo ID Nhẫn
            return View(products);
        }

        // HÀM 3: Trang Vòng Tay (BRACELETS)
        public ActionResult Bracelets()
        {
            var products = GetProductsWithHover("C01"); // Lọc theo ID Vòng tay
            return View(products);
        }

        // HÀM 4: Trang Vòng Cổ (NECKLACES)
        public ActionResult Necklaces()
        {
            var products = GetProductsWithHover("C04"); // Lọc theo ID Vòng cổ
            return View(products);
        }

        // HÀM 5: Trang Bông Tai (EARRINGS)
        public ActionResult Earrings()
        {
            var products = GetProductsWithHover("C02"); // Lọc theo ID Bông tai
            return View(products);
        }

        public ActionResult Search(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return RedirectToAction("Index");
            }
            ViewBag.SearchTerm = searchTerm;

            // 1. Tìm sản phẩm trước
            var products = database.Products
                .Where(p => p.NamePro.Contains(searchTerm) ||
                            p.DecriptionPro.Contains(searchTerm))
                .ToList();

            // 2. Gọi hàm lấy ảnh Hover cho danh sách vừa tìm được
            var viewModelList = GetProductsWithHover(products);

            return View("Search", viewModelList);
        }

        //hàm hover xài cho search
        private List<ProductHomeViewModel> GetProductsWithHover(List<Product> products)
        {
            // 1. Nếu danh sách rỗng thì trả về rỗng luôn
            if (products == null || !products.Any())
            {
                return new List<ProductHomeViewModel>();
            }

            // 2. Lấy TẤT CẢ ProductId từ list
            var ProductIds = products.Select(p => p.ProductId).ToList();

            // 3. Lấy TẤT CẢ ảnh gallery của đống sản phẩm đó (Query 1 lần cho nhanh)
            var allImages = database.ProductImages
                            .Where(img => ProductIds.Contains(img.ProductId ?? 0))
                            .OrderBy(img => img.ImageId)
                            .ToList();

            // 4. Ghép ảnh vào từng sản phẩm
            var viewModelList = new List<ProductHomeViewModel>();
            foreach (var p in products)
            {
                // Tìm ảnh thứ 2 của sản phẩm 'p'
                var hoverImg = allImages
                                .Where(img => img.ProductId == p.ProductId)
                                .Skip(1) // Bỏ qua ảnh đầu tiên (ảnh bìa)
                                .FirstOrDefault(); // Lấy ảnh thứ 2

                viewModelList.Add(new ProductHomeViewModel
                {
                    Product = p,
                    HoverImage = hoverImg?.ImagePath
                });
            }

            return viewModelList;
        }
        // =============================================
        // ===     HÀM PRIVATE MỚI (LẤY ẢNH HOVER)   ===
        // =============================================
        private List<ProductHomeViewModel> GetProductsWithHover(string categoryCode)
        {
            // 1. Lấy sản phẩm (Lọc theo Category nếu có)
            IQueryable<Product> productsQuery = database.Products;
            if (!string.IsNullOrEmpty(categoryCode))
            {
                productsQuery = productsQuery.Where(p => p.Category.Trim() == categoryCode);
            }
            var products = productsQuery.ToList();

            // 2. Lấy TẤT CẢ ProductId từ list trên
            var ProductIds = products.Select(p => p.ProductId).ToList();

            // 3. Lấy TẤT CẢ ảnh gallery của MỘT ĐỐNG sản phẩm đó
            // (Đây là cách lấy 1 lần, nhanh hơn N+1)
            var allImages = database.ProductImages
                            .Where(img => ProductIds.Contains(img.ProductId ?? 0))
                            .OrderBy(img => img.ImageId)
                            .ToList();

            // 4. Tạo "cái hộp"
            var viewModelList = new List<ProductHomeViewModel>();
            foreach (var p in products)
            {
                // Tìm ảnh thứ 2 của sản phẩm 'p'
                var hoverImg = allImages
                                .Where(img => img.ProductId == p.ProductId)
                                .Skip(1) // Bỏ qua ảnh đầu tiên (ảnh bìa)
                                .FirstOrDefault(); // Lấy ảnh thứ 2

                viewModelList.Add(new ProductHomeViewModel
                {
                    Product = p,
                    // Nếu 'hoverImg' là null (vì sp chỉ có 1 ảnh)
                    // thì HoverImage sẽ là null
                    HoverImage = hoverImg?.ImagePath
                });
            }

            return viewModelList;
        }
    }
}