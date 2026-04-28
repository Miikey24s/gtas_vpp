-- ============================================================================
-- 03_SeedLibraryData.sql - Library data: L01-L06, LEX02 departments
-- Combined from: 04_AddVPP.sql + 05_AddSuppliers.sql + 06_AddLEX.sql
-- Idempotent: Uses NOT EXISTS checks
-- ============================================================================

-- PART 1: VPP Categories + Items + UOM (from 04_AddVPP.sql)

GO

SET NOCOUNT ON;

-- =========================================================================================
-- 1. KHAI BÁO BIẾN AUDIT VÀ THỜI GIAN
-- =========================================================================================
DECLARE @AuditUserId INT = 5615;
DECLARE @Now DATETIME = GETDATE();

-- =========================================================================================
-- 2. TẠO BẢNG TẠM CHỨA RAW DATA (thêm cột UOMName cho Đơn vị tính)
-- =========================================================================================
IF OBJECT_ID('tempdb..#RawData') IS NOT NULL DROP TABLE #RawData;
CREATE TABLE #RawData (
    CategoryName NVARCHAR(255),
    ItemName NVARCHAR(255),
    UOMName NVARCHAR(50)
);

-- =========================================================================================
-- 3. INSERT FULL DATA TỪ FILE (CategoryName, ItemName, UOMName)
-- =========================================================================================
INSERT INTO #RawData (CategoryName, ItemName, UOMName) VALUES
-- Nhóm 1: Băng keo_ bấm kim_ bấm lỗ
(N'Băng keo_ bấm kim_ bấm lỗ', N'Acco nhựa UNI', N'Hộp'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Acco sắt SDI', N'Hộp'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Bấm kim Trio - 50 LA (hãng)', N'Cái'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Bấm kim số 10 Plus', N'Cái'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Bấm kim Munix - số 3', N'Cái'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Bấm kim không dùng kim plus', N'Cái'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Kềm Bấm Đinh Ghim Có Điều Chỉnh Tăng Lực', N'Cái'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Bấm kim 50 SA', N'Cái'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Bấm kim 50 LA', N'Cái'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Bấm lỗ trung - 837', N'Cái'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Bấm lỗ trung Trio - 978', N'Cái'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Bấm lỗ lớn Trio - 9670', N'Cái'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Bấm 4 lỗ lớn Trio - 0999D', N'Cái'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Băng keo 2 mặt (1.2cm) 14y', N'Cuộn'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Băng keo 2 mặt (2.4cm) 14y', N'Cuộn'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Băng keo 2 mặt (5cm) 14y', N'Cuộn'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Băng keo 2 mặt đen 2F4 Hàn Quốc', N'Cuộn'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Băng keo 2 mặt vàng (1.0cm) 50y', N'Cuộn'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Băng keo 2 mặt vàng (1.2cm) 50y', N'Cuộn'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Băng keo 2 mặt vàng (2.4cm) 50y', N'Cuộn'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Lưỡi lam trắng', N'Cái'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Băng keo điện NANO- 2cm', N'Cuộn'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Băng keo điện NANO- lớn', N'Cuộn'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Băng keo giấy kem 1.2 cm', N'Cuộn'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Băng keo giấy kem 5 cm', N'Cuộn'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Băng keo giấy kem 2.4 cm', N'Cuộn'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Băng keo xốp 2.4cm 12y', N'Cuộn'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Băng keo trong 1.2cm 100y', N'Cuộn'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Băng Keo Trong 2.4cm 100y', N'Cuộn'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Băng keo VP 1.8x 25y (cuộn nhỏ)', N'Cuộn'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Băng keo dán sách (loại 5 phân)', N'Cuộn'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Băng keo si 5 cm (vàng)', N'Cuộn'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Băng keo si 5 cm (xd)', N'Cuộn'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Băng keo si 3,6 cm (xd)', N'Cuộn'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Băng keo vải 5P', N'Cuộn'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Băng keo trong 2.4cm 100y', N'Cuộn'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Băng keo trong 5cm 100y', N'Cuộn'),
(N'Băng keo_ bấm kim_ bấm lỗ', N'Băng keo trong 7cm 100y', N'Cuộn'),

-- Nhóm 2: Bìa còng_ bìa phân trang_ trình ký
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa giấy A4 ĐL - thơm', N'Xấp'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa Mỹ - A3', N'Tờ'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa còng cua 3f5', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa còng 7cm 2Si HTM F4', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa còng 5cm 2Si HTM F4', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa lỗ TQ GM', N'Xấp'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa lỗ dày A4 500g', N'Xấp'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa kiếng - A4, dày', N'Xấp'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa cây (LĐ)', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa 20 lá nhựa A4- LĐ', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa 40 lá nhựa A4- LĐ', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa 60 lá nhựa A4- LĐ', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa 100 lá nhựa A4- LĐ', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa Accord giấy CV (màu hồng)', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa Accord giấy CV (màu xanh)', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Cặp 12 ngăn Flexoffice FO-EB01 (FS)', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa accord nhựa A4', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa accord nhựa A4 - L1 có lỗ bên gáy', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa nút F4 LĐ', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Túi đựng Hồ Sơ', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa phân trang mũi tên post - it', N'Xấp'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa phân trang nhựa 12 số', N'Bộ'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa 2 lá A4 Plus', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa 2 lá F4 Plus', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa HS 2 ngăn', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa Nhẫn trắng 3.5 P', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa nhẫn O Ring 3,5 P', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'File lồng kính 2 còng 3,5P - A4', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'File lồng kính 2 còng 5P - A5', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Dao rọc giấy lớn SDI 0423-2', N'Cây'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Dao rọc giấy nhỏ SDI 0404-2', N'Cây'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Dao rọc giấy nhỏ Inox Deli 2034', N'Cây'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa trình ký đôi mt', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa trình ký đơn - mica A4', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa trình ký đơn mt', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa trình ký đơn nhựa dẻo', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa dây kéo A4', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa 3 dây caro 7cm', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa 3 dây caro 10cm -TL', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa 3 dây caro 15cm -TL', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa 3 dây caro 20cm -TL dây dài', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa 2 kẹp ngang dọc A4 - đỏ', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa 2 kẹp ngang dọc A4 - xanh', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa 2 kẹp ngang dọc A4 - xanh lá', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa 2 kẹp ngang dọc F4 -xanh (Dài)', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa 2 kẹp ngang dọc A4 - vàng', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa còng 7cm/F4 Plus', N'Cái'),
(N'Bìa còng_ bìa phân trang_ trình ký', N'Bìa còng 5cm/F4 Plus', N'Cái'),

