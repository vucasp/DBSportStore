using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DBSportStore.Models;

namespace DBSportStore.Controllers
{
    public class ShoppingCartController : Controller
    {
        // Kết nối CSDL để đọc Product.
        private DBSportStoreEntities db =
            new DBSportStoreEntities();

        // Lấy Cart hiện tại từ Session.
        // Nếu chưa có thì tạo Cart mới.
        private Cart GetCart()
        {
            var cart = Session["Cart"] as Cart;

            if (cart == null)
            {
                cart = new Cart();
                Session["Cart"] = cart;
            }

            return cart;
        }

        // Hiển thị giỏ - phiên bản cơ bản để test AddToCart.
        public ActionResult ShowCart()
        {
            return View(GetCart());
        }

        // Nhận ProductID từ nút Mua.
        public ActionResult AddToCart(int id)
        {
            // Chỉ cho thêm Product đang tồn tại và đang bán.
            var product = db.Products
            .FirstOrDefault(p => p.ProductID == id && p.IsActive);

            if (product == null || product.StockQuantity <= 0)
            {
                TempData["CartError"] = "Sản phẩm đã hết hàng hoặc không tồn tại.";
                return RedirectToAction("Index", "Store");
            }

            // Cart.Add tự kiểm tra tồn kho.
            bool added = GetCart().Add(product);

            if (!added)
            {
                TempData["CartError"] =
                    "Không thể thêm sản phẩm. "
                    + "Số lượng đã đạt giới hạn tồn kho.";
            }

            // Sau khi thêm, mở giỏ để người dùng nhìn thấy kết quả.
            return RedirectToAction("ShowCart");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }


        // GET: ShoppingCart
        public ActionResult Index()
        {
            return View();
        }
        // ==================== CẬP NHẬT SỐ LƯỢNG SẢN PHẨM ====================

        // Action này chỉ nhận request theo phương thức POST.
        // Dùng POST vì thao tác này làm thay đổi dữ liệu trong giỏ hàng.
        [HttpPost]

        // Kiểm tra Anti-Forgery Token gửi từ View.
        // Giúp hạn chế request giả mạo từ bên ngoài.
        [ValidateAntiForgeryToken]
        public ActionResult UpdateQuantity(
            int productId,   // Mã sản phẩm cần cập nhật
            int quantity)    // Số lượng mới người dùng nhập
        {
            // Đọc lại Product từ CSDL theo ProductID.
            // Mục đích: lấy StockQuantity mới nhất trong database,
            // không chỉ dựa vào số tồn kho đã lưu trong Session.
            var product = db.Products.Find(productId);


            // Nếu Product không còn tồn tại trong CSDL
            // thì không thể cập nhật giỏ hàng.
            if (product == null)
            {
                // TempData dùng để gửi thông báo sang request kế tiếp.
                TempData["CartError"] =
                    "Không tìm thấy sản phẩm.";

                // Quay lại trang Giỏ hàng.
                return RedirectToAction("ShowCart");
            }


            // Lấy Cart hiện tại từ Session rồi gọi hàm Update().
            // Update() sẽ kiểm tra:
            // - quantity phải > 0
            // - quantity không được vượt quá tồn kho hiện tại.
            bool updated = GetCart().Update(
                productId,
                quantity,
                product.StockQuantity);


            // Nếu Update() trả về false
            // nghĩa là số lượng người dùng nhập không hợp lệ.
            if (!updated)
            {
                TempData["CartError"] =
                    "Số lượng phải lớn hơn 0 "
                    + "và không vượt tồn kho.";
            }


            // Sau khi cập nhật xong,
            // quay lại trang Giỏ hàng để hiển thị kết quả mới.
            return RedirectToAction("ShowCart");
        }



        // ==================== XÓA SẢN PHẨM KHỎI GIỎ ====================

        // Dùng POST vì thao tác xóa làm thay đổi dữ liệu Cart.
        [HttpPost]

        // Kiểm tra Anti-Forgery Token từ form trong View.
        [ValidateAntiForgeryToken]
        public ActionResult Remove(
            int productId)   // Mã Product cần xóa
        {
            // Lấy Cart hiện tại từ Session
            // và xóa CartItem có ProductID tương ứng.
            GetCart().Remove(productId);

            // Quay lại trang Giỏ hàng sau khi xóa.
            return RedirectToAction("ShowCart");
        }
        // ==================== HIỂN THỊ THÔNG TIN GIỎ TRÊN NAVBAR ====================

