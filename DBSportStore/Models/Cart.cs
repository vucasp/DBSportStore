using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DBSportStore.Models
{
    // Một dòng sản phẩm trong giỏ hàng.
    public class CartItem
    {
        // Các dữ liệu cần hiển thị trong giỏ.
        // Không lưu trực tiếp object Product của EF vào Session.
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string ImageUrl { get; set; }

        // Giá được ghi nhận khi sản phẩm được thêm vào giỏ.
        public decimal UnitPrice { get; set; }

        // Số lượng khách muốn mua.
        public int Quantity { get; set; }

        // Tồn kho tại thời điểm thêm/cập nhật giỏ.
        public int StockQuantity { get; set; }

        // Thành tiền của một dòng.
        public decimal LineTotal
        {
            get { return UnitPrice * Quantity; }
        }
    }

    // Toàn bộ giỏ hàng.
    public class Cart
    {
        // Danh sách nội bộ của giỏ.
        private readonly List<CartItem> items =
            new List<CartItem>();

        // View chỉ được đọc danh sách, không thay trực tiếp.
        public IEnumerable<CartItem> Items
        {
            get { return items; }
        }

        // Thêm một Product vào giỏ.
        // Trả true nếu thêm/tăng số lượng thành công.
        public bool Add(Product product)
        {
            if (product == null
                || !product.IsActive
                || product.StockQuantity <= 0)
            {
                return false;
            }

            // Tìm xem Product đã có trong giỏ chưa.
            var item = items.FirstOrDefault(
                x => x.ProductID == product.ProductID);

            if (item == null)
            {
                // Chưa có -> tạo dòng mới với Quantity = 1.
                items.Add(new CartItem
                {
                    ProductID = product.ProductID,
                    ProductName = product.ProductName,
                    ImageUrl = product.ImageUrl,
                    UnitPrice = product.Price,
                    Quantity = 1,
                    StockQuantity = product.StockQuantity
                });

                return true;
            }

            // Đã có -> chỉ tăng nếu chưa vượt tồn kho.
            if (item.Quantity < product.StockQuantity)
            {
                item.Quantity++;
                item.StockQuantity = product.StockQuantity;
                return true;
            }

            return false;
        }

        // Cập nhật số lượng một dòng.
        public bool Update(
            int productId,
            int quantity,
            int currentStock)
        {
            var item = items.FirstOrDefault(
                x => x.ProductID == productId);

            if (item == null)
            {
                return false;
            }

            // Không nhận số lượng <= 0 hoặc vượt tồn kho.
            if (quantity <= 0 || quantity > currentStock)
            {
                return false;
            }

            item.Quantity = quantity;
            item.StockQuantity = currentStock;

            return true;
        }

        // Xóa một Product khỏi giỏ.
        public void Remove(int productId)
        {
            items.RemoveAll(
                x => x.ProductID == productId);
        }

        // Tổng số lượng sản phẩm trong giỏ.
        public int TotalQuantity()
        {
            return items.Sum(x => x.Quantity);
        }

        // Tổng tiền của toàn bộ giỏ.
        public decimal TotalMoney()
        {
            return items.Sum(x => x.LineTotal);
        }

        // Xóa toàn bộ giỏ sau khi đặt hàng thành công.
        public void Clear()
        {
            items.Clear();
        }
    }

}
