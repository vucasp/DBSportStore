using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DBSportStore.Models
{
    public class StoreHomeViewModel
    {
        // Danh sách danh mục lấy trực tiếp từ bảng Category.
        public List<Category> Categories { get; set; }

        // Danh sách sản phẩm đang bán để Trang Chủ tiếp tục hiển thị.
        public List<ProductCardViewModel> Products { get; set; }

        // Mã danh mục người dùng đang chọn.
        // int? cho phép null = không lọc danh mục.
        public int? SelectedCategoryId { get; set; }

        // Từ khóa người dùng đang tìm kiếm.
        public string SearchTerm { get; set; }

        // Trang hiện tại.
        public int CurrentPage { get; set; }

        // Tổng số trang.
        public int TotalPages { get; set; }

        // Giá tối thiểu người dùng nhập.
        // decimal? cho phép để trống ô Giá từ.
        public decimal? MinPrice { get; set; }

        // Giá tối đa người dùng nhập.
        // decimal? cho phép để trống ô Giá đến.
        public decimal? MaxPrice { get; set; }

        // Tổng số lượng sản phẩm sau khi lọc/tìm kiếm
        public int TotalItems { get; set; }

        // Kiểu sắp xếp: "asc" (thấp đến cao), "desc" (cao đến thấp)
        public string SortOrder { get; set; }
    }
}