-- Nhóm 3: Kẹp bướm_ kim bấm_ kéo
(N'Kẹp bướm_ kim bấm_ kéo', N'Kẹp bướm 15mm', N'Hộp'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Kẹp bướm 19mm', N'Hộp'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Kẹp bướm 25 mm', N'Hộp'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Kẹp bướm 32mm', N'Hộp'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Kẹp bướm 41mm', N'Hộp'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Kẹp bướm 51mm', N'Hộp'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Kẹp bướm sắt 12cm', N'Cái'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Kẹp bướm sắt 3''''', N'Cái'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Kệ Tầng Xukiva - nhựa trong ĐL', N'Cái'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Kéo cắt giấy 183/180', N'Cái'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Kéo Suremark', N'Cái'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Kéo mổ túi cán đỏ (kéo cắt cá)', N'Cái'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Kéo Sắt P.T số 3 (18cm)', N'Cái'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Kéo Sắt mạ inox - A1 (Số 9 - 16cm)', N'Cái'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Kéo can cam TN10', N'Cây'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Keó cắt passan', N'Cái'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Kéo bấm chỉ', N'Cây'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Kéo bấm chỉ PIN 1423', N'Cây'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Kéo răng cưa', N'Cái'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Hồ khô 8gr G-36S', N'Cây'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Kim bấm Trio-23/10', N'Hộp'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Kim bấm Trio-23/13', N'Hộp'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Kim bấm Trio-23/20', N'Hộp'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Kim bấm 3 SDI', N'Hộp'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Kim bấm số 10 plus', N'Hộp'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Kẹp giấy C82 TQ', N'Hộp'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Kẹp giấy C62 TQ', N'Hộp'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Cây ghim giấy sắt', N'Cây'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Đinh ghim bảng nhung', N'Hộp'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Chặn sách - lớn', N'Cặp'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Kim khoanh', N'Vĩ'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Máy tính FX-570 plus', N'Cái'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Máy tính M28', N'Cái'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Máy tính Casio 12 số (loại tốt) AX120B', N'Cái'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Máy tính casio 12 số JS-120L', N'Cái'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Kim bắn thẻ bài KHL-68X', N'Hộp'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Keo dán UHU', N'Cái'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Máy bấm kim gỗ Barker đỏ', N'Cái'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Kim bấm gỗ 16/6', N'Hộp'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Kéo VP lớn K20', N'Cây'),
(N'Kẹp bướm_ kim bấm_ kéo', N'Máy tính CASIO FX580VN X(TL) (A012801)', N'Cái'),

-- Nhóm 4: Bút viết
(N'Bút viết', N'Ruột bút bi xanh 027', N'Cây'),
(N'Bút viết', N'Bút bi TL 08 xanh', N'Cây'),
(N'Bút viết', N'Bút bi TL 08 đỏ', N'Cây'),
(N'Bút viết', N'Bút bi TL 08 đen', N'Cây'),
(N'Bút viết', N'Bút bi xanh 027', N'Cây'),
(N'Bút viết', N'Bút bi đen 027', N'Cây'),
(N'Bút viết', N'Bút bi đỏ 027', N'Cây'),
(N'Bút viết', N'Bút bi Double A DBP-107 0.7mm Xanh', N'Cây'),
(N'Bút viết', N'Bút gel mini tím', N'Cây'),
(N'Bút viết', N'Bút bi 4 màu', N'Cây'),
(N'Bút viết', N'Bút Gel B-01 Master', N'Cây'),
(N'Bút viết', N'Bút bi Metal Clip - 036, xanh', N'Cây'),
(N'Bút viết', N'Bút T.Long đế cắm bàn PH - 02', N'Bộ'),
(N'Bút viết', N'Bút Uniball(CH)', N'Cây'),
(N'Bút viết', N'Bút ký tên UB 150 xanh', N'Cây'),
(N'Bút viết', N'Bút ký tên UB 150 đen', N'Cây'),
(N'Bút viết', N'Bút ký tên UB 150 đỏ', N'Cây'),
(N'Bút viết', N'Bút Gel mini - đỏ', N'Cây'),
(N'Bút viết', N'Bút Gel mini - đen', N'Cây'),
(N'Bút viết', N'Bút Gel mini - xanh', N'Cây'),
(N'Bút viết', N'Bút lông kim Zebifa Xanh', N'Cây'),
(N'Bút viết', N'Bút lông kim Zebifa đen', N'Cây'),
(N'Bút viết', N'Bút lông kim Zebifa đỏ', N'Cây'),
(N'Bút viết', N'Bút Chì - KOH 2B (Tiệp)', N'Cây'),
(N'Bút viết', N'Bút Chì - KOH 6B (Tiệp)', N'Cây'),
(N'Bút viết', N'Bút Chì bấm loại tốt - 125AT', N'Cây'),
(N'Bút viết', N'Bút chì bấm Pentel AX-105', N'Cây'),
(N'Bút viết', N'Bút chì bấm Pentel A255', N'Cây'),
(N'Bút viết', N'Bút dạ quang ( cam) 28 toyo', N'Cây'),
(N'Bút viết', N'Bút dạ quang ( hồng ) 28 toyo', N'Cây'),
(N'Bút viết', N'Bút dạ quang ( vàng ) 28 toyo', N'Cây'),
(N'Bút viết', N'Bút dạ quang ( xanh lá) 28 toyo', N'Cây'),
(N'Bút viết', N'Bút sáp vặn TL 12 màu (50004912)', N'Hộp'),
(N'Bút viết', N'Bút dạ quang TL - HL03, hồng', N'Cây'),
(N'Bút viết', N'Bút dạ quang TL - HL03, vàng', N'Cây'),
(N'Bút viết', N'Bút dạ quang TL - HL03, x.lá', N'Cây'),
(N'Bút viết', N'Bút dạ quang TL - HL03, cam', N'Cây'),
(N'Bút viết', N'Bút sáp màu vỏ xé', N'Hộp'),
(N'Bút viết', N'Bút Nhũ Bạc Con Gấu', N'Cây'),
(N'Bút viết', N'Bút lông bảng (đỏ) WB 03', N'Cây'),
(N'Bút viết', N'Bút lông bảng (đen) WB 03', N'Cây'),
(N'Bút viết', N'Bút lông bảng (xanh) WB 03', N'Cây'),
(N'Bút viết', N'Bút lông dầu nhỏ T.long PM 04 (Xanh)', N'Cây'),
(N'Bút viết', N'Bút lông dầu nhỏ T.long PM 04 (đen)', N'Cây'),
(N'Bút viết', N'Bút lông dầu nhỏ T.Long PM 04 (đỏ)', N'Cây'),
(N'Bút viết', N'Bút Lông Dầu PM09 (Đen)', N'Cây'),
(N'Bút viết', N'Bút Lông Dầu PM09 (Xanh)', N'Cây'),
(N'Bút viết', N'Bút Lông Dầu PM09 (Đỏ)', N'Cây'),
(N'Bút viết', N'Bút Lông Dầu màu xanh PM01', N'Cây'),
(N'Bút viết', N'Bút Lông Dầu màu đen PM01', N'Cây'),
(N'Bút viết', N'Bút lông dầu nhỏ MO 120 Xanh', N'Cây'),
(N'Bút viết', N'Bút lông dầu nhỏ MO - 120 Đen', N'Cây'),
(N'Bút viết', N'Bút xóa nước TL CP02', N'Cây'),
(N'Bút viết', N'Bút bi master 01', N'Cây'),
(N'Bút viết', N'Bút bay hồng - HQ', N'Cây'),
(N'Bút viết', N'Bút xoá kéo hiệu Plus Corection', N'Cây'),
(N'Bút viết', N'Bọ cắt băng keo sunny 2004', N'Cái'),
(N'Bút viết', N'Bọ cắt băng keo sunny 2003', N'Cái'),
(N'Bút viết', N'Bọ cắt băng keo 2001', N'Cái'),
(N'Bút viết', N'Bút sơn Century''''s', N'Cái'),
(N'Bút viết', N'Bảng 0,8m x 1,2m', N'Cái'),
(N'Bút viết', N'Cắt băng keo DH-5cm', N'Cái'),
(N'Bút viết', N'Cắt băng keo DH-7cm', N'Cái'),
(N'Bút viết', N'Ruột xóa kéo Plus', N'Cái'),
(N'Bút viết', N'Ruột chì ĐL', N'Vĩ'),
(N'Bút viết', N'Ruột chì 0.9mm', N'Hộp'),
(N'Bút viết', N'Ruột chì 0.7mm', N'Vĩ'),
(N'Bút viết', N'Ruột chì Monami ML-SQ 0.5', N'Vĩ'),
(N'Bút viết', N'Bút sơn Demi - vàng', N'Tuýp'),
(N'Bút viết', N'Chuốt bút chì sắt', N'Cái'),
(N'Bút viết', N'Chuốt bút chì Maped nhỏ', N'Cái'),
(N'Bút viết', N'Đồ lau bảng nhung', N'Chiếc'),
(N'Bút viết', N'Gở kim kiềm', N'Cái'),
(N'Bút viết', N'Dụng cụ gỡ kim Trio', N'Cái'),
(N'Bút viết', N'Dụng cụ gỡ kim Eagle 1029', N'Cái'),
(N'Bút viết', N'Ruột bút bi Ceo 2221 1.0mm', N'Cái'),
(N'Bút viết', N'Ruột bút lông bi Ceo 0.5mm', N'Cái'),
(N'Bút viết', N'Bút Chì Bấm 0.7 mm Staedtler 777', N'Cây'),
(N'Bút viết', N'Bút chì khúc', N'Cây'),
(N'Bút viết', N'Bút TL095 xanh', N'Cây'),

