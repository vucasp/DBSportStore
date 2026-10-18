USE master;
GO


IF DB_ID(N'DBSportStore') IS NOT NULL
BEGIN
    ALTER DATABASE DBSportStore SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE DBSportStore;
END
GO

CREATE DATABASE DBSportStore;
GO
USE DBSportStore;
GO

/* =========================================================
   1. ROLE / USER ACCOUNT
   ========================================================= */
CREATE TABLE dbo.Role (
    RoleID INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(30) NOT NULL UNIQUE
);

CREATE TABLE dbo.UserAccount (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    -- Mã tài khoản.
    -- IDENTITY(1,1): tự tăng từ 1.
    -- PRIMARY KEY: khóa chính của bảng.

    UserName NVARCHAR(50) NOT NULL UNIQUE,
    -- Tên đăng nhập.
    -- NOT NULL: bắt buộc phải có.
    -- UNIQUE: không cho phép hai tài khoản trùng UserName.

    PasswordHash NVARCHAR(255) NOT NULL,
    -- Lưu mật khẩu sau khi đã được băm (Hash).
    -- Không lưu mật khẩu gốc dạng plain text trong CSDL.
    -- Khi đăng nhập, hệ thống sẽ băm lại mật khẩu người dùng nhập
    -- rồi so sánh với PasswordHash đã lưu.

    PasswordSalt NVARCHAR(100) NOT NULL,
    -- Salt là chuỗi ngẫu nhiên kết hợp với mật khẩu trước khi tạo Hash.
    -- Mỗi tài khoản có Salt riêng.
    -- Nhờ đó, hai người dùng có cùng mật khẩu vẫn có thể tạo ra Hash khác nhau.

    RoleID INT NOT NULL,
    -- Mã quyền của tài khoản, ví dụ: Admin hoặc Customer.
    -- RoleID sẽ liên kết với bảng Role.

    IsActive BIT NOT NULL
        CONSTRAINT DF_UserAccount_IsActive DEFAULT(1),
    -- Trạng thái tài khoản.
    -- 1: đang hoạt động.
    -- 0: bị khóa/ngừng sử dụng.
    -- DEFAULT(1): khi tạo mới, tài khoản mặc định được kích hoạt.

    CreatedAt DATETIME2 NOT NULL
        CONSTRAINT DF_UserAccount_CreatedAt DEFAULT(SYSDATETIME()),
    -- Thời điểm tạo tài khoản.
    -- SYSDATETIME(): tự động lấy ngày giờ hiện tại của SQL Server.

    CONSTRAINT FK_UserAccount_Role
        FOREIGN KEY(RoleID)
        REFERENCES dbo.Role(RoleID)
    -- Khóa ngoại:
    -- UserAccount.RoleID phải tồn tại trong bảng Role.
);

CREATE TABLE dbo.Customer (
    CustomerID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NULL UNIQUE,
    FullName NVARCHAR(100) NOT NULL,
    Phone NVARCHAR(20) NULL,
    Email NVARCHAR(100) NULL UNIQUE,
    Address NVARCHAR(250) NULL,
    CONSTRAINT FK_Customer_UserAccount
        FOREIGN KEY(UserID) REFERENCES dbo.UserAccount(UserID)
);

/* =========================================================
   2. CATEGORY / PRODUCT
   ImageUrl chỉ lưu TÊN FILE trong Content/images
   ========================================================= */
