using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebTrangSucNangCao.Models; // Đổi lại đúng namespace Models của bạn

namespace WebTrangSucNangCao.Controllers // Đổi tên namespace cho khớp project
{
    public class ProductController : Controller
    {
        // 1. Dùng Dependency Injection thay cho "new ShopOnlineEntities()"
        private readonly ShopOnlineContext _database;

        public ProductController(ShopOnlineContext database)
        {
            _database = database;
        }

        // HÀM 1: (GET) Mở trang Chi Tiết Sản Phẩm
        public IActionResult Detail(int? id)
        {
            if (id == null)
            {
                // 2. Đổi HttpStatusCodeResult thành BadRequest() cho gọn
                return BadRequest();
            }

            Product product = _database.Products.Find(id);
            if (product == null)
            {
                // 3. Đổi HttpNotFound() thành NotFound()
                return NotFound();
            }

            var images = _database.ProductImages.Where(img => img.ProductId == id).ToList();
            var variants = _database.ProductVariants.Where(v => v.ProductId == id).ToList();
            var relatedProducts = _database.Products
                .Where(p => p.Category == product.Category && p.ProductId != id)
                .Take(4)
                .ToList();

            var viewModel = new ProductDetailViewModel
            {
                Product = product,
                Images = images,
                Variants = variants,
                RelatedProducts = relatedProducts
            };

            return View(viewModel);
        }
    }
}