-- Nhóm 5: Tập viết_ Thước kẻ
(N'Tập viết_ Thước kẻ', N'Sổ card 320', N'Cuốn'),
(N'Tập viết_ Thước kẻ', N'Sổ card 160', N'Cuốn'),
(N'Tập viết_ Thước kẻ', N'Sổ name card loại 194 lá', N'Cuốn'),
(N'Tập viết_ Thước kẻ', N'Sổ da A5 - dày', N'Cuốn'),
(N'Tập viết_ Thước kẻ', N'Sổ đen A4 bìa da - dày', N'Cuốn'),
(N'Tập viết_ Thước kẻ', N'Sổ caro 25x35 (D)', N'Cuốn'),
(N'Tập viết_ Thước kẻ', N'Sổ caro 30x40 (D)', N'Cuốn'),
(N'Tập viết_ Thước kẻ', N'Tập 200tr SV', N'Quyển'),
(N'Tập viết_ Thước kẻ', N'Tập 200tr Tiến phát', N'Cuốn'),
(N'Tập viết_ Thước kẻ', N'Tập 96tr Tiến phát', N'Cuốn'),
(N'Tập viết_ Thước kẻ', N'Tập 96tr ACB', N'Quyển'),
(N'Tập viết_ Thước kẻ', N'Thước dẻo 20cm (M)', N'Cây'),
(N'Tập viết_ Thước kẻ', N'Thước dẻo 30cm (M)', N'Cây'),
(N'Tập viết_ Thước kẻ', N'Thước dẻo 50cm (M)', N'Cây'),
(N'Tập viết_ Thước kẻ', N'Thước cứng 20cm', N'Cây'),
(N'Tập viết_ Thước kẻ', N'Thước cứng 30cm', N'Cây'),
(N'Tập viết_ Thước kẻ', N'Thước cứng 50cm', N'Cây'),
(N'Tập viết_ Thước kẻ', N'Thước may nhôm - 1m', N'Cây'),
(N'Tập viết_ Thước kẻ', N'Thước may sắt - 50cm', N'Cây'),
(N'Tập viết_ Thước kẻ', N'Thước may sắt - 30cm', N'Cây'),
(N'Tập viết_ Thước kẻ', N'Thước dây may - Hàng Đức 2 mặt vàng (M)', N'Sợi'),
(N'Tập viết_ Thước kẻ', N'Thước dây may kéo - tròn', N'Cái'),
(N'Tập viết_ Thước kẻ', N'Thước dây may - Hàng Đức 2 mặt vàng (L)', N'Sợi'),
(N'Tập viết_ Thước kẻ', N'Thước cuộn sắt', N'Cái'),
(N'Tập viết_ Thước kẻ', N'Thước sắt 1m - 1,5li', N'Cây'),
(N'Tập viết_ Thước kẻ', N'Thước cuộn DEJ 5m', N'Cái'),
(N'Tập viết_ Thước kẻ', N'Thước dây cuộn hoechstmass', N'Cái'),
(N'Tập viết_ Thước kẻ', N'Hóa đơn bán lẻ 1 liên', N'Cuốn'),
(N'Tập viết_ Thước kẻ', N'Sổ card A5 - 240 lá', N'Cuốn'),

-- Nhóm 6: Giấy in các loại
(N'Giấy in các loại', N'Giấy A4 trắng 80 Excell', N'Ram'),
(N'Giấy in các loại', N'Giấy A4 trắng 70 Excell', N'Ram'),
(N'Giấy in các loại', N'Giấy A5 trắng 80 Excell', N'Ram'),
(N'Giấy in các loại', N'Giấy A1 Trắng', N'Tờ'),
(N'Giấy in các loại', N'Giấy Niêm Phong (Mỏng)', N'Xấp'),
(N'Giấy in các loại', N'Giấy in 1 liên chia đôi', N'Thùng'),
(N'Giấy in các loại', N'Giấy in 2 liên không chia', N'Thùng'),
(N'Giấy in các loại', N'Giấy in 3 liên A4 không chia', N'Thùng'),
(N'Giấy in các loại', N'Giấy note vàng 3*3 - UNC', N'Xấp'),
(N'Giấy in các loại', N'Giấy note vàng 3*5', N'Xấp'),
(N'Giấy in các loại', N'Giấy note 4 màu 1*4 (4 màu dạ quang)', N'Xấp'),
(N'Giấy in các loại', N'Note mũi tên 5 màu', N'Xấp'),
(N'Giấy in các loại', N'Note 1.5x2 -Pronoti', N'Xấp'),
(N'Giấy in các loại', N'Note 2x3 - UNC', N'Xấp'),
(N'Giấy in các loại', N'Note 3x4 - UNC', N'Xấp'),
(N'Giấy in các loại', N'Giấy note 3x3 5 màu dạ quang', N'Xấp'),
(N'Giấy in các loại', N'Giấy Decal A4 đế XANH', N'Tờ'),
(N'Giấy in các loại', N'Giấy Decal A4 đế vàng', N'Tờ'),
(N'Giấy in các loại', N'Giấy Decal A4 da bò', N'Tờ'),
(N'Giấy in các loại', N'Giấy A0 Trắng', N'Tờ'),
(N'Giấy in các loại', N'Giấy Decal Tomy - 100', N'Xấp'),
(N'Giấy in các loại', N'Giấy Fo 80 - A4, x.dương', N'Ram'),
(N'Giấy in các loại', N'Giấy Fo 80 - A4, hồng', N'Ram'),
(N'Giấy in các loại', N'Giấy Fo 80 - A4, vàng', N'Ram'),
(N'Giấy in các loại', N'Giấy Fo 80 - A4, x.lá', N'Ram'),
(N'Giấy in các loại', N'Giấy Fo 70 - A4, hồng', N'Ram'),
(N'Giấy in các loại', N'Giay Fo 70 - A4 vàng', N'Ram'),
(N'Giấy in các loại', N'Giay Fo 70 - A4 xanh lá', N'Ram'),
(N'Giấy in các loại', N'Giấy Fo 70 - A4, xanh dương', N'Ram'),
(N'Giấy in các loại', N'Giấy bìa dày - A4 250gam', N'Xấp'),
(N'Giấy in các loại', N'Giấy bìa cứng trắng A4', N'Xấp'),
(N'Giấy in các loại', N'Giấy bìa dầy - A4, x.dương', N'Xấp'),
(N'Giấy in các loại', N'Giấy bìa dầy - A4, trắng', N'Xấp'),
(N'Giấy in các loại', N'Giấy bìa dầy - A4, hồng', N'Xấp'),
(N'Giấy in các loại', N'Giấy bìa dầy - A4, xanh lá', N'Xấp'),
(N'Giấy in các loại', N'Giấy bìa dầy - A4, vàng', N'Xấp'),
(N'Giấy in các loại', N'Giấy bìa dầy - A3, x.dương', N'Xấp'),
(N'Giấy in các loại', N'Giấy than Horse - 100 tờ/xấp', N'Xấp'),
(N'Giấy in các loại', N'Giấy Paper on 80 - A3', N'Ram'),
(N'Giấy in các loại', N'Giấy A3 80gsm Excel Indo', N'Ram'),
(N'Giấy in các loại', N'Giấy ép nhựa A4 80 mic', N'Xấp'),
(N'Giấy in các loại', N'Giấy ép nhựa 80 Mic A3', N'Xấp'),
(N'Giấy in các loại', N'Giấy in ảnh 2 mặt bóng dày ĐL 230', N'Xấp'),
(N'Giấy in các loại', N'Giấy in ảnh 2 mặt bóng mỏng A4', N'Xấp'),
(N'Giấy in các loại', N'Giấy in ảnh decal 135 A4', N'Xấp'),
(N'Giấy in các loại', N'Giấy in ảnh decal A4 1 mặt mòng 135', N'Xấp'),
(N'Giấy in các loại', N'Giấy in ảnh decal A4 1 mặt dày 150', N'Xấp'),
(N'Giấy in các loại', N'Giấy in ảnh 1 mặt dày 230g', N'Xấp'),
(N'Giấy in các loại', N'Bàn cắt giấy A3', N'Cái'),
(N'Giấy in các loại', N'Giấy Fo 80 - A4, cam', N'Ram'),