CREATE TABLE dbo.Category (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE dbo.Product (
    ProductID INT IDENTITY(1,1) PRIMARY KEY,
    ProductName NVARCHAR(150) NOT NULL,
    Description NVARCHAR(500) NULL,
    CategoryID INT NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    ImageUrl NVARCHAR(255) NULL,
    StockQuantity INT NOT NULL CONSTRAINT DF_Product_Stock DEFAULT(0),
    IsActive BIT NOT NULL CONSTRAINT DF_Product_IsActive DEFAULT(1),
    CONSTRAINT CK_Product_Price CHECK (Price >= 0),
    CONSTRAINT CK_Product_Stock CHECK (StockQuantity >= 0),
    CONSTRAINT FK_Product_Category
        FOREIGN KEY(CategoryID) REFERENCES dbo.Category(CategoryID)
);

/* =========================================================
   3. ORDER
   ========================================================= */
CREATE TABLE dbo.OrderPro (
    OrderID INT IDENTITY(1,1) PRIMARY KEY,
    OrderDate DATETIME2 NOT NULL CONSTRAINT DF_OrderPro_Date DEFAULT(SYSDATETIME()),
    CustomerID INT NOT NULL,
    ShippingAddress NVARCHAR(250) NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_OrderPro_Total DEFAULT(0),
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_OrderPro_Status DEFAULT(N'Pending'),
    CONSTRAINT FK_OrderPro_Customer
        FOREIGN KEY(CustomerID) REFERENCES dbo.Customer(CustomerID)
);

CREATE TABLE dbo.OrderDetail (
    OrderDetailID INT IDENTITY(1,1) PRIMARY KEY,
    OrderID INT NOT NULL,
    ProductID INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    CONSTRAINT CK_OrderDetail_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_OrderDetail_UnitPrice CHECK (UnitPrice >= 0),
    CONSTRAINT FK_OrderDetail_OrderPro
        FOREIGN KEY(OrderID) REFERENCES dbo.OrderPro(OrderID),
    CONSTRAINT FK_OrderDetail_Product
        FOREIGN KEY(ProductID) REFERENCES dbo.Product(ProductID)
);
GO

/* =========================================================
   4. SEED ROLE / ACCOUNT
   Password dùng cho LAB:
   Admin      / Admin@123
   Customer01 / Customer@123
   Customer02 / Customer2@123

   Trong DB chỉ lưu PBKDF2-SHA256 hash + salt.
   ========================================================= */
INSERT INTO dbo.Role(RoleName)
VALUES (N'Admin'), (N'Customer');

INSERT INTO dbo.UserAccount
    (UserName, PasswordHash, PasswordSalt, RoleID, IsActive)
VALUES
    (N'Admin', N'jXI04eT1JoCJm3XNggxB83cbmILvimcXN/OhnXTpjMg=', N'ABEiM0RVZneImaq7zN3u/w==',
        (SELECT RoleID FROM dbo.Role WHERE RoleName=N'Admin'), 1),
    (N'Customer01', N'4Sh6UXRWvHaxONWKOsoy3FqmG1mhyEREAztER7fUw1c=', N'ECEyQ1RldoeYqbrL3O3+Dw==',
        (SELECT RoleID FROM dbo.Role WHERE RoleName=N'Customer'), 1),
    (N'Customer02', N'Ps5M+lKQdyqTHO9jfJflolkULgK+fJYomTOXB4QBOSg=', N'/+7dzLuqmYh3ZlVEMyIRAA==',
        (SELECT RoleID FROM dbo.Role WHERE RoleName=N'Customer'), 1);

INSERT INTO dbo.Customer(UserID, FullName, Phone, Email, Address)
VALUES
    ((SELECT UserID FROM dbo.UserAccount WHERE UserName=N'Customer01'),
     N'Nguyễn Minh Anh', N'0909123456', N'minhanh01@gmail.com',
     N'Quận 10, TP.HCM'),
    ((SELECT UserID FROM dbo.UserAccount WHERE UserName=N'Customer02'),
     N'Trần Gia Hân', N'0918234567', N'giahan02@gmail.com',
     N'Quận 3, TP.HCM');

/* =========================================================
   5. SEED CATEGORY
   ========================================================= */
INSERT INTO dbo.Category(CategoryName)
VALUES
    (N'Giày thể thao'),
    (N'Áo quần thể thao'),
    (N'Dụng cụ thể thao'),
    (N'Phụ kiện thể thao');

/* =========================================================
   6. SEED 18 PRODUCTS
   ========================================================= */

INSERT INTO dbo.Product
    (ProductName, Description, CategoryID, Price, ImageUrl, StockQuantity, IsActive)
VALUES
    (N'Velocity Run White', N'Giày chạy bộ nhẹ, đệm êm, phù hợp chạy hằng ngày',
     (SELECT CategoryID FROM dbo.Category WHERE CategoryName=N'Giày thể thao'),
     1290000, N'shoe-01.jpg', 20, 1);

INSERT INTO dbo.Product
    (ProductName, Description, CategoryID, Price, ImageUrl, StockQuantity, IsActive)
VALUES
    (N'Aero High Blue', N'Giày cổ cao phong cách thể thao năng động',
     (SELECT CategoryID FROM dbo.Category WHERE CategoryName=N'Giày thể thao'),
     1390000, N'shoe-02.jpg', 18, 1);

INSERT INTO dbo.Product
    (ProductName, Description, CategoryID, Price, ImageUrl, StockQuantity, IsActive)
VALUES
    (N'Cloud Runner White', N'Giày sneaker trắng tối giản, dễ phối đồ',
     (SELECT CategoryID FROM dbo.Category WHERE CategoryName=N'Giày thể thao'),
     1490000, N'shoe-03.jpg', 22, 1);

INSERT INTO dbo.Product
    (ProductName, Description, CategoryID, Price, ImageUrl, StockQuantity, IsActive)
VALUES
    (N'Urban Sprint White', N'Giày thể thao trắng phong cách hiện đại',
     (SELECT CategoryID FROM dbo.Category WHERE CategoryName=N'Giày thể thao'),
     1190000, N'shoe-04.jpg', 16, 1);

INSERT INTO dbo.Product
    (ProductName, Description, CategoryID, Price, ImageUrl, StockQuantity, IsActive)
VALUES
    (N'Pulse Runner Pink', N'Giày thể thao phối màu nổi bật',
     (SELECT CategoryID FROM dbo.Category WHERE CategoryName=N'Giày thể thao'),
     1350000, N'shoe-05.jpg', 14, 1);

INSERT INTO dbo.Product
    (ProductName, Description, CategoryID, Price, ImageUrl, StockQuantity, IsActive)
VALUES
    (N'Core Training Set', N'Bộ áo thun và quần short tập luyện nam',
     (SELECT CategoryID FROM dbo.Category WHERE CategoryName=N'Áo quần thể thao'),
     690000, N'wear-01.jpg', 30, 1);

INSERT INTO dbo.Product
    (ProductName, Description, CategoryID, Price, ImageUrl, StockQuantity, IsActive)
VALUES
    (N'Grey Performance Set', N'Bộ đồ thể thao nam màu xám, thoải mái vận động',
     (SELECT CategoryID FROM dbo.Category WHERE CategoryName=N'Áo quần thể thao'),
     790000, N'wear-02.jpg', 25, 1);

INSERT INTO dbo.Product
    (ProductName, Description, CategoryID, Price, ImageUrl, StockQuantity, IsActive)
VALUES
    (N'Black Active Set', N'Bộ tập nữ tối giản, phù hợp gym và fitness',
     (SELECT CategoryID FROM dbo.Category WHERE CategoryName=N'Áo quần thể thao'),
     850000, N'wear-03.jpg', 24, 1);

INSERT INTO dbo.Product
    (ProductName, Description, CategoryID, Price, ImageUrl, StockQuantity, IsActive)
VALUES
    (N'Beige Yoga Set', N'Bộ activewear nữ màu trung tính',
     (SELECT CategoryID FROM dbo.Category WHERE CategoryName=N'Áo quần thể thao'),
     920000, N'wear-04.jpg', 20, 1);

INSERT INTO dbo.Product
    (ProductName, Description, CategoryID, Price, ImageUrl, StockQuantity, IsActive)
VALUES
    (N'Power Training Set', N'Bộ tập nữ phong cách mạnh mẽ',
     (SELECT CategoryID FROM dbo.Category WHERE CategoryName=N'Áo quần thể thao'),
     890000, N'wear-05.jpg', 18, 1);

INSERT INTO dbo.Product
    (ProductName, Description, CategoryID, Price, ImageUrl, StockQuantity, IsActive)
VALUES
    (N'Iron Dumbbell Pro', N'Tạ tay thép dùng cho bài tập sức mạnh',
     (SELECT CategoryID FROM dbo.Category WHERE CategoryName=N'Dụng cụ thể thao'),
     450000, N'gear-01.jpg', 35, 1);

INSERT INTO dbo.Product
    (ProductName, Description, CategoryID, Price, ImageUrl, StockQuantity, IsActive)
VALUES
    (N'Home Fitness Kit', N'Bộ dụng cụ tập tại nhà gồm thảm, tạ và dây kháng lực',
     (SELECT CategoryID FROM dbo.Category WHERE CategoryName=N'Dụng cụ thể thao'),
     990000, N'gear-02.jpg', 15, 1);

INSERT INTO dbo.Product
    (ProductName, Description, CategoryID, Price, ImageUrl, StockQuantity, IsActive)
VALUES
    (N'Indoor Basketball', N'Bóng rổ dùng cho tập luyện và thi đấu trong nhà',
     (SELECT CategoryID FROM dbo.Category WHERE CategoryName=N'Dụng cụ thể thao'),
     520000, N'gear-03.jpg', 28, 1);

INSERT INTO dbo.Product
    (ProductName, Description, CategoryID, Price, ImageUrl, StockQuantity, IsActive)
VALUES
    (N'Aerobic Step Kit', N'Bộ bục aerobic và phụ kiện tập luyện',
     (SELECT CategoryID FROM dbo.Category WHERE CategoryName=N'Dụng cụ thể thao'),
     1100000, N'gear-04.jpg', 12, 1);

INSERT INTO dbo.Product
    (ProductName, Description, CategoryID, Price, ImageUrl, StockQuantity, IsActive)
VALUES
    (N'Yoga Mat Studio', N'Thảm yoga dành cho tập luyện tại nhà hoặc studio',
     (SELECT CategoryID FROM dbo.Category WHERE CategoryName=N'Dụng cụ thể thao'),
     390000, N'gear-05.jpg', 26, 1);

INSERT INTO dbo.Product
    (ProductName, Description, CategoryID, Price, ImageUrl, StockQuantity, IsActive)
VALUES
    (N'Hydro Sport Bottle', N'Bình nước thể thao nhẹ, tiện mang theo',
     (SELECT CategoryID FROM dbo.Category WHERE CategoryName=N'Phụ kiện thể thao'),
     290000, N'acc-01.jpg', 40, 1);

INSERT INTO dbo.Product
    (ProductName, Description, CategoryID, Price, ImageUrl, StockQuantity, IsActive)
VALUES
    (N'Active Backpack', N'Balo thể thao nhiều ngăn, phù hợp tập luyện và du lịch',
     (SELECT CategoryID FROM dbo.Category WHERE CategoryName=N'Phụ kiện thể thao'),
     650000, N'acc-02.jpg', 18, 1);

INSERT INTO dbo.Product
    (ProductName, Description, CategoryID, Price, ImageUrl, StockQuantity, IsActive)
VALUES
    (N'Classic Sport Cap', N'Nón thể thao phong cách tối giản',
     (SELECT CategoryID FROM dbo.Category WHERE CategoryName=N'Phụ kiện thể thao'),
     320000, N'acc-03.jpg', 32, 1);

/* =========================================================
   7. SAMPLE ORDER
   ========================================================= */
INSERT INTO dbo.OrderPro
    (OrderDate, CustomerID, ShippingAddress, TotalAmount, Status)
VALUES
    (SYSDATETIME(),
     (SELECT CustomerID FROM dbo.Customer WHERE Email=N'minhanh01@gmail.com'),
     N'Quận 10, TP.HCM',
     2970000,
     N'Completed');


DECLARE @SampleOrderID INT = SCOPE_IDENTITY();
INSERT INTO dbo.OrderDetail(OrderID, ProductID, Quantity, UnitPrice)
VALUES
    (@SampleOrderID,
     (SELECT ProductID FROM dbo.Product WHERE ProductName=N'Velocity Run White'),
     1, 1290000),
    (@SampleOrderID,
     (SELECT ProductID FROM dbo.Product WHERE ProductName=N'Core Training Set'),
     1, 690000),
    (@SampleOrderID,
     (SELECT ProductID FROM dbo.Product WHERE ProductName=N'Home Fitness Kit'),
     1, 980000);
GO

/* =========================================================
   8. QUICK CHECK
   ========================================================= */
SELECT * FROM dbo.Role;
SELECT UserID, UserName, RoleID, IsActive, CreatedAt FROM dbo.UserAccount;
SELECT * FROM dbo.Customer;
SELECT * FROM dbo.Category;
SELECT ProductID, ProductName, Price, ImageUrl, StockQuantity FROM dbo.Product;
SELECT * FROM dbo.OrderPro;
SELECT * FROM dbo.OrderDetail;
GO
