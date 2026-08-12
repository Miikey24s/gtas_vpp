# LVTN-PRESENTATION-001 — Deck bảo vệ khóa luận GTAS VPP

> Trạng thái: `ACTIVE — OWNER CONTENT/VISUAL REVIEW`
> Cập nhật: `2026-08-13`
> Deck hiện hành trong repository: `presentation/SlideBaoVe.pptx`
> Bản nộp hiện hành: `GTAS_VPP_Submission_NguyenAnNam_DH52201078/SlideBaoVe.pptx`
> Các visual draft cũ đã được loại khỏi repository sau khi deck hiện hành được owner chọn.
> Figma deck: `https://www.figma.com/slides/WBVQyZzDDI9FUnUKG0ol4d`
> Phạm vi: lập kế hoạch, nghiên cứu, cập nhật nội dung và kiểm chứng deck bảo vệ; không thay đổi nguồn chuẩn Word trong plan này.

## 0. Bản một ánh nhìn

### Mục tiêu

Tạo một deck bảo vệ khoảng **10–12 phút, tối đa 15 slide chính**, giúp hội đồng hiểu rằng GTAS VPP không chỉ là một biểu mẫu đặt hàng mà là một quy trình nội bộ có:

- đặt hàng linh hoạt theo nhiều kỳ;
- phân quyền theo vai trò và phạm vi;
- quản lý bảng giá, nhà cung cấp và chốt kỳ;
- lưu lịch sử, bản chốt và điều chỉnh có truy vết;
- giá trị vận hành thực tế và demo các luồng chính.

### Đối tượng và công việc truyền thông

- **Đối tượng:** thầy cô, giảng viên và hội đồng trường đại học; mức độ chuyên sâu kỹ thuật có thể khác nhau.
- **Deck cần làm:** giải thích rõ vấn đề, cách giải quyết, điểm khác biệt, kiến trúc, kết quả và mức độ hiểu hệ thống của sinh viên.
- **Deck không làm:** bán sản phẩm, mô phỏng pitch deck doanh nghiệp, chép lại toàn bộ luận văn hoặc khoe nhiều màn hình nhưng không giải thích giá trị.
- **Thông điệp cuối:** GTAS VPP là một quy trình đặt hàng văn phòng phẩm theo kỳ có kiểm soát, giữ được lịch sử dữ liệu và có bằng chứng triển khai thực tế.

### Quyết định thiết kế

| Hạng mục | Quyết định |
|---|---|
| Tỷ lệ | `16:9` |
| Hướng hình ảnh | OpenAI-inspired theo hướng editorial: chữ lớn, khoảng trắng rộng, bố cục bất đối xứng và hình thật; không dùng logo/wordmark OpenAI |
| Môi trường ưu tiên | Phòng học sáng, máy chiếu phổ thông, màu có thể bị bạc |
| Nền | Trắng ngà `#F7F7F4`; dùng peach/blue/mint/yellow rất nhạt để phân đoạn |
| Chữ | Đen gần tuyệt đối `#111111`; Arial fallback; tiêu đề lớn, đậm, ngắn |
| Màu nhấn | Xanh ngọc `#10A37F` cho section label, số thứ tự và đường nhấn; không dùng như màu nhận diện chính thức của OpenAI |
| Cỡ chữ body | Mục tiêu `22–24 pt`; nội dung quan trọng không dưới `20 pt` |
| Ảnh giao diện | Một ảnh chính/slide, crop đúng vùng cần nói, có tối đa 2 callout |
| Animation | Không dùng hoặc chỉ `Fade` nhẹ, không dùng hiệu ứng bay/zoom/trang trí |
| Số slide | Tối đa 15 slide chính + phụ lục hỏi đáp |
| Nguồn | Mọi claim ngoài dự án và tài sản ngoài repo phải có `[Sources]` trong speaker notes |

### Các phase

1. **Content freeze:** chờ bản Word review và sơ đồ nghiệp vụ được duyệt.
2. **Template audit:** dùng deck hiện có làm visual source, không dựng template khác đè lên.
3. **Storyboard:** khóa 15 slide chính và phụ lục.
4. **Asset pass:** đơn giản hóa use case/sơ đồ chức năng; chụp lại UI route thật.
5. **Authoring:** cập nhật bản sao PPTX, giữ master/layout/typography có chủ đích.
6. **Projector QA:** render, kiểm tra washout, downscale và thử trên máy chiếu thật nếu có thể.
7. **Rehearsal:** chạy bài nói 10–12 phút, có demo và phương án dự phòng.

### Definition of done

- Nội dung deck khớp Word đã duyệt và code hiện tại.
- Không còn mô tả cứng một kỳ/ngày 5 hoặc luồng “mở lại để chốt lại”.
- 15 slide chính kể được một câu chuyện liên tục trong tối đa 12 phút.
- Mọi nội dung chính đọc được ở render `1366×768` và bản mô phỏng bạc màu.
- Không có text overflow, title wrap ngoài ý muốn, placeholder rỗng hoặc ảnh mờ.
- Có PPTX, PDF và bộ ảnh dự phòng; speaker notes có talk track và nguồn.
- Số liệu nghiệp vụ và ảnh giao diện được kiểm chứng/chụp lại ở checkpoint cuối.

---

## 1. Source authority và ranh giới

Thứ tự ưu tiên khi có nội dung mâu thuẫn:

1. Code, API, database và hành vi route thật ở HEAD được duyệt.
2. Bản Word review cuối cùng và các sơ đồ đã được owner duyệt.
3. Hai workbook vận hành thực tế trong `LVTN/data/` cho bằng chứng về hiện trạng Excel và cấu trúc dữ liệu đầu vào; không dùng chúng để ghi đè nghiệp vụ hiện tại của hệ thống.
4. `docs/GTAS-VPP-DEFENSE-GUIDE.md` cho thời lượng, kịch bản nói và demo.
5. Deck PPTX hiện có cho motif, master, layout và nhịp thị giác.
6. Nguồn research bên ngoài chỉ dùng để cải thiện cách trình bày, không thay nghiệp vụ GTAS VPP.

Deck hiện có là **visual source**, không phải authority cho nghiệp vụ. Các câu như “máy chủ xác định kỳ hiện tại và 00:00 ngày 5”, vòng đời bốn trạng thái cũ, số liệu cũ hoặc ảnh UI cũ phải được kiểm chứng lại.

Không sao chép phong cách nhận diện của trường khác. Các deck/template bên ngoài chỉ được dùng để nhận diện pattern tốt/xấu.

### 1.1. Nguồn Excel vận hành thực tế

Hai workbook dưới đây được owner cho phép dùng làm nguồn chứng minh bài toán có thật:

| Nguồn | Bằng chứng đã kiểm tra | Cách dùng trong deck |
|---|---|---|
| `LVTN/data/2024.BẢNG ĐẶT VPP.xlsx` | 4 sheet tháng `T1.2024–T4.2024`; cùng một biểu mẫu tháng lặp lại, mỗi sheet có vùng dữ liệu khoảng 537 dòng và khoảng 543 công thức | Slide 2: minh họa việc tách biểu mẫu theo tháng và phụ thuộc công thức tổng hợp |
| `LVTN/data/DANG KY VPP - CAC DON VI.xlsx` | 55 sheet gồm 2 sheet danh mục/tham chiếu và 53 sheet theo đơn vị; các sheet đơn vị lặp cấu trúc `Bộ phận → Nhóm VPP → Tên VPP → Đơn vị → VAT → Đơn giá → Tháng → Số lượng → Thành tiền` | Slide 2 và phụ lục: chứng minh dữ liệu phân tán theo đơn vị, cần tra cứu giá và tổng hợp tập trung |

Các công thức đại diện đã đối chiếu gồm `VLOOKUP` từ sheet danh mục, `SUBTOTAL` cho số lượng/thành tiền và phép tính `Đơn giá × Số lượng`. Sheet danh mục đồng thời gom số lượng từ nhiều đơn vị. Điều này cho thấy Excel hiện hữu đã giải quyết một phần nhập liệu và tính toán, nhưng chi phí bảo trì tăng mạnh khi cần quản lý nhiều đơn vị, nhiều kỳ, quyền truy cập, trạng thái và lịch sử thay đổi.

**Quy tắc dùng dữ liệu thật khi đưa lên slide**

- Dùng ảnh render/crop trực tiếp từ tệp Excel nguồn để chứng minh bài toán có thật; không dựng bảng mô phỏng thay cho dữ liệu đã được owner cung cấp.
- Giữ ngữ cảnh doanh nghiệp và đơn vị khi nó giúp hội đồng hiểu nguồn dữ liệu; không thêm nhãn kỹ thuật hoặc lời giải thích nội bộ lên canvas.
- Không đưa tên cá nhân, tài khoản, credential hoặc dữ liệu không liên quan đến lập luận của slide.
- Tên file đầy đủ và mô tả vùng dữ liệu được lưu trong speaker notes `[Sources]`, không cần hiện trên slide.
- Không dùng tổng số dòng hoặc tổng số công thức của workbook lớn làm KPI vì có sheet bị phình used range do công thức/format kéo xuống cuối Excel.

---

## 2. Research — slide bảo vệ của sinh viên Việt Nam

### 2.1. Cách đánh giá nguồn

Không có hệ thống công khai đáng tin cậy để xác nhận một deck cụ thể “được hội đồng chấm tốt” chỉ dựa trên giao diện. Plan dùng ba nhóm bằng chứng:

| Mức | Nguồn | Cách dùng |
|---|---|---|
| Cao | Hướng dẫn/mẫu chính thức của khoa, trường đại học | Thời lượng, số slide, cấu trúc học thuật và hình thức tối thiểu |
| Trung bình | Talkshow của giảng viên, chia sẻ của sinh viên/cộng đồng CNTT | Cách kể chuyện, cách phản biện, mật độ chữ và demo |
| Tham khảo thị giác | Ảnh buổi bảo vệ thật và kho mẫu slide | Kiểm tra điều kiện phòng, độ sáng, tỷ lệ màn chiếu và lỗi trình bày thường gặp |