        // Action này chỉ được gọi từ View/Layout,
        // không cho người dùng truy cập trực tiếp bằng URL.
        [ChildActionOnly]
        public PartialViewResult BagCart()
        {
            // Lấy Cart hiện tại từ Session.
            // Nếu chưa có Cart thì biến cart sẽ bằng null.
            var cart = Session["Cart"] as Cart;


            // Nếu chưa có Cart -> số lượng = 0.
            // Nếu có Cart -> tính tổng số lượng sản phẩm.
            ViewBag.CartQty =
                cart == null ? 0 : cart.TotalQuantity();


            // Nếu chưa có Cart -> tổng tiền = 0.
            // Nếu có Cart -> tính tổng tiền của giỏ.
            ViewBag.CartTotal =
                cart == null ? 0 : cart.TotalMoney();


            // Gửi dữ liệu sang Partial View BagCart.cshtml.
            return PartialView();
        }
        // ==================== MỞ FORM CHECKOUT ====================

        // GET: hiển thị trang Checkout cho người dùng nhập thông tin.
        public ActionResult Checkout()
        {
            // Lấy Cart hiện tại đang được lưu trong Session.
            var cart = GetCart();

            // Any() kiểm tra giỏ có ít nhất một sản phẩm hay không.
            // Dấu ! nghĩa là phủ định -> giỏ KHÔNG có sản phẩm.
            if (!cart.Items.Any())
            {
                // Giỏ trống thì quay lại trang ShowCart.
                return RedirectToAction("ShowCart");
            }

            // Tạo ViewModel dùng cho trang Checkout.
            var model = new CheckoutViewModel
            {
                // Gắn Cart vào ViewModel để View hiển thị
                // danh sách sản phẩm và tổng tiền.
                Cart = cart
            };

            // Gửi ViewModel sang Checkout.cshtml.
            return View(model);
        }


        // ==================== NHẬN DỮ LIỆU CHECKOUT VÀ LƯU ĐƠN HÀNG ====================

        // Action này nhận dữ liệu từ form Checkout bằng phương thức POST.
        [HttpPost]