-- Nhóm 7: Khác
(N'Khác', N'Bao thư trắng A4', N'Cái'),
(N'Khác', N'Bao thư Vàng A4', N'Cái'),
(N'Khác', N'Bao thư trắng A4 (Có keo 2 mặt)', N'Cái'),
(N'Khác', N'Bao thư vàng F4', N'Cái'),
(N'Khác', N'Bao thư trắng A3', N'Cái'),
(N'Khác', N'Bao thư 12x22 F80 CK - có keo', N'Cái'),
(N'Khác', N'Bao thư Trắng A5 không cửa sổ', N'Cái'),
(N'Khác', N'Bao PE 15x25cm', N'Kg'),
(N'Khác', N'Bao PE 20x35cm không quai', N'Kg'),
(N'Khác', N'Bao PE 40x50cm không quai', N'Kg'),
(N'Khác', N'Bao PE 45x60cm không quai', N'Kg'),
(N'Khác', N'bao xốp 2kg có quai', N'Kg'),
(N'Khác', N'bao xốp 50 có quai', N'Kg'),
(N'Khác', N'bao xốp 60x40 có quai', N'Kg'),
(N'Khác', N'Bao zipper 14x10', N'Kg'),
(N'Khác', N'Bao zipper đựng mẫu 5*8cm', N'Kg'),
(N'Khác', N'Bao PE 10x15cm', N'Cái'),
(N'Khác', N'Rổ 3 ngăn liên hoàn', N'Cái'),
(N'Khác', N'Rổ đựng HS', N'Cái'),
(N'Khác', N'Kệ HS nhựa 3 tầng, thanh trụ', N'Cái'),
(N'Khác', N'Gôm Paber pentel Z5', N'Cục'),
(N'Khác', N'Lưỡi dao rọc giấy lớn UNI xanh', N'Vĩ'),
(N'Khác', N'Lưỡi dao rọc giấy SDI 1404 - lớn. 18mm', N'Vĩ'),
(N'Khác', N'Lưỡi dao rọc giấy SDI 1403 - nhỏ. 9mm', N'Vĩ'),
(N'Khác', N'Kệ HS mica 3 tầng trụ', N'Cái'),
(N'Khác', N'Phấn may bay con gấu', N'Hộp'),
(N'Khác', N'Phấn trắng Mic không bụi - 10 viên/hộp', N'Hộp'),
(N'Khác', N'Phấn trắng Mic không bụi - 100 viên/hộp', N'Hộp'),
(N'Khác', N'Phiếu NK Trung 18x20 3L', N'Cuốn'),
(N'Khác', N'Phiếu XK Trung 18x20 3L', N'Cuốn'),
(N'Khác', N'Hộp bút xoay TTM 3006', N'Cái'),
(N'Khác', N'Dụng cụ gỡ chỉ', N'Cái'),
(N'Khác', N'Kim ghim khoanh', N'Vĩ'),
(N'Khác', N'Kẹp bảng tên sắt', N'Cái'),
(N'Khác', N'Dây đeo thẻ lụa hồng', N'Sợi'),
(N'Khác', N'Dây đeo thẻ lụa vàng', N'Sợi'),
(N'Khác', N'Dây Đeo Nút Xoay Inox Xanh Dương', N'Sợi'),
(N'Khác', N'Bảng Tên Dẻo Ngang', N'Cái'),
(N'Khác', N'Bảng Tên Dẻo Ngang Lớn', N'Cái'),
(N'Khác', N'Bảng Tên Dẻo đứng', N'Cái'),
(N'Khác', N'Bảng Tên cứng đứng lớn', N'Cái'),
(N'Khác', N'Cọ vệ sinh bàn phím Chổi Vt', N'Cái'),
(N'Khác', N'Đạn nhựa 10 cm - 5.000 cái/ hộp', N'Hộp'),
(N'Khác', N'Đạn nhựa 5 cm - 5.000 cái/ hộp', N'Hộp'),
(N'Khác', N'Đạn nhựa 7.5 cm - 5.000 cái/hộp', N'Hộp'),
(N'Khác', N'Đạn nhựa 0.7 cm - 5.000 cái/hộp', N'Hộp'),
(N'Khác', N'Dây nhựa xỏ vòng Trắng', N'Hộp'),
(N'Khác', N'Dây nhựa xỏ vòng Đen', N'Hộp'),
(N'Khác', N'Dụng cụ ghim nhãn Súng bắn ti VP Tool F(Đỏ) - Ti Nhuyễn', N'Cây'),
(N'Khác', N'Dụng cụ ghim nhãn Súng bắn ti VP Tool S (xanh dương) - Ti tiêu chuẩn', N'Cây'),
(N'Khác', N'Dụng cụ ghim nhãn Súng bắn ti KHL-8S - Ti tiêu chuẩn (Đen)', N'Cái'),
(N'Khác', N'Kim ghim nhãn N4-F Saga - Ti nhuyễn sắt', N'Hộp'),
(N'Khác', N'Kim ghim nhãn KHL N4-P - Ti tiêu chuẩn nhựa', N'Hộp'),
(N'Khác', N'Lưỡi dao lam', N'Hộp'),
(N'Khác', N'Bao PE 6x12cm', N'Kg'),
(N'Khác', N'Hộp Bút Gỗ GM-6004', N'Cái'),
(N'Khác', N'Lọ Đựng bút sắt', N'Cái'),
(N'Khác', N'Ca đựng nước Tulip 2L', N'Cái'),
(N'Khác', N'Fomex 5mm 60x80 cm', N'Tấm'),
(N'Khác', N'Băng keo giấy vàng 7mm (40 cuộn/cây)', N'Cây'),
(N'Khác', N'Bao PE 4x7 cm', N'Kg'),
(N'Khác', N'Hộp bút sắt 3 ngăn', N'Cái'),
(N'Khác', N'Dao rọc giấy Deli cao cấp E2043', N'Cây'),
(N'Khác', N'Bao PE 9x16 cm', N'Kg'),
(N'Khác', N'Bao PE 30x40 cm', N'Kg'),
(N'Khác', N'Bao PE 25x35cm', N'Kg'),
(N'Khác', N'Túi hột xoài PE 40x60 Đen', N'Kg'),
(N'Khác', N'Menu mica A4 đứng, để bàn', N'Cái'),
(N'Khác', N'Kim súng bắn giá (N4-P)', N'Hộp'),
(N'Khác', N'Decal đánh lỗi mũi tên A5 (25 xấp/lốc)', N'Lốc'),
(N'Khác', N'Bao zipper 16x23cm', N'Kg'),
(N'Khác', N'Bao PE 8x14 cm', N'Kg'),
(N'Khác', N'Bao kiếng 5x8 cm', N'Kg'),
(N'Khác', N'Bao kiếng 32x42cm có keo', N'Kg'),
(N'Khác', N'Dây đeo thẻ lụa xanh dương', N'Cái'),
(N'Khác', N'Bảng tên Enter BT060 xanh dương', N'Cái'),
(N'Khác', N'Sóng hở 1T9', N'Cái'),
(N'Khác', N'Khung bằng khen A4', N'Cái'),
(N'Khác', N'Sóng hở 1T5', N'Cái'),
(N'Khác', N'Bảng tên có kim cài', N'Cái'),
(N'Khác', N'Bao bì nhựa thân thiện môi trường tashing 90x120 cm, 51mic', N'Kg'),

