using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using WebTrangSucNangCao.Models;

namespace WebApplication1.Controllers
{
    public class CategoriesController : Controller
    {
        // 1. Khai báo biến database
        private readonly ShopOnlineContext database;

        // 2. Tạo Constructor để tiêm ShopOnlineContext vào
        public CategoriesController(ShopOnlineContext context)
        {
            database = context;
        }

        // GET: Categories
        public ActionResult Index()
        {
            return View(database.Categories.ToList());
        }
        //get
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(Category cate)
        {
            try
            {
                database.Categories.Add(cate);
                database.SaveChanges();
                return RedirectToAction("Index");
            }
            catch
            {
                return Content("Error Create New");
            }
        }

        //chức năng xem thông tin chi tiết 
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index"); // hoặc return HttpNotFound();
            }

            var category = database.Categories.FirstOrDefault(s => s.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }



    }
}