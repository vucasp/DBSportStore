// Dùng ProductCardViewModel trong thư mục Models
using DBSportStore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

// Namespace chứa các Controller
namespace DBSportStore.Controllers
{
    // Controller xử lý các request của trang Store
    public class StoreController : Controller
    {
        private DBSportStoreEntities db = new DBSportStoreEntities();
        // GET: Store
        // Action Index xử lý URL /Store/Index
        public ActionResult Index()
        {
            // Tạo danh sách sản phẩm mẫu
            var products = db.Products
    .Where(p => p.IsActive)
    .Select(p => new ProductCardViewModel
    {
        ProductID = p.ProductID,
        NamePro = p.ProductName,
        CategoryName = p.Category.CategoryName,
        Price = p.Price,
        ImagePro = p.ImageUrl
    })
    .ToList();

            return View(products);

        }
        public ActionResult AboutStore()
        {
            return View();
        }

    }
}