-- Nhóm 8: Phục vụ Văn phòng
(N'Phục vụ Văn phòng', N'Hồ nước Keo lỏng TL G08 (30 ml)', N'Hộp'),
(N'Phục vụ Văn phòng', N'Nước suối lavie 350ml', N'Thùng'),
(N'Phục vụ Văn phòng', N'Mực Lông Bảng TL 25ml - xanh', N'Hộp'),
(N'Phục vụ Văn phòng', N'Mực Lông Bảng TL 25ml - đỏ', N'Hộp'),
(N'Phục vụ Văn phòng', N'Mực lông dầu - xanh', N'Hộp'),
(N'Phục vụ Văn phòng', N'Mực lông dầu - đen', N'Hộp'),
(N'Phục vụ Văn phòng', N'Mực lông dầu - đỏ', N'Hộp'),
(N'Phục vụ Văn phòng', N'Mực đóng dấu Shiny - đỏ', N'Hộp'),
(N'Phục vụ Văn phòng', N'Mực đóng dấu Shiny - xanh', N'Hộp'),
(N'Phục vụ Văn phòng', N'Mực đóng dấu Shiny - đen', N'Hộp'),
(N'Phục vụ Văn phòng', N'Tampon Horse - Xanh', N'Hộp'),
(N'Phục vụ Văn phòng', N'Tampon dấu tên', N'Cái'),
(N'Phục vụ Văn phòng', N'Tampon Shiny - No 1, có mực', N'Hộp'),
(N'Phục vụ Văn phòng', N'Chai chống sét RP07', N'Chai'),
(N'Phục vụ Văn phòng', N'Pin Vuông 9V Golite/Toshiba/Panasonic', N'Viên'),
(N'Phục vụ Văn phòng', N'Pin đồng hồ 2A Maxell', N'Viên'),
(N'Phục vụ Văn phòng', N'Pin đồng hồ 3A Maxell', N'Viên'),
(N'Phục vụ Văn phòng', N'Pin đồng hồ Pin 2A Energizer', N'Vĩ'),
(N'Phục vụ Văn phòng', N'Pin đồng hồ Pin 3A Energizer', N'Vĩ'),
(N'Phục vụ Văn phòng', N'Pin chuông A23-12V Camelion vỉ/5v', N'Vĩ'),
(N'Phục vụ Văn phòng', N'Pin nút áo AG13', N'Viên'),
(N'Phục vụ Văn phòng', N'Pin sạc 2A', N'Cặp'),
(N'Phục vụ Văn phòng', N'Pin Trung C R14UT/2S Panasonic/Toshiba', N'Viên'),
(N'Phục vụ Văn phòng', N'Pin cmos loại dày LR927', N'Vĩ'),
(N'Phục vụ Văn phòng', N'Pin cmos CR2025 Maxcell/ panasonic/Toshiba', N'Viên'),
(N'Phục vụ Văn phòng', N'Pin cmos CR2032 Maxcell/ panasonic/Toshiba', N'Viên'),
(N'Phục vụ Văn phòng', N'Pin cmos CR1220 - 3V', N'Viên'),
(N'Phục vụ Văn phòng', N'Pin cmos loại cúc áo AG10', N'Viên'),
(N'Phục vụ Văn phòng', N'Pin Camelion A23', N'Viên'),
(N'Phục vụ Văn phòng', N'Pin Panasonic LR6AA', N'Viên'),
(N'Phục vụ Văn phòng', N'Pin Panasonic 4200 MAH', N'Viên'),
(N'Phục vụ Văn phòng', N'Pin Panasonic 3400 MAH', N'Viên'),
(N'Phục vụ Văn phòng', N'Thun (màu vàng) cỡ lớn', N'Bịch'),
(N'Phục vụ Văn phòng', N'Thun (màu vàng) cỡ trung', N'Bịch'),
(N'Phục vụ Văn phòng', N'Thun (màu vàng) cỡ tiểu', N'Bịch'),
(N'Phục vụ Văn phòng', N'Thun (màu trắng) Dây Thun XK L1', N'Bịch'),
(N'Phục vụ Văn phòng', N'Dây dù - trung', N'Bó'),
(N'Phục vụ Văn phòng', N'Dây rút nhỏ', N'Bịch'),
(N'Phục vụ Văn phòng', N'Dây rút 20cm', N'Bịch'),
(N'Phục vụ Văn phòng', N'Dây rút 30cm', N'Bịch'),
(N'Phục vụ Văn phòng', N'Dây nilon trắng', N'Cuộn'),
(N'Phục vụ Văn phòng', N'Dây nilon 3 màu', N'Cuộn'),
(N'Phục vụ Văn phòng', N'Bảng flipchart 1m2x2m', N'Cái'),
(N'Phục vụ Văn phòng', N'Cây Lau Nhà Thường tròn', N'Cây'),
(N'Phục vụ Văn phòng', N'Cây Lau Nhà tròn - inox', N'Cây'),
(N'Phục vụ Văn phòng', N'Cây Lau Nhà Công Nghiệp 6T', N'Cây'),
(N'Phục vụ Văn phòng', N'Cây lau nhà Muoss', N'Cây'),
(N'Phục vụ Văn phòng', N'Bộ lau nhà vắt xoay - 360', N'Bộ'),
(N'Phục vụ Văn phòng', N'Cây lau nhà vắt xoay - 360', N'Cái'),
(N'Phục vụ Văn phòng', N'Bông lau nhà 360', N'Cái'),
(N'Phục vụ Văn phòng', N'Cây lăn chặm bụi', N'Cái'),
(N'Phục vụ Văn phòng', N'Chổi lông gà (loại nilon)', N'Cây'),
(N'Phục vụ Văn phòng', N'Chổi lông gà', N'Cây'),
(N'Phục vụ Văn phòng', N'Chổi Chà Tốt', N'Cây'),
(N'Phục vụ Văn phòng', N'Chổi lông cỏ Cán Cây', N'Cây'),
(N'Phục vụ Văn phòng', N'Chổi lông cỏ cán nhựa', N'Cây'),
(N'Phục vụ Văn phòng', N'Chổi lông cỏ cán ngắn', N'Cây'),
(N'Phục vụ Văn phòng', N'Chổi lông cỏ cán cây (ĐB dày)', N'Cây'),
(N'Phục vụ Văn phòng', N'Chổi chà (chổi dừa quét nước)', N'Cây'),
(N'Phục vụ Văn phòng', N'Cọ vệ sinh máy vi tính', N'Cây'),
(N'Phục vụ Văn phòng', N'Chổi quét nhà - nhựa mềm', N'Cây'),
(N'Phục vụ Văn phòng', N'Bao xốp zin đại 60x100', N'Kg'),
(N'Phục vụ Văn phòng', N'Bao rác lớn', N'Cuộn'),
(N'Phục vụ Văn phòng', N'Bao rác trung', N'Cuộn'),
(N'Phục vụ Văn phòng', N'Bao rác tiểu', N'Cuộn'),
(N'Phục vụ Văn phòng', N'Cây quét trần nhà 3 in 1', N'Bộ'),
(N'Phục vụ Văn phòng', N'Bao PE 35 x 50cm', N'Kg'),
(N'Phục vụ Văn phòng', N'Bao PE ( 37 x 47 cm)', N'Kg'),
(N'Phục vụ Văn phòng', N'Bao PE 40 x 60cm', N'Kg'),
(N'Phục vụ Văn phòng', N'Bao PE 60 x 100cm', N'Kg'),
(N'Phục vụ Văn phòng', N'Bao PE 70 x 80cm', N'Kg'),
(N'Phục vụ Văn phòng', N'Bao PE 0.9x1.2m', N'Kg'),
(N'Phục vụ Văn phòng', N'Bao PE 50 x 70cm', N'Kg'),
(N'Phục vụ Văn phòng', N'Bao PE loại 0.5 kg (13x23)cm', N'Kg'),
(N'Phục vụ Văn phòng', N'Bao PE 9 x 15 cm', N'Kg'),
(N'Phục vụ Văn phòng', N'Bao zipper 6x8 cm', N'Kg'),
(N'Phục vụ Văn phòng', N'Bao zipper 12x17 cm', N'Kg'),
(N'Phục vụ Văn phòng', N'Chổi quét trần nhà', N'Cái'),
(N'Phục vụ Văn phòng', N'Chổi quét nước nhựa cứng', N'Cây'),
(N'Phục vụ Văn phòng', N'Cây Cạo Nước', N'Cây'),
(N'Phục vụ Văn phòng', N'Ky hốt rác cán dài Ki rác cán dài 70cm', N'Cái'),
(N'Phục vụ Văn phòng', N'Ky hốt rác nhỏ', N'Cái'),
(N'Phục vụ Văn phòng', N'Ky hốt rác inox', N'Cái'),
(N'Phục vụ Văn phòng', N'Nước rửa chén Sunlight 750g', N'Chai'),
(N'Phục vụ Văn phòng', N'Nước rửa chén Sunlight - 3.5kg', N'Can'),
(N'Phục vụ Văn phòng', N'Nước lau sàn Sunlight 1L', N'Chai'),
(N'Phục vụ Văn phòng', N'Nước lau sàn Sunlight 3.5kg', N'Can'),
(N'Phục vụ Văn phòng', N'Nước lau sàn Gift 1L', N'Chai'),
(N'Phục vụ Văn phòng', N'Nuớc lau kiếng Gift - 520ml', N'Chai'),
(N'Phục vụ Văn phòng', N'Chổi lông gà quyét trần nhà', N'Cây'),
(N'Phục vụ Văn phòng', N'Cọ vệ sinh - tốt', N'Cây'),
(N'Phục vụ Văn phòng', N'Long não - lớn', N'Bịch'),
(N'Phục vụ Văn phòng', N'Nước tẩy vịt Duck 1L tím', N'Chai'),
(N'Phục vụ Văn phòng', N'Nước tẩy Vim 500ml', N'Chai'),
(N'Phục vụ Văn phòng', N'Nước tẩy sumo', N'Chai'),
(N'Phục vụ Văn phòng', N'Nước tẩy Sprayway', N'Chai'),
(N'Phục vụ Văn phòng', N'Nước tẩy quần áo Javel 1L', N'Chai'),
(N'Phục vụ Văn phòng', N'Sọt rác bầu đại', N'Cái'),
(N'Phục vụ Văn phòng', N'Sọt rác Đại', N'Cái'),
(N'Phục vụ Văn phòng', N'Sọt rác Trung Fataco', N'Cái'),
(N'Phục vụ Văn phòng', N'Sọt rác Tiểu', N'Cái'),
(N'Phục vụ Văn phòng', N'Khay đựng xà phòng', N'Cái'),
(N'Phục vụ Văn phòng', N'Thùng rác đại công nghiệp', N'Cái'),
(N'Phục vụ Văn phòng', N'Thùng rác trung công nghiệp', N'Cái'),
(N'Phục vụ Văn phòng', N'Thùng rác vệ sinh tiểu', N'Cái'),
(N'Phục vụ Văn phòng', N'Thùng rác vệ sinh trung', N'Cái'),
(N'Phục vụ Văn phòng', N'Thùng rác vệ sinh lớn (Xám/ Nâu)', N'Cái'),
(N'Phục vụ Văn phòng', N'Xà bông Surf hoa hạ 400g', N'Gói'),
(N'Phục vụ Văn phòng', N'Xà bông Surf hoa hạ 800g', N'Gói'),
(N'Phục vụ Văn phòng', N'Xà bông omo bột 770g', N'Gói'),
(N'Phục vụ Văn phòng', N'Xà bông omo bột 2.9KG', N'Gói'),
(N'Phục vụ Văn phòng', N'Cây gắp rác', N'Cái'),
(N'Phục vụ Văn phòng', N'Nước lau tay lightbuoy 180g', N'Chai'),
(N'Phục vụ Văn phòng', N'Xà bông Lifebuoy (90g)', N'Cục'),
(N'Phục vụ Văn phòng', N'Cước xanh nhám lớn (C4)', N'Cái'),
(N'Phục vụ Văn phòng', N'Miếng Cước - sắt', N'Cái'),
(N'Phục vụ Văn phòng', N'Cước kim tuyến', N'Cái'),
(N'Phục vụ Văn phòng', N'Xô đựng nước - 8 lít', N'Cái'),
(N'Phục vụ Văn phòng', N'Xô đựng nước lớn - 12 lít', N'Cái'),
(N'Phục vụ Văn phòng', N'Xô đựng nước lớn - 16 lít', N'Cái'),
(N'Phục vụ Văn phòng', N'Xịt muỗi Raid max 600 ml', N'Chai'),
(N'Phục vụ Văn phòng', N'Xịt phòng Hương lài 280 ml', N'Chai'),
(N'Phục vụ Văn phòng', N'Xịt phòng Glade 280ml', N'Chai'),
(N'Phục vụ Văn phòng', N'Sáp thơm hương lài', N'Hộp'),
(N'Phục vụ Văn phòng', N'Bao tay cao su dài HQ', N'Đôi'),
(N'Phục vụ Văn phòng', N'Bao tay cao su HQ - Loại dày L', N'Đôi'),
(N'Phục vụ Văn phòng', N'Ca múc nước', N'Cái'),
(N'Phục vụ Văn phòng', N'Khóa Việt Tiệp 01610', N'Bộ'),
(N'Phục vụ Văn phòng', N'Khóa dây', N'Bộ'),
(N'Phục vụ Văn phòng', N'Ổ cắm điện', N'Cái'),
(N'Phục vụ Văn phòng', N'Ổ điện 3 lỗ', N'Cái'),
(N'Phục vụ Văn phòng', N'Ổ điện 6 lỗ', N'Cái'),
(N'Phục vụ Văn phòng', N'Nước lau tay PAX (hương táo, đào)', N'Chai'),
(N'Phục vụ Văn phòng', N'Nước rửa tay Lifebuoy 4kg', N'Can'),
(N'Phục vụ Văn phòng', N'Nước lau tay Lifebuoy 450g', N'Chai'),
(N'Phục vụ Văn phòng', N'Nước lau tay Lifebuoy 180g', N'Chai'),
(N'Phục vụ Văn phòng', N'Thảm lau chân lớn', N'Tấm'),
(N'Phục vụ Văn phòng', N'Thảm lau chân vải sợi mềm', N'Tấm'),
(N'Phục vụ Văn phòng', N'Thảm welcom nhựa 40cm x 60cm', N'Tấm'),
(N'Phục vụ Văn phòng', N'Thảm lau chân vải thun', N'Tấm'),
(N'Phục vụ Văn phòng', N'Bông lau nhà vắt Mỹ phong', N'Cái'),
(N'Phục vụ Văn phòng', N'Bông lau nhà tròn 360', N'Cái'),
(N'Phục vụ Văn phòng', N'Giấy vệ sinh', N'Cây'),
(N'Phục vụ Văn phòng', N'Bật lữa', N'Cái'),
(N'Phục vụ Văn phòng', N'Giấy hộp Japani', N'Hộp'),
(N'Phục vụ Văn phòng', N'Bộ vệ sinh máy tính lap top', N'Bộ'),
(N'Phục vụ Văn phòng', N'Cây bàn chải nhựa lớn có cán (chà sàn)', N'Cái'),
(N'Phục vụ Văn phòng', N'Bàn chải giặt đồ', N'Cái'),
(N'Phục vụ Văn phòng', N'Nước suối LAVIE 500ml', N'Thùng'),
(N'Phục vụ Văn phòng', N'Khăn lau kính', N'Cái'),
(N'Phục vụ Văn phòng', N'Bột thông cầu', N'Bịch'),
(N'Phục vụ Văn phòng', N'Cây hút hầm cầu', N'Cây'),
(N'Phục vụ Văn phòng', N'Cây lau kính cán dài đa năng 2 in 1', N'Bộ'),
(N'Phục vụ Văn phòng', N'Bông cây lau kính', N'Cái'),
(N'Phục vụ Văn phòng', N'Ủng cao su', N'Đôi'),
(N'Phục vụ Văn phòng', N'Nước xả vaỉ comfort 1L6', N'Bịch'),
(N'Phục vụ Văn phòng', N'Dây tag nhựa Trắng 13,5cm', N'Bịch'),
(N'Phục vụ Văn phòng', N'Nước lau kính Happy price 5L', N'Can'),
(N'Phục vụ Văn phòng', N'Keo chống tưa vải', N'Chai'),
(N'Phục vụ Văn phòng', N'Cây chà nhà vệ sinh cán dài 1m', N'Cái'),
(N'Phục vụ Văn phòng', N'Sáp đếm tiền', N'Hộp'),
(N'Phục vụ Văn phòng', N'Bao Tay nilong', N'Hộp'),
(N'Phục vụ Văn phòng', N'Bao Tay len kem 60g', N'Đôi'),
(N'Phục vụ Văn phòng', N'Bảng Trắng 0.8m x 1.2m thường', N'Cái'),
(N'Phục vụ Văn phòng', N'Tampon Con dấu S829 - Đỏ', N'Cái'),
(N'Phục vụ Văn phòng', N'Tampon Con dấu S538 - Đỏ', N'Cái'),
(N'Phục vụ Văn phòng', N'Bông lau bẹ 80cm', N'Cái'),
(N'Phục vụ Văn phòng', N'Tăm bông S-843', N'Cái'),
(N'Phục vụ Văn phòng', N'Tăm bông S-542 (đỏ)', N'Cái'),
(N'Phục vụ Văn phòng', N'Nước rửa chén Sunlight hương thiên nhiên 750g', N'Chai'),
(N'Phục vụ Văn phòng', N'Nước rửa chén Sunlight hương thiên nhiên 3.5kg', N'Can'),
(N'Phục vụ Văn phòng', N'Thảm welcome 60x90cm', N'Tấm'),
(N'Phục vụ Văn phòng', N'Mực đóng dấu chuyên dụng SI-63- Đỏ', N'Chai'),
(N'Phục vụ Văn phòng', N'Mực đóng dấu chuyên dụng SI-63- Xanh', N'Chai'),
(N'Phục vụ Văn phòng', N'Dấu TD T414 58x22mm', N'Cái');

