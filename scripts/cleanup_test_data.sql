-- ─────────────────────────────────────────────────────────────
-- GTAS VPP – Dọn rác data test trước khi export cho Live Demo
-- Chạy trên DB GTAS_VPP_TEST (Local) TRƯỚC KHI backup
-- ─────────────────────────────────────────────────────────────
-- ⚠️ KIỂM TRA KỸ TRƯỚC KHI CHẠY! Sửa tên bảng/cột cho đúng schema của bạn.
-- ─────────────────────────────────────────────────────────────

USE GTAS_VPP_TEST;
GO

PRINT N'═══ BẮT ĐẦU DỌN RÁC ═══';

-- ── 1. Xóa user test/rác ────────────────────────────────────
-- Giữ lại admin và các user demo chính
-- Sửa điều kiện WHERE cho phù hợp data thực tế của bạn
/*
DELETE FROM P04_UserGroup 
WHERE UserId IN (
    SELECT UserID FROM [dbo].[Users] 
    WHERE UserLogin LIKE '%test%' 
       OR UserLogin LIKE '%abc%'
       OR UserLogin LIKE '%demo_old%'
);

DELETE FROM [dbo].[Users]
WHERE UserLogin LIKE '%test%' 
   OR UserLogin LIKE '%abc%'
   OR UserLogin LIKE '%demo_old%';
*/
PRINT N'[1/4] Kiểm tra user rác → BỎ COMMENT block trên nếu cần xóa';

-- ── 2. Xóa đơn hàng/yêu cầu rác ───────────────────────────
-- Xóa các request test, lỗi, hoặc data "asdasd"
/*
-- Xóa chi tiết trước, rồi mới xóa header
DELETE FROM [dbo].[VPP_REQUEST_DETAIL] 
WHERE VPPRequestId IN (
    SELECT Id FROM [dbo].[VPP_REQUEST] 
    WHERE VPPNo LIKE '%TEST%' 
       OR VPPNo LIKE '%test%'
       OR Remarks LIKE '%test%'
       OR CreatedDate < '2025-01-01'
);

DELETE FROM [dbo].[VPP_REQUEST]
WHERE VPPNo LIKE '%TEST%' 
   OR VPPNo LIKE '%test%'
   OR Remarks LIKE '%test%'
   OR CreatedDate < '2025-01-01';
*/
PRINT N'[2/4] Kiểm tra request rác → BỎ COMMENT block trên nếu cần xóa';

-- ── 3. Dọn log/audit cũ (nếu có) ───────────────────────────
/*
-- Xóa log cũ hơn 30 ngày
DELETE FROM [dbo].[AuditLogs] WHERE CreatedDate < DATEADD(DAY, -30, GETDATE());
*/
PRINT N'[3/4] Kiểm tra log cũ → BỎ COMMENT block trên nếu cần xóa';

-- ── 4. Verify data còn lại ──────────────────────────────────
SELECT 'Users' AS [Table], COUNT(*) AS [Count] FROM [dbo].[Users]
UNION ALL
SELECT 'P04_UserGroup', COUNT(*) FROM [dbo].[P04_UserGroup]
UNION ALL
SELECT 'VPP_REQUEST', COUNT(*) FROM [dbo].[VPP_REQUEST];

PRINT N'═══ HOÀN TẤT DỌN RÁC – Kiểm tra kết quả trên rồi backup ═══';
GO