        // Kiểm tra Anti-Forgery Token gửi từ View.
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(
            CheckoutViewModel model)
        {
            // Lấy Cart hiện tại đang lưu trong Session.
            var cart = GetCart();

            // Cart không được gửi từ form lên,
            // nên phải gắn lại vào ViewModel để View vẫn hiển thị tóm tắt đơn hàng.
            model.Cart = cart;


            // ==================== KIỂM TRA GIỎ HÀNG ====================

            // Nếu giỏ không còn sản phẩm thì không cho đặt hàng.
            // Any() kiểm tra danh sách có ít nhất một phần tử hay không.
            if (!cart.Items.Any())
            {
                return RedirectToAction("ShowCart");
            }


            // ==================== KIỂM TRA DỮ LIỆU KHÁCH HÀNG ====================

            // ModelState.IsValid = true khi dữ liệu người dùng nhập
            // thỏa các Validation trong CheckoutViewModel.
            if (!ModelState.IsValid)
            {
                // Nếu dữ liệu chưa hợp lệ thì trả lại form
                // để hiển thị thông báo lỗi.
                return View(model);
            }


            // ==================== KIỂM TRA LẠI TỒN KHO ====================

            // Phải kiểm tra lại ngay trước khi tạo đơn
            // vì tồn kho có thể đã thay đổi sau lúc Product được thêm vào Cart.
            foreach (var item in cart.Items)
            {
                // Đọc Product mới nhất từ CSDL theo ProductID.
                var product = db.Products.Find(item.ProductID);

                // Không cho đặt nếu:
                // - Product không còn tồn tại
                // - Product đã ngừng bán
                // - tồn kho hiện tại nhỏ hơn số lượng khách đặt
                if (product == null
                    || !product.IsActive
                    || product.StockQuantity < item.Quantity)
                {
                    // Thêm lỗi chung vào ModelState để hiển thị trên Checkout View.
                    ModelState.AddModelError(
                        "",
                        "Sản phẩm " + item.ProductName
                        + " không còn đủ tồn kho.");

                    return View(model);
                }
            }


            // ==================== BẮT ĐẦU TRANSACTION ====================

            // Transaction bảo đảm các thao tác:
            // Customer + OrderPro + OrderDetail + cập nhật tồn kho
            // phải cùng thành công.
            // Nếu một bước lỗi thì toàn bộ thay đổi sẽ rollback.
            using (var transaction =
                db.Database.BeginTransaction())
            {
                try
                {
                    // =====================================================
                    // 1. TẠO CUSTOMER MỚI CHO KHÁCH VÃNG LAI
                    // =====================================================

                    // B07 chưa có Login.
                    // Mỗi lần Guest Checkout sẽ tạo một Customer mới.
                    var customer = new Customer
                    {
                        // UserID = null nghĩa là Customer chưa có tài khoản.
                        UserID = null,

                        FullName = model.FullName,
                        Phone = model.Phone,
                        Email = model.Email,
                        Address = model.Address
                    };

                    // Đánh dấu Customer mới cần được thêm vào CSDL.
                    db.Customers.Add(customer);

                    // Ghi Customer xuống CSDL.
                    db.SaveChanges();

                    // Sau SaveChanges(), SQL Server đã sinh CustomerID
                    // và EF tự cập nhật giá trị vào customer.CustomerID.


                    // =====================================================
                    // 2. TẠO ORDERPRO
                    // =====================================================

                    var order = new OrderPro
                    {
                        // Ghi nhận thời điểm đặt hàng.
                        OrderDate = System.DateTime.Now,

                        // Gắn đơn hàng với Customer vừa được tạo.
                        CustomerID = customer.CustomerID,

                        // Lưu snapshot thông tin giao hàng trên chính đơn hàng.
                        // Sau này Customer thay đổi thông tin,
                        // đơn cũ vẫn giữ đúng dữ liệu lúc đặt.
                        RecipientName = model.FullName,
                        ShippingPhone = model.Phone,
                        ShippingAddress = model.ShippingAddress,

                        // Tổng tiền lấy từ Cart hiện tại.
                        TotalAmount = cart.TotalMoney(),

                        // Trạng thái ban đầu của đơn.
                        Status = "Chờ xử lý"
                    };

                    // Đánh dấu OrderPro mới cần thêm vào CSDL.
                    db.OrderProes.Add(order);

                    // Ghi OrderPro xuống CSDL.
                    db.SaveChanges();

                    // Sau SaveChanges(), SQL Server đã sinh OrderID
                    // và EF cập nhật vào order.OrderID.


                    // =====================================================
                    // 3. TẠO CÁC ORDERDETAIL
                    // =====================================================

                    // Mỗi CartItem sẽ tạo thành một dòng OrderDetail.
                    foreach (var item in cart.Items)
                    {
                        // Đọc lại Product để cập nhật tồn kho.
                        var product = db.Products.Find(
                            item.ProductID);


                        // Tạo chi tiết đơn hàng cho Product hiện tại.
                        var detail = new OrderDetail
                        {
                            // Gắn chi tiết vào OrderPro vừa tạo.
                            OrderID = order.OrderID,

                            ProductID = item.ProductID,

                            // Số lượng khách đặt.
                            Quantity = item.Quantity,

                            // Lưu đơn giá tại thời điểm đặt hàng.
                            // Không lấy lại Product.Price sau này.
                            UnitPrice = item.UnitPrice
                        };


                        // Thêm OrderDetail vào CSDL.
                        db.OrderDetails.Add(detail);


                        // Trừ tồn kho theo số lượng vừa đặt.
                        product.StockQuantity -= item.Quantity;
                    }


                    // Ghi toàn bộ OrderDetail và tồn kho mới xuống CSDL.
                    db.SaveChanges();


                    // ==================== COMMIT TRANSACTION ====================

                    // Tất cả thao tác phía trên đều thành công
                    // thì xác nhận lưu toàn bộ thay đổi.
                    transaction.Commit();


                    // ==================== XÓA GIỎ HÀNG ====================

                    // Chỉ xóa Cart SAU KHI transaction đã commit thành công.
                    // Nếu lưu đơn bị lỗi thì giỏ vẫn còn để khách thử lại.
                    cart.Clear();


                    // ==================== CHUYỂN SANG TRANG THÀNH CÔNG ====================

                    // Chuyển sang OrderSuccess và gửi OrderID vừa tạo.
                    return RedirectToAction(
                        "OrderSuccess",
                        new { id = order.OrderID });
                }
                catch
                {
                    // ==================== XỬ LÝ KHI CÓ LỖI ====================

                    // Nếu có lỗi trong quá trình lưu,
                    // hủy toàn bộ thay đổi của transaction.
                    transaction.Rollback();


                    // Thêm thông báo lỗi để hiển thị lại trên Checkout View.
                    ModelState.AddModelError(
                        "",
                        "Có lỗi khi lưu đơn hàng. "
                        + "Vui lòng thử lại.");

                    // Giữ nguyên Cart và dữ liệu người dùng đã nhập.
                    return View(model);
                }
            }
        }
        // ==================== HIỂN THỊ TRANG ĐẶT HÀNG THÀNH CÔNG ====================

        // Nhận OrderID từ URL sau khi Checkout thành công.
        public ActionResult OrderSuccess(int id)
        {
            // Tìm OrderPro trong CSDL theo OrderID.
            var order = db.OrderProes.Find(id);

            // Nếu không tìm thấy đơn hàng tương ứng
            // thì trả về lỗi 404 - Not Found.
            if (order == null)
            {
                return HttpNotFound();
            }

            // Gửi OrderPro sang View OrderSuccess.cshtml.
            // Ở chức năng 6 chỉ cần hiển thị thông tin cơ bản
            // để xác nhận đơn hàng đã được tạo thành công.
            return View(order);
        }

        // ==================== XÓA TOÀN BỘ GIỎ HÀNG ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ClearCart()
        {
            // Lấy giỏ hàng hiện tại và làm trống danh sách
            GetCart().Clear();
            TempData["CartError"] = "Đã xóa toàn bộ giỏ hàng.";
            return RedirectToAction("ShowCart");
        }
    }
}