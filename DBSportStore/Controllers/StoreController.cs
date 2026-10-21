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
        public ActionResult Index(
    int? categoryId,
    string keyword,
    decimal? minPrice,
    decimal? maxPrice,
    string sortOrder, // <--- 1. Bắt buộc phải có tham số này để nhận giá trị từ View gửi lên
    int page = 1)
        {
            const int pageSize = 6;
            if (page < 1) { page = 1; }

            // Bắt đầu truy vấn sản phẩm đang bán
            var products = db.Products.Where(p => p.IsActive);

            // Lọc theo danh mục
            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryID == categoryId.Value);
            }

            // Tìm kiếm theo từ khóa
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                products = products.Where(p => p.ProductName.Contains(keyword) || p.Description.Contains(keyword));
            }

            // Lọc theo giá (từ)
            if (minPrice.HasValue)
            {
                products = products.Where(p => p.Price >= minPrice.Value);
            }

            // Lọc theo giá (đến)
            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.Price <= maxPrice.Value);
            }

            // ==========================================
            // 2. XỬ LÝ SẮP XẾP THEO GIÁ (PHẢI ĐẶT TRƯỚC Count, Skip, Take)
            // ==========================================
            if (sortOrder == "desc")
            {
                products = products.OrderByDescending(p => p.Price);
            }
            else
            {
                // Mặc định hoặc khi sortOrder == "asc"
                products = products.OrderBy(p => p.Price);
            }

            // Tính toán phân trang
            int totalItems = products.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            if (totalPages == 0) { totalPages = 1; }
            if (page > totalPages) { page = totalPages; }

            var model = new StoreHomeViewModel
            {
                Categories = db.Categories.OrderBy(c => c.CategoryName).ToList(),

                // Phân trang lấy dữ liệu sau khi đã sắp xếp
                Products = products
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new ProductCardViewModel
                    {
                        ProductID = p.ProductID,
                        NamePro = p.ProductName,
                        CategoryName = p.Category.CategoryName,
                        Price = p.Price,
                        ImagePro = p.ImageUrl
                    })
                    .ToList(),

                SelectedCategoryId = categoryId,
                SearchTerm = keyword,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SortOrder = sortOrder, // <--- 3. Gán lại giá trị để duy trì trạng thái trên giao diện
                TotalItems = totalItems,
                CurrentPage = page,
                TotalPages = totalPages
            };

            return View(model);
        }
        public ActionResult AboutStore()
        {
            return View();
        }

    }
}
