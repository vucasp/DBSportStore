USE DBSportStore;
GO
/* =========================================================
   1. BỎ UNIQUE TRÊN Customer.Email
   - Guest có thể đặt nhiều đơn ở nhiều thời điểm.
   - Mỗi lần Checkout B07 tạo một Customer mới.
   - Vì vậy cùng email phải được phép xuất hiện nhiều lần.
	Trong CSDL hiện tại, cột Email của bảng Customer đang có ràng buộc UNIQUE, nghĩa là mỗi email chỉ được xuất hiện một lần.
Tuy nhiên ở B07, khách vãng lai có thể đặt hàng nhiều lần và mỗi lần đặt hàng được xem là một Customer mới, nên cùng một email có thể xuất hiện ở nhiều bản ghi Customer.
	Đoạn SQL trên dùng các bảng hệ thống của SQL Server để tự tìm tên constraint UNIQUE đang gắn với cột Customer.Email, sau đó xóa constraint đó.
	không cần phân tích sâu sys.key_constraints, sys.index_columns, sys.columns. Chỉ cần hiểu mục đích của đoạn script

   ========================================================= */
DECLARE @UQName SYSNAME;

SELECT TOP 1 @UQName = kc.name
FROM sys.key_constraints kc
JOIN sys.index_columns ic
    ON kc.parent_object_id = ic.object_id
   AND kc.unique_index_id = ic.index_id
JOIN sys.columns c
    ON ic.object_id = c.object_id
   AND ic.column_id = c.column_id
WHERE kc.parent_object_id = OBJECT_ID(N'dbo.Customer')
  AND kc.type = 'UQ'
  AND c.name = N'Email';

IF @UQName IS NOT NULL
BEGIN
    EXEC(
        N'ALTER TABLE dbo.Customer DROP CONSTRAINT ['
        + @UQName + N']'
    );
END
GO
/* =========================================================
   2. BỔ SUNG SNAPSHOT THÔNG TIN GIAO HÀNG TRÊN OrderPro
   B08 sẽ dùng ShippingPhone để tra cứu đơn khách vãng lai.
   ========================================================= */
IF COL_LENGTH('dbo.OrderPro', 'RecipientName') IS NULL
BEGIN
    ALTER TABLE dbo.OrderPro
    ADD RecipientName NVARCHAR(100) NULL;
END
GO

IF COL_LENGTH('dbo.OrderPro', 'ShippingPhone') IS NULL
BEGIN
    ALTER TABLE dbo.OrderPro
    ADD ShippingPhone NVARCHAR(20) NULL;
END
GO

SELECT TOP 5 *
FROM dbo.OrderPro
ORDER BY OrderID DESC;
GO

/* =========================================================
   CHO PHÉP NHIỀU CUSTOMER GUEST CÓ UserID = NULL
   nhưng UserID có giá trị vẫn phải duy nhất
   Ý nghĩa: Khách vãng lai chưa có tài khoản nên UserID = NULL. 
   B07 cho phép nhiều Customer guest cùng có UserID = NULL. 
   Với Customer đã có tài khoản, mỗi UserID vẫn chỉ được gắn với một Customer.
   ========================================================= */
DECLARE @UQName SYSNAME;

-- Tìm UNIQUE constraint đang gắn với cột UserID.
SELECT TOP 1 @UQName = kc.name
FROM sys.key_constraints kc
JOIN sys.index_columns ic
    ON kc.parent_object_id = ic.object_id
   AND kc.unique_index_id = ic.index_id
JOIN sys.columns c
    ON ic.object_id = c.object_id
   AND ic.column_id = c.column_id
WHERE kc.parent_object_id = OBJECT_ID(N'dbo.Customer')
  AND kc.type = 'UQ'
  AND c.name = N'UserID';


-- Nếu tìm thấy thì xóa UNIQUE constraint cũ.
IF @UQName IS NOT NULL
BEGIN
    EXEC(
        N'ALTER TABLE dbo.Customer DROP CONSTRAINT ['
        + @UQName + N']'
    );
END
GO


/* Tạo UNIQUE INDEX mới:
   - UserID khác NULL phải duy nhất
   - UserID = NULL không tham gia kiểm tra UNIQUE
*/
CREATE UNIQUE INDEX UX_Customer_UserID
ON dbo.Customer(UserID)
WHERE UserID IS NOT NULL;
GO