-- =========================================================================================
-- 4. BẮT ĐẦU TRANSACTION ĐỂ ĐỒNG BỘ DATA VÀO DATABASE
-- =========================================================================================
BEGIN TRAN;
    BEGIN TRY
        -- ---------------------------------------------------------------------------------
        -- 4.1. INSERT BẢNG L01_Class (Tạo Class "Đơn vị tính" nếu chưa có)
        -- ---------------------------------------------------------------------------------
        IF NOT EXISTS (SELECT 1 FROM L01_Class WHERE ClassCode = 'UOM')
        BEGIN
            INSERT INTO L01_Class (Id, ClassCode, ClassName, ClassModul, Description, CreateUserId, CreateDate, UpdateUserId, UpdateDate, IsDeleted)
            VALUES (NEWID(), 'UOM', N'Đơn vị tính', N'VPP', N'Danh mục đơn vị tính cho Văn phòng phẩm', @AuditUserId, @Now, @AuditUserId, @Now, 0);
        END

        PRINT N'✅ Đã đồng bộ Class UOM (L01_Class).';

        -- ---------------------------------------------------------------------------------
        -- 4.2. INSERT BẢNG L02_ClassDetail (Các giá trị Đơn vị tính)
        -- Lấy danh sách UOM duy nhất từ #RawData, chống trùng ClassDetailValue
        -- ---------------------------------------------------------------------------------
        DECLARE @UOMClassId UNIQUEIDENTIFIER;
        SELECT @UOMClassId = Id FROM L01_Class WHERE ClassCode = 'UOM';

        INSERT INTO L02_ClassDetail (Id, ClassId, ClassDetailCode, ClassDetailValue, Sort, Description, CreateUserId, CreateDate, UpdateUserId, UpdateDate, IsDeleted)
        SELECT 
            NEWID(),
            @UOMClassId,
            'UOM_' + RIGHT('00' + CAST(ROW_NUMBER() OVER(ORDER BY UOMName) AS VARCHAR), 2),
            UOMName,
            ROW_NUMBER() OVER(ORDER BY UOMName),
            N'Đơn vị: ' + UOMName,
            @AuditUserId, @Now, @AuditUserId, @Now, 0
        FROM (
            SELECT DISTINCT UOMName FROM #RawData
        ) AS distUOM
        WHERE NOT EXISTS (
            SELECT 1 FROM L02_ClassDetail cd 
            WHERE cd.ClassId = @UOMClassId AND cd.ClassDetailValue = distUOM.UOMName
        );

        PRINT N'✅ Đã đồng bộ Đơn vị tính (L02_ClassDetail).';

        -- ---------------------------------------------------------------------------------
        -- 4.3. INSERT BẢNG L03_VPPCategory (Nhóm Danh Mục)
        -- Chống trùng tên nhóm, đánh mã CAT_01
        -- ---------------------------------------------------------------------------------
        INSERT INTO L03_VPPCategory (Id, VPPCategoryCode, VPPCategoryName, Description, CreateUserId, CreateDate, UpdateUserId, UpdateDate, IsDeleted)
        SELECT 
            NEWID(),
            'CAT_' + RIGHT('00' + CAST(ROW_NUMBER() OVER(ORDER BY CategoryName) AS VARCHAR), 2) AS VPPCategoryCode,
            CategoryName,
            N'Danh mục ' + CategoryName AS Description,
            @AuditUserId, @Now, @AuditUserId, @Now, 0
        FROM (
            SELECT DISTINCT CategoryName FROM #RawData
        ) AS distRaw
        WHERE NOT EXISTS (
            SELECT 1 FROM L03_VPPCategory dbCat WHERE dbCat.VPPCategoryName = distRaw.CategoryName
        );

        PRINT N'✅ Đã đồng bộ Danh mục (L03_VPPCategory).';

        -- ---------------------------------------------------------------------------------
        -- 4.4. INSERT BẢNG L04_VPP (Vật Phẩm Chi Tiết)
        -- Liên kết (JOIN) với L03 để lấy CategoryId, JOIN L02 để lấy UOMId
        -- ---------------------------------------------------------------------------------
        INSERT INTO L04_VPP (Id, VPPCode, VPPName, UOMId, VPPCategoryId, Description, CreateUserId, CreateDate, UpdateUserId, UpdateDate, IsDeleted)
        SELECT 
            NEWID(),
            'VPP_' + UPPER(SUBSTRING(REPLACE(CAST(NEWID() AS VARCHAR(36)), '-', ''), 1, 6)) AS VPPCode,
            r.ItemName AS VPPName,
            uom.Id AS UOMId,
            c.Id AS VPPCategoryId,
            N'Vật phẩm ' + r.ItemName AS Description,
            @AuditUserId, @Now, @AuditUserId, @Now, 0
        FROM #RawData r
        INNER JOIN L03_VPPCategory c ON r.CategoryName = c.VPPCategoryName
        INNER JOIN L02_ClassDetail uom ON uom.ClassDetailValue = r.UOMName AND uom.ClassId = @UOMClassId
        WHERE NOT EXISTS (
            SELECT 1 FROM L04_VPP dbItem WHERE dbItem.VPPName = r.ItemName
        );

        PRINT N'✅ Đã đồng bộ Vật Phẩm (L04_VPP).';
        
        COMMIT TRAN;
        PRINT N'🎉🎉🎉 HOÀN TẤT ĐỒNG BỘ DỮ LIỆU!!!';
    END TRY
    BEGIN CATCH
        ROLLBACK TRAN;
        PRINT N'❌ CÓ LỖI XẢY RA, ĐÃ ROLLBACK DỮ LIỆU!';
        PRINT ERROR_MESSAGE();
    END CATCH