Các nguồn quảng cáo viết thuê, tài liệu không rõ tác giả hoặc tự gắn nhãn “9 điểm/10 điểm” không được dùng làm authority.

### 2.2. Phát hiện chính từ nguồn Việt Nam

1. **10–12 phút và tối đa 15 slide là mốc phù hợp.** Khoa Ngân hàng — Học viện Ngân hàng hướng dẫn trình bày 10–12 phút và tối đa 15 slide. Một số trường khác dùng 10–15 slide cho khung 5–10 phút. Với GTAS VPP, 15 slide chính là upper bound, không phải mục tiêu phải lấp đầy.
2. **Slide cần tóm tắt và dẫn dắt, không phải bản Word thu nhỏ.** Nguồn cộng đồng CNTT lặp lại pattern: mỗi slide một thông điệp, ít chữ, có một sơ đồ hoặc hình đủ rõ để người trình bày giải thích thêm.
3. **Cỡ chữ cộng đồng thường khuyến nghị 20–22 pt cho body trong phòng rộng.** Vì mục tiêu của GTAS VPP là phòng sáng và máy chiếu phổ thông, plan nâng chuẩn body lên 22–24 pt.
4. **Hội đồng quan tâm sinh viên hiểu lựa chọn của mình.** Khi phản biện, câu trả lời nên theo bối cảnh → yêu cầu → phương án → lý do chọn → hạn chế. Slide cần chuẩn bị “điểm tựa” cho các câu hỏi này thay vì chỉ liệt kê công nghệ.
5. **Use case và sơ đồ hệ thống thường xuất hiện trong buổi bảo vệ CNTT thật.** Tuy nhiên ảnh phòng thực tế cũng cho thấy sơ đồ dày chữ rất khó đọc trên màn chiếu. GTAS VPP sẽ dùng bản rút gọn trên slide và đưa bản đầy đủ vào phụ lục.
6. **Phòng bảo vệ thường vẫn bật sáng.** Ảnh từ các khoa CNTT Việt Nam cho thấy đèn phòng còn bật, hội đồng ngồi gần bàn và màn chiếu có hiện tượng màu nhạt. Đây là lý do ưu tiên nền sáng dịu, chữ rất đậm, nét lớn và ảnh crop gần.
7. **Demo cần có phương án dự phòng.** Chuẩn bị sẵn ảnh PNG/PDF, dữ liệu demo ổn định và không phụ thuộc hoàn toàn vào mạng hay mutation rủi ro.
8. **Rubric chấm nội dung ưu tiên khả năng phân tích hơn số lượng chức năng.** Rubric CNTT của HUMG dành điểm cao hơn khi sinh viên giải thích được vì sao giải pháp hợp lý, mô tả rõ quy trình và so sánh với phương án khác.
9. **Hội đồng cần thấy đầu ra, điểm khác biệt và lý do lựa chọn.** Hướng dẫn CNTT của Đại học Mở Hà Nội yêu cầu làm rõ bài toán thực tế, sản phẩm đầu ra, điểm phân biệt với đề tài liên quan và lý do chọn công nghệ/giải pháp.
10. **Tính khoa học, tính mới và khả năng ứng dụng là các tiêu chí lặp lại.** Chương trình của UIT và các báo cáo hội đồng tại HaUI/Đại học Hải Dương đều nhấn mạnh chiều sâu phân tích, tính mới, giá trị thực tiễn và triển vọng phát triển.
11. **Slide phải giúp sinh viên thể hiện kiến thức, không thay sinh viên nói.** Hướng dẫn của HUST SoICT yêu cầu nội dung cô đọng, liền mạch, kết nối với khán giả và thể hiện được kiến thức của người trình bày.

### 2.3. Nguồn Việt Nam đã tham khảo