-- =========================================================================================
-- 5. DỌN DẸP BẢNG TẠM
-- =========================================================================================
IF OBJECT_ID('tempdb..#RawData') IS NOT NULL DROP TABLE #RawData;
GO


-- PART 2: Suppliers + Mapping (from 05_AddSuppliers.sql)
BEGIN TRANSACTION;
BEGIN TRY
    -- 1. Khai báo bảng tạm chứa 3 nhà cung cấp mới
    DECLARE @NewSuppliers TABLE (Id UNIQUEIDENTIFIER, Name NVARCHAR(MAX), ShortName NVARCHAR(MAX), City NVARCHAR(MAX));
    
    INSERT INTO @NewSuppliers (Id, Name, ShortName, City)
    VALUES     
    (NEWID(), N'VPP Thăng Long', 'VPP_HN', N'Hà Nội'),    
    (NEWID(), N'VPP Sông Hàn', 'VPP_DN', N'Đà Nẵng'),    
    (NEWID(), N'VPP Gia Định', 'VPP_HCM', N'Hồ Chí Minh');

    -- 2. Thêm vào bảng L05_VPPSupplier 
    -- (Bổ sung UpdateUserId và UpdateDate)
    INSERT INTO dbo.L05_VPPSupplier (
        Id, SupplierShortName, SupplierName, City, 
        CreateUserId, CreateDate, UpdateUserId, UpdateDate, IsDeleted
    )
    SELECT 
        Id, ShortName, Name, City, 
        5615, GETDATE(), 5615, GETDATE(), 0
    FROM @NewSuppliers;

    -- 3. Bulk Map: Nhân 3 NCC này với TẤT CẢ VPP đang hoạt động vào bảng L06
    -- (Bổ sung UpdateUserId và UpdateDate)
    INSERT INTO dbo.L06_VPPSupplierMapping (
        Id, Price, L04_VPPId, L05_VPPSupplierId, Description, 
        CreateUserId, CreateDate, UpdateUserId, UpdateDate, IsDeleted
    )
    SELECT 
        NEWID(), 
        0,              -- Giá mặc định khởi tạo
        V.Id,           -- Id từ bảng VPP hiện có
        S.Id,           -- Id của 3 NCC vừa tạo
        N'Thiết lập giá mặc định theo khu vực ' + S.City,
        5615, 
        GETDATE(), 
        5615,           -- Gán giá trị bắt buộc cho UpdateUserId
        GETDATE(),      -- Gán giá trị bắt buộc cho UpdateDate
        0
    FROM dbo.L04_VPP V
    CROSS JOIN @NewSuppliers S 
    WHERE V.IsDeleted = 0;

    COMMIT TRANSACTION;
    PRINT N'Thành công: Đã thêm 3 NCC và tự động tạo Mapping cho toàn bộ danh mục VPP.';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    SELECT ERROR_MESSAGE() AS Error;
END CATCH
-------------------------------------
BEGIN TRANSACTION;
BEGIN TRY
    -- Cập nhật giá ngẫu nhiên cho những dòng mapping vừa tạo (CreateUserId = 5615)
    UPDATE dbo.L06_VPPSupplierMapping
    SET 
        Price = (ABS(CHECKSUM(NEWID())) % 495001) + 5000, -- Công thức: (Random % (Max-Min+1)) + Min
        UpdateDate = GETDATE()
    WHERE CreateUserId = 5615 
      AND Price = 0; -- Chỉ cập nhật những dòng đang bị bằng 0

    COMMIT TRANSACTION;
    PRINT N'Thành công: Đã cập nhật giá ngẫu nhiên cho toàn bộ dữ liệu!';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    SELECT ERROR_MESSAGE() AS Error;
END CATCH

-- PART 3: Departments (from 06_AddLEX.sql)
-- Idempotent wrapper for LEX02 departments
IF NOT EXISTS (SELECT 1 FROM LEX02_CompanyDepartmentLocation WHERE LEX02Code = 'IT')
BEGIN
BEGIN TRAN;

INSERT INTO LEX02_CompanyDepartmentLocation 
    (Id, LEX02Code, LEX02Name, LEX02Type, ParentId, Description, CreateUserId, CreateDate, UpdateUserId, UpdateDate, IsDeleted)
VALUES
    (NEWID(), 'CBSX', N'CBSX', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'CONGNGHEMAY', N'CÔNG NGHỆ MAY', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'CONGNGHEWASH', N'CÔNG NGHỆ WASH', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'SOURCING', N'SOURCING', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'CPD', N'CPD', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'DAUTU', N'ĐẦU TƯ', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'DINHMUC', N'ĐỊNH MỨC', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'FD', N'FD', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'FQM', N'FQM', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'GIAMDINH', N'GIÁM ĐỊNH', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'HCQT', N'HCQT', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'HOANTATPHUOCLONG', N'HOÀN TẤT PHƯỚC LONG', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'IT', N'IT', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'KD5+6', N'KD5+6', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'KD25', N'KD25', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'KD26', N'KD26', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'KD27', N'KD27', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'KD1', N'KD1', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'KD17', N'KD17+KD16+PPJW1', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'KD19', N'KD19', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'KD2', N'KD2', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'KD3', N'KD3', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'KD4', N'KD4', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'KD7', N'KD7', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'KD8', N'KD8', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'KEHOACH', N'KẾ HOẠCH', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'KEHOACHPNC', N'KẾ HOẠCH PNC', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'KHOTONG', N'KHO TỔNG', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'LONGAN', N'LONG AN', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'MAULONGAN', N'MAY MẪU LONG AN', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'NHOMFITTECH Rap', N'NHÓM FITTECH RẬP', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'NSTL', N'NSTL', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'PHAPCHE', N'PHÁP CHẾ', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'PURCHASING', N'PURCHASING', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'QA', N'QA', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'QLTBMAY', N'QLTB MAY', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'R&D', N'R&D', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'SOURCING', N'SOURCING', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'TCKT', N'TCKT', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'TCKTKHO', N'TCKT KHO', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'TCKTVTJ', N'TCKT VTJ', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'c', N'THÊU MẪU', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'TQM', N'TQM', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'TTHTLINHTRUNG', N'TTHT LINH TRUNG', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'WASHLINHTRUNG', N'WASH LINH TRUNG', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'XNK', N'XNK', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0),
    (NEWID(), 'XUONGMAYMAU', N'XƯỞNG MAY MẪU', 'PhongBan', NULL, NULL, 5615, GETDATE(), 5615, GETDATE(), 0);

COMMIT TRAN;
END
GO