- [Học viện Ngân hàng — Hướng dẫn bảo vệ KLTN, 10–12 phút và tối đa 15 slide](https://hvnh.edu.vn/bank/vi/thong-bao/huong-dan-sinh-vien-bao-ve-khoa-luan-tot-nghiep-hk-i-2024-2025-1005.html)
- [HUST SoICT — Hướng dẫn trình bày đồ án và bảo vệ tốt nghiệp](https://soict.hust.edu.vn/wp-content/uploads/Huong-dan-trinh-bay-do-an-Bao-ve-tot-nghiep-TS.-Trinh-Van-Chien-1.pdf)
- [Khoa CNTT — Đại học Nguyễn Tất Thành: mẫu slide khóa luận bậc đại học](https://cntt.ntt.edu.vn/hoat-dong/slide-mau-bao-cao_-khoa-cong-nghe-thong-tin-new/)
- [Khoa CNTT — Đại học Công nghệ GTVT: ảnh bảo vệ thật trong phòng sáng, có trình bày use case](https://fit.utt.edu.vn/vi/le-bao-ve-do-an-tot-nghiep-cho-sinh-vien-khoa-69-khoa-cong-nghe-thong-tin.html)
- [Nhóm chuyên môn CNPM — Đại học Xây dựng Hà Nội: ảnh bảo vệ đồ án phần mềm thực tế](https://se.huce.edu.vn/sinh-vien-nhom-chuyen-mon-cnpm-ghi-dau-an-voi-loat-giai-phap-cong-nghe-tai-le-bao-ve-do-an-tot-nghiep-dot-1-k67-1)
- [Talkshow kinh nghiệm KLTN với sinh viên nhiều trường có thành tích tốt](https://thayphongdang.edubit.vn/talkshow-chia-se-kinh-nghiem-lam-va-bao-ve-kltn-voi-sinh-vien-100-truong-dh)
- [123Code — pattern cộng đồng về mật độ chữ, màu và cỡ chữ 20–22 pt](https://123code.net/a-5-mau-slide-do-an-tot-nghiep-cntt-pho-bien-320.html)
- [Cộng đồng đồ án CNTT — một thông điệp chính, một hình/sơ đồ rõ và cách trả lời phản biện](https://doansinhvien24h.vn/blog/kinh-nghiem-thuyet-trinh-do-an-cntt-tu-tin)
- [HUMG — Rubric đánh giá nội dung đồ án tốt nghiệp ngành CNTT](https://daotao.humg.edu.vn/Upload/ctdt/2018_4nam_CDIO/7480201.pdf)
- [Đại học Mở Hà Nội — hướng dẫn nội dung đồ án/luận văn tốt nghiệp ngành CNTT](https://fit.hou.edu.vn/huong-dan-ve-noi-dung-de-cuong-do-an-tot-nghiep-dai-hoc-luan-van-tot-nghiep-thac-si-nganh-cntt/)
- [UIT — chuẩn đầu ra nhấn mạnh chiều sâu, tính khoa học, tính mới và khả năng ứng dụng](https://daa.uit.edu.vn/content/cu-nhan-nganh-thiet-ke-vi-mach-ap-dung-tu-khoa-19-2024?page=3)
- [SICT HaUI — các đề tài được đánh giá cao về tính mới và khả năng ứng dụng](https://sict.haui.edu.vn/vn/tin-tuc/chuc-mung-le-bao-ve-do-an-tot-nghiep-khoa-16/71455)
- [Đại học Hải Dương — hội đồng đánh giá cao tính mới, thực tiễn, khả năng tổng hợp và phản biện](https://uhd.edu.vn/tin-tuc/truong-dai-hoc-hai-duong-to-chuc-danh-gia-khoa-luan-do-an-tot-nghiep-trinh-do-dai-hoc-cao-dang-chinh-quy-nam-2020-%28dot-1%29-post5YPx4eemGMuB92501n3x)

### 2.4. Hội đồng cần nhìn thấy gì trên deck GTAS VPP

| Điều hội đồng cần đánh giá | Bằng chứng nên trình chiếu | Không cần bê lên |
|---|---|---|
| Bài toán có thật và có ý nghĩa | Excel vận hành thực tế, điểm đau và người dùng chịu ảnh hưởng | Lịch sử dự án dài, mô tả doanh nghiệp chung chung |
| Mục tiêu và đầu ra | Phạm vi rõ, vai trò, quy trình mà sản phẩm giải quyết | Danh sách mọi màn hình hoặc mọi CRUD |
| Lý do chọn giải pháp | Quyết định quan trọng, phương án đã cân nhắc và trade-off | Logo công nghệ, package, endpoint hoặc folder tree |
| Phần đóng góp/cải tiến | Nhiều kỳ độc lập, ngoại lệ có duyệt/phiên bản, dữ liệu giá và chốt có lịch sử | Liệt kê sáu nhóm chức năng như một menu |
| Mức độ hiểu kỹ thuật | Một sơ đồ kiến trúc và một logic bảo vệ dữ liệu có ví dụ cụ thể | Source code, log build, ma trận kiểm thử code |
| Kết quả và khả năng ứng dụng | Ảnh route thật, Before/After, demo một luồng có giá trị | Nhiều screenshot nhỏ hoặc tour toàn bộ giao diện |
| Giới hạn và hướng phát triển | Nêu rõ phần chưa làm và điều kiện để phát triển tiếp | Hứa hẹn chung chung hoặc mô tả chức năng chưa tồn tại |

**Cổng chọn nội dung:** một slide chỉ được giữ ở deck chính nếu giúp trả lời ít nhất một câu hỏi sau:

1. Vì sao bài toán này đáng giải quyết?
2. Sinh viên đã giải quyết phần nào và tạo ra đầu ra gì?
3. Vì sao chọn cách này thay vì phương án khác?
4. Bằng chứng nào cho thấy giải pháp có giá trị và có thể sử dụng?
5. Giới hạn hiện tại là gì và bước tiếp theo hợp lý ra sao?

Nếu một nội dung không trả lời được câu nào, nó phải được chuyển xuống notes/phụ lục hoặc loại bỏ.

---

## 3. Research — trình chiếu trong phòng sáng và máy chiếu phổ thông

### 3.1. Kết luận áp dụng

- Máy chiếu có thể làm màu bị nhạt hơn; không dùng khác biệt màu tinh tế để truyền đạt trạng thái.
- Dùng sans serif quen thuộc như Arial; tránh all-caps dài, italic và underline.
- Duy trì tương phản ít nhất `4.5:1` cho chữ thường và `3:1` cho chữ lớn.
- Nền trắng tinh có thể gây chói; navy trên nền off-white là phương án phù hợp cho phòng sáng.
- Không dùng nền tối cho phần lớn deck vì ambient light làm vùng tối chuyển thành xám và mất chiều sâu.
- Cần thử trên đúng máy chiếu nếu có thể; mô phỏng washout chỉ là QA dự phòng.

### 3.2. Nguồn kỹ thuật đã tham khảo

- [Microsoft — PowerPoint accessibility: font sans serif, màu và độ tương phản](https://support.microsoft.com/en-us/accessibility/powerpoint/make-your-powerpoint-presentations-accessible-to-people-with-disabilities)
- [Microsoft — tạo và trình bày PowerPoint hiệu quả, đọc được từ xa và kiểm tra độ phân giải máy chiếu](https://support.microsoft.com/en-us/powerpoint/tips-for-creating-and-delivering-an-effective-presentation)
- [Cambridge Centre for Teaching and Learning — màu có thể bị washout; khuyến nghị navy trên off-white](https://www.cctl.cam.ac.uk/slide-accessibility-guidance)
- [Monash Teach HQ — contrast 4.5:1 cho chữ thường, 3:1 cho chữ lớn và tách speaker notes khỏi nội dung trình chiếu](https://www.monash.edu/learning-teaching/teachhq/Teaching-practices/using-multimedia/how-to/powerpoint-slides)
- [Epson projector guide — chế độ Presentation/Dynamic được thiết kế cho phòng sáng](https://files.support.epson.com/docid/cpd6/cpd61222.pdf)

---

## 4. Projector-safe visual contract

### 4.1. Palette

| Token | Màu | Cách dùng | Ghi chú |
|---|---:|---|---|
| `Canvas` | `#F7F7F4` | Nền chính | Off-white ấm, giảm chói hơn trắng tinh |
| `Surface` | `#FFFFFF` | Ảnh và vùng chứng cứ | Chỉ dùng khi cần tách screenshot/sơ đồ khỏi nền |
| `Ink` | `#111111` | Tiêu đề, body chính | Đen gần tuyệt đối để chịu washout của máy chiếu |
| `InkSecondary` | `#666660` | Chú thích phụ cần đọc | Xám ấm, không dùng cho thông tin trọng yếu |
| `Accent` | `#10A37F` | Section label, số thứ tự, đường nhấn | Dùng tiết chế; không phải yếu tố nhận diện OpenAI chính thức |
| `Peach` | `#F2E7DA` | Bài toán và ngoại lệ | Nền phân đoạn ấm |
| `Blue` | `#E2ECF5` | Giải pháp và kiến trúc | Nền phân đoạn lạnh |
| `Mint` | `#E2EFE8` | Màn hình hệ thống | Gắn với luồng đã hiện thực |
| `Yellow` | `#F2E8BC` | Before/After và kết quả | Làm nổi bằng chứng so sánh |
| `Danger` | `#B33A3A` | Cảnh báo thật | Chỉ dùng một câu hoặc một chi tiết cần chú ý |

Quy tắc:

- Không dùng opacity thấp cho chữ hoặc đường biểu đồ.
- Không dùng pastel làm nội dung duy nhất để phân biệt trạng thái.
- Không đặt chữ trắng trên `#0EA5E9` cho nội dung nhỏ.
- Cyan sáng được giữ để kế thừa motif hiện tại nhưng chỉ là accent phi văn bản hoặc mảng lớn.
- Dark navy full-slide chỉ dùng tối đa ở slide kết thúc; phải có bản light fallback nếu thử máy chiếu không đạt.

### 4.2. Typography

Font mặc định tiếp tục là **Arial** để ổn định trên máy trường và khớp deck hiện có.

| Loại | Cỡ mục tiêu | Ràng buộc |
|---|---:|---|
| Tên đề tài ở cover | `44–52 pt` | Tối đa 3 dòng, không co chữ để nhét |
| Tiêu đề slide | `34–38 pt` | Một dòng; ưu tiên tiêu đề dạng kết luận |
| Số liệu/kết luận chính | `30–36 pt` | Không dùng quá 2 điểm nhấn/slide |
| Subheading | `24–28 pt` | Ngắn và có thứ bậc rõ |
| Body | `22–24 pt` | Mục tiêu chính cho phòng sáng |
| Bảng, diagram label | `18–20 pt` | Không có thông tin bắt buộc dưới 18 pt |
| Eyebrow/page marker | `14–16 pt` | Chỉ metadata không bắt buộc phải đọc từ cuối phòng |

Quy tắc copy:

- Mỗi slide một claim chính.
- Tối đa khoảng 60–70 từ hiển thị ở slide nội dung; ưu tiên ít hơn.
- Tối đa 5 bullet; mỗi bullet lý tưởng một dòng.
- Không dùng đoạn văn ba dòng trở lên.
- Không dùng viết hoa toàn bộ cho câu dài.
- Không giảm font để cứu layout; phải rút copy hoặc tách slide/phụ lục.

### 4.3. Layout và hình ảnh

- Lề trái/phải nhất quán, ưu tiên bố cục phẳng thay vì grid card giống giao diện web.
- Một ảnh UI chính chiếm khoảng `55–70%` vùng nội dung.
- Crop thanh địa chỉ, tab trình duyệt và vùng không liên quan nếu không cần chứng minh route.
- Screenshot phải từ route thật, dữ liệu ổn định và độ phân giải tối thiểu `1600×900`.
- Ảnh UI sáng cần viền navy/xám đậm `1.5–2 pt` để không hòa vào canvas máy chiếu.
- Chỉ dùng tối đa 2 callout lớn trên một ảnh; không dùng nhiều mũi tên nhỏ.
- Sơ đồ use case/chức năng trên slide là bản rút gọn; bản đầy đủ ở phụ lục hoặc Word.
- Tránh bảng nhiều hơn 6 cột trong slide chính. Nếu cần, chuyển thành so sánh hoặc highlight một hàng.

---

## 5. Audit deck hiện có

Deck nguồn có 15 slide, `16:9`, dùng Arial, navy/cyan/off-white và đã có speaker notes. Motif cơ bản phù hợp nhưng cần cập nhật:

| Slide hiện tại | Vấn đề | Hướng xử lý |
|---|---|---|
| 2 | Còn nói kỳ hiện tại và ngày 5 cố định | Viết lại thành bài toán quản lý nhiều kỳ và cấu hình linh hoạt |
| 3 | Vai trò `DEV` có thể khó hiểu với hội đồng | Hiển thị `Quản trị hệ thống (DEV)` |
| 4 | Vòng đời cũ, thiếu `Scheduled/Draft` và rolling horizon | Thay bằng flow thân thiện, không phô enum kỹ thuật |
| 6 | Bản đồ source không giúp hội đồng hiểu giá trị hoặc quyết định chính | Loại khỏi deck; không trình chiếu folder tree/source map |
| 7 | Bốn card khá giống dashboard | Chuyển thành một flow/logic trực quan, giảm cảm giác “hộp” |
| 8 | Ba screenshot nhỏ | Dùng một hero screenshot và hai callout |
| 9 | Luồng chốt cũ, thiếu cửa sổ 10 ngày và bản chốt tiếp theo | Viết lại theo nghiệp vụ cuối cùng đã duyệt |
| 10 | Ba screenshot và số bản ghi QA có thể stale | Chọn một ảnh quản trị đại diện; số liệu chỉ khóa ở final checkpoint |
| 11 | Ảnh báo cáo lớn nhưng claim chưa rõ | Đặt title dạng kết luận và highlight phạm vi/xuất dữ liệu |
| 12 | Chứa số liệu kiểm thử kỹ thuật không cần thiết với mạch bảo vệ | Bỏ khỏi deck; tái sử dụng layout cho nội dung giá trị/kết quả |
| 15 | Nền navy có thể bạc trong phòng sáng | Giữ motif nhưng chuẩn bị light fallback |

---

## 6. Storyboard 15 slide chính

| # | Thời lượng | Claim chính | Visual ưu tiên |
|---:|---:|---|---|
| 1 | 0:00–0:20 | GTAS VPP quản lý yêu cầu văn phòng phẩm theo kỳ | Cover hiện có, tối giản |
| 2 | 0:20–1:05 | Vấn đề không nằm ở nhập đơn mà ở kỳ, hạn, giá và lịch sử | Một flow Before → Risk, không dùng bốn card nhỏ |
| 3 | 1:05–1:45 | Phạm vi tập trung vào ba vai trò và quy trình nội bộ | Ba vai trò + phạm vi vào/ngoài |
| 4 | 1:45–2:25 | Mỗi vai trò nhìn thấy đúng use case được phép | Use case tổng quát rút gọn |
| 5 | 2:25–3:05 | Phần đóng góp nằm ở ba quyết định giải quyết đúng hạn chế của bảng tính | Ba dòng `vấn đề → quyết định → giá trị`, không dùng danh sách menu |
| 6 | 3:05–4:00 | Nhiều kỳ có thể mở trước nhưng mỗi kỳ giữ lịch riêng | Timeline nhiều kỳ, cấu hình mặc định 3 nhưng linh hoạt |
| 7 | 4:00–5:00 | Đơn bổ sung và điều chỉnh sau chốt đều được kiểm soát | Hai mini-flow đối xứng, mỗi flow 3–4 bước |
| 8 | 5:00–5:50 | Modular monolith phù hợp quy mô luận văn và vẫn tách trách nhiệm | Sơ đồ kiến trúc đơn tuyến |
| 9 | 5:50–6:40 | Dữ liệu đúng nhờ nhiều lớp bảo vệ, không nhờ giao diện | Constraint → transaction → concurrency → snapshot |
| 10 | 6:40–7:30 | Nhân viên đặt và theo dõi đơn theo đúng kỳ đang mở | Một screenshot thật + hai callout |
| 11 | 7:30–8:30 | Quản lý vận hành kỳ từ theo dõi đến chốt | Một screenshot danh mục kỳ hoặc chốt kỳ + flow nhỏ |
| 12 | 8:30–9:20 | Quản trị hệ thống cấu hình chính sách và dữ liệu nền | Một screenshot cấu hình hoặc bảng giá, không ghép ba ảnh nhỏ |
| 13 | 9:20–10:15 | GTAS VPP thay chuỗi bảng tính rời rạc bằng một luồng dữ liệu có kiểm soát | Before/After: Excel thật → màn hình vận hành kỳ và chốt kỳ |
| 14 | 10:15–11:10 | Hệ thống đã hoàn thành luồng chính nhưng vẫn có giới hạn rõ | Kết quả / hạn chế / hướng phát triển theo 3 tầng |
| 15 | 11:10–12:00 | Chuyển sang demo ba điểm có giá trị nhất | Closing + ba mục demo ngắn |

### Bản đồ giá trị đối với hội đồng

| Hội đồng cần đánh giá | Slide đáp ứng |
|---|---|
| Bài toán, ý nghĩa và phạm vi | 2–3 |
| Người dùng và quy trình tổng quát | 4 |
| Điểm đóng góp và lý do chọn giải pháp | 5–9 |
| Sản phẩm có hoạt động và giải quyết việc gì | 10–13 |
| Kết quả, giới hạn và khả năng phát triển | 14 |
| Khả năng trình bày, làm chủ sản phẩm và demo | 15 + phần hỏi đáp |

### Nội dung nghiệp vụ đã khóa từ Word review v2

- rolling horizon mặc định ba kỳ nhưng cấu hình được `0–12`;
- ngày mở, ngày đóng, hạn duyệt bổ sung và cửa sổ điều chỉnh là cấu hình;
- ngày `1–31` được clamp đúng cuối tháng và năm nhuận;
- cửa sổ điều chỉnh mặc định 10 ngày tính từ ngày đóng;
- được chốt sớm nếu không còn đơn bổ sung chờ duyệt;
- kỳ đã chốt không quay lại trạng thái cũ; điều chỉnh hợp lệ tạo bản chốt kế tiếp;
- bảng giá hoạt động ngay khi tạo, có mặc định/vô hiệu hóa/xóa theo guard hiện hành;
- PO/hợp đồng/chi phí vận chuyển và tối ưu nhiều NCC là hướng phát triển, chưa trình bày như chức năng đã hoàn thiện.

### Nội dung cố ý không đưa vào deck

- Số lượng unit test, integration test, Playwright test hoặc code coverage.
- Ma trận viewport, log build/verify và chi tiết công cụ kiểm thử.
- Slide hoặc phụ lục riêng về kiểm thử code.

Các kiểm tra này vẫn được chạy như QA nội bộ trước khi bàn giao, nhưng không xuất hiện trên canvas, phụ lục hay mạch nói chính.

### P1 content map — `READY FOR OWNER REVIEW`

Nguồn nội dung chính của phần này là `LVTN/checkpoints/NguyenAnNam_DH52201078_review_20260811_v2.docx`. Hai workbook thực tế trong `LVTN/data/` bổ sung bằng chứng cho hiện trạng ở Slide 2; chúng không thay source authority của nghiệp vụ. Copy dưới đây là nội dung dự kiến xuất hiện trên slide; talk track là phần người trình bày nói và sẽ được đặt trong speaker notes, không hiển thị lên canvas.

#### Slide 1 — Trang bìa

**Tiêu đề hiển thị**

> XÂY DỰNG WEBSITE QUẢN LÝ
> VĂN PHÒNG PHẨM PHONG PHÚ

**Nội dung hiển thị**

- Luận văn tốt nghiệp — Khoa Công nghệ Thông tin.
- Sinh viên: **Nguyễn An Nam** — MSSV: **DH52201078**.
- Giảng viên hướng dẫn: **ThS. Viên Thanh Nhã**.
- Thành phố Hồ Chí Minh — 2026.

**Talk track**

> Kính thưa quý thầy cô, em xin trình bày luận văn xây dựng website quản lý văn phòng phẩm Phong Phú, tên hệ thống là GTAS VPP. Đề tài tập trung vào quy trình yêu cầu văn phòng phẩm theo kỳ trong doanh nghiệp, từ đặt hàng đến chốt kỳ và báo cáo.

**Nguồn:** bìa Word; deck nguồn slide 1.
**Visual:** giữ bố cục cover hiện tại, không thêm agenda hoặc danh sách công nghệ.

#### Slide 2 — Bài toán không dừng ở việc nhập số lượng

**Tiêu đề hiển thị**

> Bài toán không dừng ở việc nhập số lượng

**Nội dung hiển thị**

- File thực tế có **53 sheet theo đơn vị** và 2 sheet danh mục/tham chiếu.
- Một biểu mẫu khác được tách thành **4 sheet theo tháng** với cấu trúc lặp lại.
- Đơn giá, VAT và thành tiền phụ thuộc nhiều công thức tra cứu, tính và tổng hợp.
- Khi thêm kỳ, quyền, trạng thái và lịch sử, doanh nghiệp cần một quy trình tập trung.

**Talk track**

> Hai workbook thực tế cho thấy Excel đã hỗ trợ danh mục, tra giá và tính thành tiền, nhưng dữ liệu vẫn được chia theo tháng và theo từng đơn vị. Khi quy trình cần nhiều kỳ, duyệt bổ sung, phân quyền và lưu lịch sử chốt, việc chỉ mở rộng thêm sheet và công thức sẽ ngày càng khó kiểm soát.

**Câu hỏi có thể gặp:** Vì sao không tiếp tục dùng Excel hoặc Google Form?
**Ý trả lời:** Excel nhập liệu và tính toán tốt; chính hai file thực tế cũng chứng minh điều đó. Điểm hệ thống bổ sung là máy trạng thái, quyền theo phạm vi, ngăn trùng đồng thời, phiên bản dữ liệu và lịch sử chốt.

**Nguồn:** Word 1.1.1; 1.2.1–1.2.7; hai workbook trong `LVTN/data/`.
**Visual:** collage từ ba vùng Excel thật, kèm hai callout `53 trang tính theo đơn vị` và `4 trang tính theo tháng`; chỉ giữ các chi tiết giúp chứng minh dữ liệu phân tán và lỗi công thức, không dùng bốn card độc lập.

#### Slide 3 — GTAS VPP chuẩn hóa quy trình nội bộ theo kỳ

**Tiêu đề hiển thị**

> GTAS VPP chuẩn hóa quy trình nội bộ theo kỳ

**Nội dung hiển thị**

**Trong phạm vi**

- Đặt hàng theo nhiều kỳ đang mở.
- Đơn bổ sung và luồng duyệt riêng.
- Danh mục, nhà cung cấp và bảng giá.
- Vận hành, chốt kỳ, báo cáo và phân quyền.

**Ba vai trò**

- Nhân viên.
- Quản lý.
- Quản trị hệ thống `(DEV)`.

**Ngoài phạm vi**

> Thanh toán · tồn kho · PO/hợp đồng · tối ưu chia đơn nhiều nhà cung cấp

**Talk track**

> Phạm vi luận văn là hệ thống web nội bộ. Đề tài chưa thay thế hệ thống mua hàng hoặc kho; đầu ra hiện tại dừng ở kết quả chốt nhu cầu có giá và lịch sử để mở rộng sang mua sắm sau này.

**Nguồn Word:** 1.1.2; 1.3.1–1.3.2.
**Visual:** hai vùng `Trong phạm vi / Ngoài phạm vi`, ba vai trò nằm giữa; tránh biến thành danh sách chức năng dài.

#### Slide 4 — Ba vai trò dùng cùng hệ thống nhưng khác phạm vi

**Tiêu đề hiển thị**

> Ba vai trò dùng cùng hệ thống nhưng khác phạm vi

**Nội dung hiển thị cạnh sơ đồ**

- **Nhân viên:** tạo, theo dõi và điều chỉnh đơn của mình.
- **Quản lý:** duyệt bổ sung, vận hành và chốt từng kỳ.
- **Quản trị hệ thống:** tài khoản, quyền và cấu hình mặc định.

**Talk track**

> Nhân viên chỉ thao tác trên dữ liệu cá nhân. Quản lý có thêm dữ liệu phòng ban hoặc toàn công ty theo quyền. Quản trị hệ thống quản lý tài khoản, quyền và chính sách tạo kỳ nhưng không được bỏ qua các ràng buộc nghiệp vụ.

**Câu hỏi có thể gặp:** DEV có toàn quyền và sửa được mọi dữ liệu không?
**Ý trả lời:** DEV quản trị truy cập và cấu hình; các API nghiệp vụ vẫn kiểm tra trạng thái, phạm vi, transaction và constraint.

**Nguồn Word:** Hình 2-3; 2.3.1; 4.1.1–4.1.3.
**Visual:** vẽ lại bản rút gọn từ `LVTN/diagrams/ch02/use-case-overview.svg`; bản đầy đủ để trong phụ lục.

#### Slide 5 — Ba quyết định biến biểu mẫu thành quy trình có kiểm soát

**Tiêu đề hiển thị**

> Ba quyết định biến biểu mẫu thành quy trình có kiểm soát

**Nội dung hiển thị**

1. **Kỳ độc lập:** mở trước nhiều tháng nhưng mỗi kỳ giữ lịch và trạng thái riêng.
2. **Ngoại lệ có kiểm soát:** đơn bổ sung hoặc sửa sau chốt phải đi qua luồng duyệt, không ghi đè lịch sử.
3. **Giá trị có căn cứ:** bảng giá và kết quả chốt được lưu theo thời điểm để truy vết về sau.

**Talk track**

> Điểm đóng góp của đề tài không nằm ở số lượng màn hình. Ba quyết định này giải quyết trực tiếp hạn chế của bảng tính: kỳ không còn bị suy cứng, ngoại lệ không được sửa tùy ý và kết quả tài chính luôn có căn cứ lịch sử.

**Câu hỏi có thể gặp:** Điểm cải tiến chính của đề tài so với một biểu mẫu web thông thường là gì?
**Ý trả lời:** hệ thống quản lý vòng đời và căn cứ dữ liệu, không chỉ lưu dòng đơn hàng; nhiều kỳ, ngoại lệ và bản chốt đều có quy tắc riêng.

**Nguồn Word:** 1.2.1–1.2.7; 2.3.1.
**Visual:** ba dòng liên tục `hạn chế Excel → quyết định thiết kế → giá trị`, không dùng ba card hoặc sơ đồ phân rã chức năng. Bản phân rã đầy đủ chỉ để phụ lục.

#### Slide 6 — Mỗi kỳ có lịch riêng và có thể mở trước nhiều kỳ

**Tiêu đề hiển thị**

> Mỗi kỳ có lịch riêng và có thể mở trước nhiều kỳ

**Nội dung hiển thị**

- Mặc định duy trì **3 kỳ**, có thể cấu hình từ **0 đến 12**.
- Ngày mở và đóng từ **1 đến 31**; tháng ngắn dùng ngày cuối tháng.
- Mỗi kỳ lưu ngày mở, ngày đóng và hạn duyệt bổ sung riêng.
- Cấu hình mới chỉ áp dụng cho **các kỳ được tạo sau**.

**Nhãn trạng thái hiển thị**

> Sắp mở → Đang mở → Đã đóng → Đang chốt → Đã chốt

`Nháp` là trạng thái chuẩn bị nội bộ và không cần nhấn mạnh trong luồng nói chính.

**Talk track**

> Trước đây bài toán chỉ suy một kỳ theo ngày 5. Phiên bản mới lưu kỳ thành thực thể độc lập, nên tháng 8 có thể đồng thời mở kỳ tháng 8, 9 và 10. Ngày 29 đến 31 được xử lý theo ngày cuối tháng và đúng năm nhuận.

**Câu hỏi có thể gặp:** Nếu cấu hình số kỳ bằng 0 thì sao?
**Ý trả lời:** scheduler không tạo thêm kỳ mới; các kỳ đã tồn tại giữ nguyên lịch và dữ liệu.

**Nguồn Word:** 1.2.1; 2.3.1.5.
**Visual:** timeline ba kỳ `08/2026 · 09/2026 · 10/2026`, mỗi kỳ có mốc đóng khác nhau; không dùng ảnh form cấu hình ở slide này.

#### Slide 7 — Ngoại lệ được xử lý bằng luồng riêng, không sửa lịch sử

**Tiêu đề hiển thị**

> Ngoại lệ được xử lý bằng luồng riêng, không sửa lịch sử

**Nội dung hiển thị**

**Đơn bổ sung**

- Có lý do và giới hạn số lần.
- Chuyển sang trạng thái chờ duyệt.
- Hạn duyệt mặc định: **5 ngày sau ngày đóng**.

**Điều chỉnh sau chốt**

- Yêu cầu sửa/hủy phải có lý do.
- Một Quản lý khác xác nhận.
- Tạo **bản chốt tiếp theo**, giữ nguyên bản cũ.

**Dòng kết luận**

> Không mở lại hoặc ghi đè kết quả đã chốt.

**Talk track**

> Đơn bổ sung giải quyết nhu cầu phát sinh nhưng vẫn có quota và hàng chờ duyệt. Nếu dữ liệu đã được chốt cần sửa, hệ thống dùng nguyên tắc hai người quản lý và tạo phiên bản mới thay vì sửa trực tiếp lịch sử.

**Câu hỏi có thể gặp:** Vì sao phải cần Quản lý thứ hai?
**Ý trả lời:** để người tạo yêu cầu không tự phê duyệt thay đổi tài chính và để lại bằng chứng kiểm tra độc lập.

**Nguồn Word:** 1.2.3; 1.2.4; 2.3.1.4; 2.3.1.9; Hình 3-10, 3-11, 3-14.
**Visual:** hai mini-flow ba bước, không đặt hai sơ đồ UML đầy đủ cạnh nhau.

#### Slide 8 — Modular monolith tách trách nhiệm nhưng triển khai gọn

**Tiêu đề hiển thị**

> Modular monolith tách trách nhiệm nhưng triển khai gọn

**Nội dung hiển thị trong luồng**

> Trình duyệt → Blazor Interactive Server → ASP.NET Core Web API → Application/Domain → EF Core → SQL Server

**Dòng phụ**

> Aspire cho local · Docker Compose và Nginx cho triển khai

**Lý do chọn**

> Triển khai gọn · giữ transaction nhất quán · vẫn tách trách nhiệm rõ

**Talk track**

> Em chọn modular monolith vì quy mô luận văn chưa cần microservices. Cách này vẫn tách phần giao diện, API, điều phối nghiệp vụ và domain, nhưng triển khai đơn giản hơn và giữ transaction nhất quán.

**Câu hỏi có thể gặp:** Vì sao không dùng microservices?
**Ý trả lời:** chưa có nhu cầu scale độc lập hoặc đội vận hành nhiều dịch vụ; microservices sẽ tăng chi phí mạng, quan sát, đồng bộ dữ liệu và triển khai vượt lợi ích của đề tài.

**Nguồn Word:** 2.2; Hình 2-1.
**Visual:** dùng bản đơn tuyến rút gọn từ `LVTN/diagrams/ch02/architecture-overview.svg`; chi tiết container và network để phụ lục.

#### Slide 9 — Dữ liệu được bảo vệ ở backend và database

**Tiêu đề hiển thị**

> Dữ liệu được bảo vệ ở backend và database

**Nội dung hiển thị**

1. **Policy và phạm vi phiên** — không tin phạm vi do giao diện tự gửi.
2. **Unique/Check constraint** — chặn trùng và dữ liệu sai.
3. **Transaction + RowVersion** — lưu đồng bộ và phát hiện ghi đè.
4. **Idempotency + phiên bản bất biến** — chống xử lý lặp, giữ lịch sử.

**Talk track**

> Việc ẩn hoặc làm mờ một nút chỉ hỗ trợ trải nghiệm người dùng. Quyền và ràng buộc thật được kiểm tra lại ở API và database. Ví dụ, hai yêu cầu đồng thời vẫn không thể tạo hai đơn thường trong cùng một kỳ.

**Câu hỏi có thể gặp:** RowVersion khác transaction như thế nào?
**Ý trả lời:** transaction bảo đảm một nhóm thao tác cùng thành công hoặc cùng thất bại; RowVersion phát hiện dữ liệu đã bị người khác thay đổi trước khi ghi.

**Nguồn Word:** 1.2.2; 1.2.5–1.2.7; Phụ lục D.
**Visual:** một chuỗi bốn lớp bảo vệ, tránh bố cục bốn card dashboard.

#### Slide 10 — Nhân viên chọn đúng kỳ trước khi tạo đơn

**Tiêu đề hiển thị**

> Nhân viên chọn đúng kỳ trước khi tạo đơn

**Callout hiển thị trên ảnh**

1. Chọn một kỳ đang mở.
2. Tạo mới hoặc sao chép đơn gần nhất.
3. Theo dõi đơn thường, đơn bổ sung và lịch sử.

**Talk track**

> Backend trả danh sách các kỳ đang mở và capability của từng thao tác. Nhân viên chọn kỳ đích, sau đó tạo hoặc sao chép đơn; dữ liệu luôn được kiểm tra lại theo PeriodId và lịch riêng của kỳ.

**Nguồn Word:** 2.3.1.2–2.3.1.4; Hình 3-29, 3-30, 3-31.
**Visual:** một screenshot hero từ không gian Đơn hàng của tôi; tối đa hai hoặc ba callout lớn, không ghép ba screenshot nhỏ.

#### Slide 11 — Quản lý vận hành từng kỳ trước khi xác nhận chốt

**Tiêu đề hiển thị**

> Quản lý vận hành từng kỳ trước khi xác nhận chốt

**Nội dung hiển thị**

> Theo dõi kỳ → Duyệt đơn bổ sung → Chọn nhà cung cấp và bảng giá → Xử lý điều kiện chặn → Xác nhận chốt

**Thông tin tài chính cần gọi tên rõ**

- Giá trị trước VAT.
- Thuế VAT.
- Tổng giá trị đã gồm VAT.

**Talk track**

> Quản lý không chốt trực tiếp từ danh sách đơn. Hệ thống tạo bản xem trước, rà soát đơn bổ sung chờ xử lý và mặt hàng thiếu giá. Khi xác nhận, backend tính lại dữ liệu và lưu snapshot giá cùng phân bổ theo phiên bản.

**Câu hỏi có thể gặp:** Có thể chốt sớm không?
**Ý trả lời:** có, nếu kỳ đã đóng và không còn đơn bổ sung chờ duyệt; thời gian điều chỉnh đơn mặc định vẫn tính từ ngày đóng, không tính từ lúc chốt.

**Nguồn Word:** 2.3.1.7–2.3.1.9; Hình 3-35, 3-37.
**Visual:** dùng Hình 3-37 hoặc ảnh chốt kỳ route thật làm hero; bảng giá chỉ xuất hiện như lựa chọn đầu vào, không trình bày toàn bộ grid.

#### Slide 12 — Chính sách tạo kỳ được quản trị bằng phiên bản

**Tiêu đề hiển thị**

> Chính sách tạo kỳ được quản trị bằng phiên bản

**Nội dung hiển thị cạnh ảnh**

- Số kỳ mở trước: **3**, cấu hình `0–12`.
- Ngày mở/đóng mặc định: **ngày 5**, cấu hình `1–31`.
- Duyệt đơn bổ sung: **5 ngày** sau ngày đóng.
- Điều chỉnh đơn: **10 ngày** kể từ ngày đóng.
- Mỗi lần lưu tạo phiên bản mới cho **các kỳ tương lai**.

**Talk track**

> Cấu hình hệ thống chỉ là mặc định khi tạo kỳ mới. Hệ thống không sửa ngược lịch của kỳ đã tồn tại, nhờ đó thay đổi chính sách không làm thay đổi ý nghĩa của dữ liệu lịch sử.

**Câu hỏi có thể gặp:** Vì sao cấu hình không cập nhật toàn bộ kỳ đang mở?
**Ý trả lời:** mỗi kỳ có thể đã có đơn và lịch đã được thông báo; sửa hàng loạt sẽ làm thay đổi deadline ngoài dự kiến. Quản lý vẫn có thao tác riêng trên từng kỳ khi capability cho phép.

**Nguồn Word:** 1.2.1; 2.3.1.5; Hình 3-36.
**Visual:** `order-period-settings-1920x1080.png`, crop phần form chính; không hiển thị toàn bộ sidebar nếu chữ bị nhỏ.

#### Slide 13 — Từ nhiều bảng tính đến một luồng dữ liệu có kiểm soát

**Tiêu đề hiển thị**

> Từ nhiều bảng tính đến một luồng dữ liệu có kiểm soát

**Nội dung hiển thị**

- **Trước:** biểu mẫu tách theo tháng và đơn vị, phụ thuộc nhiều công thức tra cứu và tổng hợp.
- **Sau:** danh mục, kỳ, đơn hàng và bảng giá dùng chung trong một hệ thống.
- Quyền truy cập, trạng thái và lịch sử thay đổi được kiểm soát xuyên suốt.

**Talk track**

> Điểm chính không phải là chuyển nguyên biểu mẫu Excel lên web. Hệ thống chuẩn hóa dữ liệu dùng chung và kiểm soát cả vòng đời: ai được đặt, đặt cho kỳ nào, dùng bảng giá nào, đơn đang ở trạng thái nào và thay đổi nào đã xảy ra.

**Câu hỏi có thể gặp:** Có phải hệ thống sẽ loại bỏ hoàn toàn Excel không?
**Ý trả lời:** không. Hệ thống dùng để vận hành và giữ dữ liệu chuẩn; Excel/PDF vẫn hữu ích cho xuất báo cáo, đối chiếu và chuyển tiếp dữ liệu khi cần.

**Nguồn:** hai workbook thực tế trong `LVTN/data/`; Word 1.1.1, 5.1–5.2.
**Visual:** một composition Before → After; bên trái là bảng tổng hợp Excel thật có lỗi `#REF!`, bên phải là hai màn hình vận hành kỳ và chốt kỳ. Không thêm nhãn kỹ thuật nội bộ hoặc KPI card.

#### Slide 14 — Các mục tiêu chính đã đạt, phạm vi phát triển vẫn rõ ràng

**Tiêu đề hiển thị**

> Các mục tiêu chính đã đạt, phạm vi phát triển vẫn rõ ràng

**Nội dung hiển thị**

**Đã hoàn thành**

- Nhiều kỳ mở đồng thời và lịch riêng.
- Đơn thường, bổ sung và điều chỉnh có phiên bản.
- Bảng giá, chốt kỳ, phân quyền và báo cáo.
- Dữ liệu được tập trung, tra cứu và xuất theo đúng phạm vi.

**Hướng phát triển**

- Dữ liệu vận hành dài hạn và theo dõi hiệu năng.
- PO, hợp đồng và chi phí vận chuyển.
- So sánh và tối ưu nhu cầu cho nhiều nhà cung cấp.

**Dòng kết luận**

> Quy trình chính đã được hiện thực đầy đủ trong phạm vi luận văn.

**Talk track**

> Khi đối chiếu với mục tiêu ban đầu, các nhóm kết quả trong luận văn đều đạt. Tuy nhiên, hệ thống chưa phải giải pháp mua sắm trọn vòng đời; tối ưu nhiều nhà cung cấp chỉ hợp lý sau khi dữ liệu báo giá, vận chuyển và cam kết giao hàng được chuẩn hóa.

**Nguồn Word:** Bảng 5-1; 5.2; 5.3.
**Visual:** hai vùng `Đã hoàn thành / Hướng phát triển` và một câu kết luận, không dùng roadmap bốn bước dài.

#### Slide 15 — GTAS VPP khép kín yêu cầu, chốt kỳ và báo cáo

**Tiêu đề hiển thị**

> GTAS VPP khép kín yêu cầu → chốt kỳ → báo cáo

**Nội dung hiển thị**

> Tập trung dữ liệu · kiểm soát quyền · giữ lịch sử thay đổi

**Ba điểm chuyển sang demo**

1. Nhân viên đặt hàng theo kỳ.
2. Quản lý duyệt đơn bổ sung.
3. Chốt kỳ và xem báo cáo theo phạm vi.

**Talk track**

> Tóm lại, GTAS VPP đã chuẩn hóa luồng yêu cầu văn phòng phẩm theo nhiều kỳ và giữ được lịch sử ở các bước quan trọng. Sau đây em xin demo ngắn ba điểm đại diện của hệ thống.

**Nguồn Word:** 5.1–5.3; đoạn tổng kết Chương 5.
**Visual:** ưu tiên bản closing nền sáng cho máy chiếu; giữ bản navy làm phương án nếu projector test đạt.

### Owner review checklist

Owner chỉ cần review **5 điểm nội dung chính** dưới đây; nội dung kiểm thử code đã được loại khỏi toàn bộ deck:

| Mã | Cần kiểm tra | Câu hỏi duyệt |
|---|---|---|
| R1 | Bài toán và giá trị | Slide 2 và 13 đã dùng Excel thật đúng mức, đủ thuyết phục nhưng không lộ dữ liệu nội bộ chưa? |
| R2 | Nghiệp vụ kỳ đặt hàng | Các giá trị `3 kỳ · ngày 5 · duyệt bổ sung 5 ngày · chỉnh đơn 10 ngày`, chốt sớm và bản chốt tiếp theo đã đúng ý cuối cùng chưa? |
| R3 | Vai trò và quyền | Cách gọi `Quản trị hệ thống (DEV)`, phạm vi Nhân viên/Quản lý và quy tắc hai quản lý có dễ hiểu, đúng nghiệp vụ không? |
| R4 | Kết quả và giới hạn | Phần đã hoàn thành có phản ánh đúng sản phẩm; PO/hợp đồng/vận chuyển/tối ưu nhiều NCC đã được đặt đúng ở hướng phát triển chưa? |
| R5 | Mạch thuyết trình | Mỗi slide có giúp hội đồng đánh giá `bài toán · lựa chọn · đóng góp · kết quả · giới hạn` không; còn slide nào chỉ đang liệt kê chức năng không? |

### P1 acceptance

- [x] Copy hiển thị đã viết theo ngôn ngữ dành cho hội đồng, không phải ghi chú kỹ thuật nội bộ.
- [x] Mỗi slide có một claim chính và một visual ưu tiên.
- [x] Nội dung nhiều kỳ, chốt kỳ, correction và bảng giá khớp Word review v2.
- [x] Không mô tả PO, hợp đồng hoặc tối ưu nhiều NCC như chức năng đã hoàn thành.
- [x] Nội dung kiểm thử code, test count và viewport đã được loại khỏi slide chính, phụ lục và mạch nói.
- [x] Hai workbook thực tế đã được audit ở chế độ chỉ đọc và đưa ảnh render thật vào Slide 2/13.
- [ ] Owner duyệt R1–R5.

---

## 7. Phụ lục phục vụ phản biện

Các slide phụ lục không tính vào 15 slide chính và chỉ mở khi hội đồng hỏi:

1. Use case tổng quát và sơ đồ phân rã chức năng đầy đủ.
2. Kiến trúc chi tiết và các lớp bảo vệ dữ liệu chính.
3. Vòng đời kỳ, chốt kỳ và nguyên tắc tạo bản tiếp theo.
4. Ma trận quyền `EMPLOYEE / MANAGER / DEV`.
5. Đối chiếu dữ liệu thực tế: `53 sheet đơn vị + danh mục/list → danh mục, đơn, kỳ và bảng giá trong hệ thống`.
6. Bộ screenshot demo dự phòng.

---

## 8. Speaker notes và cách trả lời phản biện

Mỗi slide cần có speaker notes gồm:

- câu mở slide;
- tối đa ba ý cần nói;
- câu chuyển sang slide tiếp theo;
- câu hỏi phản biện có thể gặp;
- câu trả lời theo cấu trúc `bối cảnh → yêu cầu → lựa chọn → lý do → giới hạn`;
- block `[Sources]` cho claim/asset bên ngoài.

Không đưa timing, hướng dẫn sản xuất slide hoặc nguồn research lên canvas trình chiếu.

---

## 9. Projector QA và acceptance

### 9.1. Render QA

- Render mọi slide ở kích thước chuẩn `1600×900` hoặc cao hơn.
- Xem từng slide riêng ở full size; contact sheet chỉ dùng kiểm tra nhịp toàn deck.
- Downscale về `1366×768` để mô phỏng laptop/máy chiếu phổ thông.
- Tạo bản mô phỏng washout bằng cách làm sáng render khoảng 20–25%; đây là heuristic, không thay thế test máy chiếu thật.
- Kiểm tra grayscale để bảo đảm trạng thái không phụ thuộc duy nhất vào màu.

### 9.2. Projector acceptance

- Title đọc được ngay từ thumbnail 25%.
- Body chính vẫn đọc được khi xem toàn màn hình từ khoảng cách xa tương đối.
- Các đường sơ đồ và table border không biến mất trong bản washout.
- Không có chữ cyan sáng trên trắng/off-white.
- Không có screenshot chứa text UI quá nhỏ để hội đồng cần nheo mắt.
- Dark closing slide phải pass trên máy chiếu thật; nếu không, dùng light fallback.

### 9.3. Hardware rehearsal

Ưu tiên thử một lần tại phòng học hoặc máy chiếu có chất lượng tương đương:

- bật đèn như lúc bảo vệ;
- kiểm tra góc nhìn từ cuối phòng;
- kiểm tra màu xanh, xám và ảnh UI có bị bạc;
- xác nhận tỷ lệ `16:9`, không bị stretch hoặc crop;
- chạy Presenter View và bút trình chiếu;
- thử mở file PPTX và PDF trên laptop dự phòng.

### 9.4. Functional QA

- PowerPoint mở không repair file.
- Không có placeholder rỗng trong PPTX XML.
- Không có overlap, clipping hoặc title wrap ngoài ý muốn.
- Ảnh không bị vỡ, kéo méo hoặc crop sai.
- Speaker notes và `[Sources]` còn nguyên sau export.
- PDF và PNG có cùng thứ tự/nội dung với PPTX.

---

## 10. Dependency, conflict và Git boundary

- Chỉ bắt đầu khóa nội dung sau khi task Word đạt content freeze.
- Task presentation không chỉnh Word, code, migration hoặc sơ đồ nguồn đang được task khác chỉnh nếu chưa phối hợp.
- Nếu cần dùng sơ đồ từ Word, chờ bản SVG cuối rồi tạo bản rút gọn riêng cho slide.
- Source PPTX luôn được giữ nguyên; output là một bản copy/review mới.
- Trước implementation phải xác nhận branch/worktree thật sự tách biệt. Tại thời điểm lập plan, terminal vẫn báo branch `Nam` và worktree có nhiều thay đổi ngoài presentation.
- Không stage/commit thay đổi ngoài `docs/execution/LVTN-PRESENTATION-001.md`, `presentation/` và asset presentation được phê duyệt.

---

## 11. Execution checkpoints

| Checkpoint | Output | Approval gate |
|---|---|---|
| P0 — Research + plan | `COMPLETED` — research, motif, palette và projector contract | Đã ghi vào living plan |
| P1 — Content map | `COMPLETED FOR DRAFT 1` — copy, talk track, nguồn và Q&A cho 15 slide | Chuyển sang review trực quan |
| P2 — Template starter | `COMPLETED` — map đủ 15 slide, giữ theme/master/layout nguồn | Template fidelity: pass, 0 issue |
| P3 — Visual Draft 3 | `REJECTED BY OWNER` — mass-restyle bằng code làm visual kém hơn bản đầu | Không tiếp tục dùng làm authority thiết kế |
| P3B — Figma style frames | `COMPLETED — OWNER APPROVED` — 3 slide mẫu: Cover, Excel problem, Before/After | Art direction đã được dùng cho full deck |
| P4 — Visual final | `COMPLETED FOR OWNER REVIEW` — đủ 15 slide trong Figma, dùng ảnh Excel/UI thật | Owner review nội dung, độ dễ đọc và nhịp kể chuyện |
| P5 — Evidence final | `COMPLETED` — 15/15 slide giữ speaker notes và block nguồn | Notes được kiểm tra lại trong PPTX |
| P6 — Projector QA | `IN PROGRESS` — đã có PPTX Complete chỉnh sửa được và bản pixel-perfect dự phòng; còn washout, PDF và rehearsal | Owner duyệt bản bảo vệ |

### Biên nhận Visual Draft 1 (lịch sử, file nháp đã dọn)

- PPTX: `presentation/GTAS_VPP_Thesis_Defense_NguyenAnNam_visual_draft_v1.pptx`.
- Đã render và xem đủ 15 slide; không phát hiện nội dung tràn canvas.
- `slides_test.py`: pass.
- Template fidelity: pass, `0 issue`.
- Slide 8 dùng sơ đồ kiến trúc rút gọn; Slide 10–13 dùng screenshot/evidence thật trong repository.
- Nội dung kiểm thử code không xuất hiện trên slide hoặc speaker notes chính.

### Biên nhận Visual Draft 2 (lịch sử, file nháp đã dọn)

| Nội dung | Đã lưu ở | Phạm vi/hiệu lực | Bằng chứng |
|---|---|---|---|
| Bỏ cách gọi mang tính xử lý nội bộ trên canvas | Slide 2, 13 và speaker notes Draft 2 | Toàn bộ deck trình chiếu | Inspect không còn cụm `ẩn danh`, `anonym` hoặc `workbook` |
| Đưa dữ liệu Excel thật vào slide | Slide 2 và 13 | Bằng chứng bài toán và Before/After | Render trực tiếp từ `T4.2024`, `CongNgheMay` và `Danh mục` |
| Đa dạng hóa bố cục | Slide 2, 4, 5, 8, 13 | Mạch hình ảnh của 15 slide | Collage Excel, use case, phân rã chức năng, kiến trúc, Before/After |
| Giữ nội dung hội đồng cần xem | Toàn bộ Draft 2 | Deck chính 15 slide | Không đưa source code, test count hoặc viewport QA lên canvas |
| Kiểm tra kỹ thuật | `qa/template-fidelity-check.json`, `slides_test.py` | Bản Draft 2 hiện tại | Template fidelity `pass`, overflow `pass` |

### Biên nhận Visual Draft 3 — OpenAI-inspired (lịch sử, file nháp đã dọn)

| Nội dung | Đã lưu ở | Phạm vi/hiệu lực | Bằng chứng |
|---|---|---|---|
| Chuyển visual system sang editorial tối giản | Toàn bộ 15 slide Draft 3 | Chữ lớn, canvas sáng dịu, khoảng trắng rộng, giảm card/grid | Đã render và xem riêng đủ 15 slide |
| Giữ bằng chứng thực tế thay vì minh họa chung chung | Slide 2, 4, 5, 8, 10–13 | Excel thật, use case, phân rã chức năng, kiến trúc và UI thật | Asset nguồn được giữ nguyên, chỉ thay cách đặt khung và nhịp trình bày |
| Dùng phong cách OpenAI như nguồn tham khảo, không sao chép thương hiệu | Palette, typography và bố cục Draft 3 | Không dùng logo, Blossom, wordmark hoặc tuyên bố liên kết với OpenAI | Speaker notes Slide 1 có `[Sources]` đến Design Guidelines và trang chủ OpenAI |
| Loại bỏ các khối còn mang cảm giác dashboard | Slide 7 và 9 | Danh sách mở, số thứ tự xanh và ví dụ đen thay cho card lặp | Render cuối không còn viền card ở hai slide này |
| Kiểm tra kỹ thuật | Draft 3 và workspace QA | Bản OpenAI-inspired hiện tại | Template fidelity `pass`, `0 issue`; overflow `pass`; inspect không có `ẩn danh`, `anonym`, `workbook` hoặc nội dung kiểm thử code |

> Kết luận owner review: **không đạt về chất lượng thị giác**. Các kiểm tra kỹ thuật trên chỉ chứng minh file hợp lệ, không chứng minh slide đẹp. Draft 3 được giữ làm checkpoint, không dùng làm visual authority cho bản tiếp theo.

### Biên nhận chuyển hướng Figma Style Frames

| Nội dung | Đã lưu ở | Phạm vi/hiệu lực | Bằng chứng |
|---|---|---|---|
| Dừng cách mass-restyle PPTX bằng code | Living plan và trạng thái P3 | Toàn bộ phần thiết kế tiếp theo | Visual Draft 3 được đánh dấu `REJECTED BY OWNER` |
| Chuyển authority thiết kế sang Figma Slides | `https://www.figma.com/slides/WBVQyZzDDI9FUnUKG0ol4d` | Art direction và bố cục trình chiếu | File `GTAS VPP — Defense Style Frames` đã được tạo trong Figma |
| Duyệt hướng trước khi làm đủ deck | 3 style frame: Cover, Excel problem, Before/After | Chỉ nhân ra 15 slide sau owner approval | Ba slide dùng `Space Grotesk + Inter`, ảnh Excel/UI thật và bố cục khác nhau |
| Kiểm tra style frame | Figma Slides | Ba mẫu hiện tại | Không có tràn khung hoặc overlap ngoài chủ ý; hình học `clean: true` |

### Biên nhận Full Figma Deck

| Nội dung | Đã lưu ở | Phạm vi/hiệu lực | Bằng chứng |
|---|---|---|---|
| Nhân visual system đã duyệt thành đủ 15 slide | `https://www.figma.com/slides/WBVQyZzDDI9FUnUKG0ol4d` | Deck chính dùng để owner review và chuẩn bị bảo vệ | Slide grid có đúng 15 slide, thứ tự `1–15` |
| Giữ mạch nội dung hội đồng cần xem | Slide 1–15 trong Figma | Bài toán → phạm vi → thiết kế → kiến trúc → UI → kết quả → demo | Không đưa source code, test count hoặc chi tiết responsive lên canvas |
| Đưa bằng chứng thực tế vào deck | Slide 2, 10, 11 và 13 | Excel phân tán, đặt hàng theo kỳ, chốt kỳ và Before/After | Ảnh nguồn từ workbook và screenshot giao diện trong repository |
| Kiểm tra hình học toàn deck | Figma Slides | Toàn bộ text và thứ tự slide | `clean: true`; không thiếu font, không có text tràn biên hoặc text overlap |
| Chuẩn bị cho PowerPoint | Visual system dùng solid fill và ảnh raster | Giảm rủi ro khi xuất `.pptx` từ Figma Slides | Không dùng gradient hoặc tương tác bắt buộc để truyền đạt nội dung chính |

### Biên nhận cân chỉnh ảnh và ví dụ

| Nội dung | Đã lưu ở | Phạm vi/hiệu lực | Bằng chứng |
|---|---|---|---|
| Bỏ bố cục ảnh nghiêng và chồng lớp | Figma Slide 2 | Hai vùng Excel được đặt cùng trục, cùng mép và có khoảng cách rõ | Hai ảnh có `rotation = 0`, không còn giao nhau |
| Cân mép composition Before/After | Figma Slide 13 | Ảnh trước và hai ảnh sau cùng mốc trên/dưới | Các ảnh bắt đầu ở `y = 520` và kết thúc tại `y = 995` |
| Đưa ảnh giao diện về đúng tỷ lệ | Figma Slide 10 | Screenshot 16:9, nhãn kỳ và chú thích tách khỏi ảnh | Không còn vùng rỗng do khung sai tỷ lệ hoặc badge chồng lên ảnh |
| Rà toàn bộ slide có ảnh thật | Figma Slide 2, 10, 11, 13 | 7 ảnh bằng chứng trong deck | Kiểm tra hình học `clean: true`; không xoay, không chồng ảnh, không tràn biên |

### Biên nhận rà soát lại tiêu chí ban đầu

| Tiêu chí | Trạng thái | Bằng chứng / việc còn lại |
|---|---|---|
| 15 slide, mạch nói 10–12 phút, mỗi slide một claim | `MATCH` | Đủ thứ tự 1–15 theo storyboard; chưa rehearsal đo thời gian thực tế |
| Dành cho hội đồng trong phòng sáng, máy chiếu phổ thông | `PARTIAL` | Nền sáng chiếm đa số, tương phản cao, chữ hiển thị không dưới 20 pt; washout/projector QA thực tế chờ bản xuất |
| Hình ảnh thật, bố cục editorial, không biến thành dashboard | `MATCH` | Excel thật ở Slide 2/13; UI thật ở Slide 10/11; ảnh đã cân trục và không chồng lớp |
| Không đưa code, test count, viewport hoặc ghi chú sản xuất lên canvas | `MATCH` | Không còn từ khóa nội bộ; đã xóa `STYLE FRAME 01` và nhãn kỹ thuật thừa |
| Ngôn ngữ dễ hiểu cho giảng viên | `MATCH` | Đã thay `sheet`, `NCC`, `PO`, `POLICY`, `CONSTRAINT`, `ROWVERSION`, `IDEMPOTENCY` trên phần hiển thị |
| Speaker notes có talk track, câu chuyển và nguồn | `MATCH` | 15/15 slide có notes và block `[Sources]` |
| Không tràn chữ, thiếu font hoặc chồng text | `MATCH` | Kiểm tra toàn deck `clean: true` |
| PPTX, PDF và bộ ảnh dự phòng | `PARTIAL` | PPTX Complete chỉnh sửa được đã sửa lỗi font; pixel-perfect chỉ là bản dự phòng; PDF và bộ ảnh thực hiện sau owner review |

### Biên nhận PPTX pixel-perfect

| Nội dung | Đã lưu ở | Phạm vi/hiệu lực | Bằng chứng |
|---|---|---|---|
| Không dùng lại các đối tượng Figma bị PowerPoint diễn giải sai | `D:/WORK/GTAS_VPP_Submission_NguyenAnNam_DH52201078/GTAS VPP — Defense Pixel Perfect.pptx` | Toàn bộ 15 slide | Mỗi slide chỉ có một ảnh 16:9 xuất trực tiếp từ Figma |
| Giữ phần ghi chú thuyết trình | Speaker notes của PPTX mới | 15/15 slide | Inspect cuối ghi nhận `15 notes` |
| Loại bỏ lớp đối tượng lỗi ẩn phía dưới | PPTX mới được dựng sạch, không phủ ảnh lên file export cũ | Toàn bộ deck | `slides_test.py`: pass, không có nội dung tràn canvas |
| Giữ file Figma export ban đầu | File nguồn không bị ghi đè | Cho phép đối chiếu và tiếp tục chỉnh trong Figma | Bản mới được lưu bằng tên riêng `Defense Pixel Perfect` |

> Quyết định mới của owner: bản pixel-perfect không dùng làm PPTX chính vì mỗi slide là một ảnh. Bản chính phải giữ text, shape và ảnh chỉnh sửa được; chỉ sửa các lỗi thật sự quan sát được.

### Biên nhận PPTX Complete chỉnh sửa được

| Nội dung | Đã lưu ở | Phạm vi/hiệu lực | Bằng chứng |
|---|---|---|---|
| Giữ bản Complete làm nguồn và không ghi đè | `D:/WORK/GTAS_VPP_Submission_NguyenAnNam_DH52201078/GTAS VPP — Defense Pixel Perfect.pptx` | Nguồn Figma export Complete do owner cung cấp lại | Inspect ghi nhận 223 textbox, 7 ảnh và 15 speaker notes |
| Sửa lỗi dấu tiếng Việt trên tiêu đề lớn | `D:/WORK/GTAS_VPP_Submission_NguyenAnNam_DH52201078/GTAS VPP — Defense Complete Fixed.pptx` | 15 tiêu đề chính; không đổi nội dung, vị trí, kích thước, màu hoặc các đối tượng khác | Chuyển riêng font tiêu đề từ `Space Grotesk` sang `Inter`; render bằng Microsoft PowerPoint |
| Giữ khả năng chỉnh sửa trong PowerPoint | PPTX Complete Fixed | Toàn bộ 15 slide | Text, shape và ảnh vẫn là đối tượng riêng; không chuyển slide thành ảnh toàn trang |
| Kiểm tra cấu trúc template | Workspace QA của presentation | Master/layout/slide và các đối tượng không nằm trong edit plan | Template fidelity `pass`, `0 issue`; không có placeholder rỗng |
| Phân loại cảnh báo tràn canvas | Các slide có hình tròn trang trí cắt mép | Chỉ decorative bleed chủ ý | Layout audit xác nhận không có textbox hoặc ảnh vượt canvas; chỉ các circle trang trí |

---

## 12. Routing/capacity snapshot

Snapshot đo lại ngày `2026-08-11`: weekly coverage quan sát khoảng `706% Plus-equivalent` trên 7 tài khoản khả dụng; coverage cửa sổ 5 giờ chưa đầy đủ nên chỉ dùng để sắp xếp checkpoint, không coi là quota cố định.

- Khuyến nghị: một implementer chính để tránh lệch motif và narrative.
- Content mapping, storyboard và final QA: model reasoning cao.
- Các edit cơ học sau khi frame map đã khóa: reasoning trung bình.
- Ước tính sơ bộ toàn bộ plan → deck final: `5–15% Plus-equivalent`, confidence thấp–trung bình vì chưa có benchmark riêng cho PPTX; buffer 50%.
- Kết luận hiện tại: `ENOUGH`, đo lại tại P3 nếu implementation kéo dài hoặc phát sinh nhiều vòng visual review.

---

## 13. Handoff xuất PowerPoint

- Trong Figma Slides: `Main menu → File → Export slides to → PPTX → All slides → Export`.
- Nguồn chính thức: [Figma — Export from Figma Slides](https://help.figma.com/hc/en-us/articles/24848334599447-Export-from-Figma-Slides).
- Sau khi xuất, kiểm tra lại Slide 1, 5, 10, 13 và 15 trong PowerPoint vì đây là các slide có chữ lớn, ảnh thật hoặc bố cục lệch trục rõ nhất.
- Font không có trên máy PowerPoint có thể bị thay thế. Deck hiện dùng `Space Grotesk + Inter`; cần cài font hoặc đổi sang font an toàn trước bản nộp cuối nếu máy trình chiếu không có font.
- Deck không dùng gradient và không phụ thuộc tương tác động, nên tránh hai nhóm sai lệch phổ biến khi xuất PPTX từ Figma Slides.

---

## 14. Checkpoint dọn repository 2026-08-13

| Nội dung | Đã lưu ở | Phạm vi/hiệu lực | Bằng chứng |
|---|---|---|---|
| Chốt một deck hiện hành trong repository | `presentation/SlideBaoVe.pptx` | Bản native tiếp tục chỉnh sửa | Hash khớp bản `SlideBaoVe.pptx` trong thư mục nộp bài; PowerPoint render đủ 15 slide |
| Giữ công cụ cần dùng tiếp | `scripts/presentation/` | Inspect, render PowerPoint, so sánh ảnh và bản dự phòng pixel-perfect | Script được tách khỏi `.artifacts/` và có README sử dụng |
| Dọn bản nháp và đầu ra trung gian | Kho lưu tạm ngoài repository `D:/WORK/gtas-vpp-cleanup-archive/20260813` | Visual Draft 1–3, render, montage, layout JSON và inspect cũ | Đã chuyển 3.489 file, khoảng 643 MB; vẫn có thể phục hồi |
| Giữ dữ liệu vận hành ngoài Git | `.gitignore` + `LVTN/data/` local | Hai workbook nguồn chỉ dùng làm bằng chứng luận văn/slide | Không stage hoặc commit workbook dữ liệu vận hành |

> `slides_test.py` tiếp tục báo các shape trang trí tràn biên ở nhiều slide; đối chiếu render Microsoft PowerPoint xác nhận đây là decorative bleed chủ ý, không phải chữ hoặc ảnh nội dung bị cắt. Không sửa bố cục trong checkpoint dọn repository.
