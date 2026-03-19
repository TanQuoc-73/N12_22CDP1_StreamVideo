# Bảng phân công công việc thực tế (Dự án LAN Video Stream)

Tuyệt vời! Tôi đã dựa theo hình ảnh tham khảo của bạn để lập bảng phân công tương tự cho toàn bộ cả 3 đợt. (Tên các file đã được tôi tham chiếu trực tiếp từ project của bạn để đảm bảo độ chính xác). 

Bạn có thể copy các bảng này vào Word để lưu lại.

---

### 1. Phân công công việc Đợt 1

| Thành viên | Công việc đợt 1 (Core stream) 1 máy chạy test cả Server và Client |
| --- | --- |
| **Dương Quốc Tản** | **Camera Capture (Client)**<br>Vị trí file:<br> - `Client_StreamLAN/Services/CameraService.cs`<br>Mục tiêu: Chạy webcam, lấy frame |
| **Nguyễn Tiến Thắng** | **Convert + Hiển thị webcam (Client UI)**<br>Vị trí file:<br> - `Client_StreamLAN/Utils/ImgConverter.cs`<br> - `Client_StreamLAN/Views/MainWindow.xaml`<br> - `Client_StreamLAN/Views/MainWindow.xaml.cs`<br>Mục tiêu: Hiển thị webcam lên màn hình client |
| **Nguyễn Đạt Quốc Huy** | **UDP sender + Encode (Client)**<br>Vị trí file :<br> - `Client_StreamLAN/Services/UdpSender.cs`<br> - `Client_StreamLAN/Views/MainWindow.xaml.cs`<br>Mục tiêu: Cấu hình IP + port phía client, gửi UDP sang Server |
| **Lê Thị Quỳnh Trang** | **UDP Receiver (Server)**<br>Vị trí file:<br> - `N12_StreamLAN/Services/UdpReceiver.cs`<br>Mục tiêu: Nhận UDP được gửi từ Client |
| **Phạm Tuấn Hưng** | **Decode + Hiển thị (Server UI)**<br>Vị trí file:<br> - `N12_StreamLAN/Utils/ImgConverter.cs`<br> - `N12_StreamLAN/Views/MainWindow.xaml`<br> - `N12_StreamLAN/Views/MainWindow.xaml.cs`<br>Mục tiêu: Hiển thị dữ liệu trả về lên màn hình Server |

---

### 2. Phân công công việc Đợt 2

| Thành viên | Công việc đợt 2 (Cải tiến app) dùng IP riêng từng máy để kết nối LAN |
| --- | --- |
| **Dương Quốc Tản** | **Quản lý Truyền Đa luồng (Multi-threading)**<br>Vị trí file:<br> - `Client_StreamLAN/Views/MainWindow.xaml.cs`<br> - `N12_StreamLAN/Views/MainWindow.xaml.cs`<br> - `Client_StreamLAN/Services/UserSession.cs`<br>Mục tiêu: Xử lý Task bất đồng bộ, cân bằng tải để giao diện Server/Client không bị treo khi truyền frame liên tục qua mạng IPv4 thực tế (LAN). |
| **Nguyễn Tiến Thắng** | **Form Đăng nhập (Client UI)**<br>Vị trí file:<br> - `Client_StreamLAN/Views/LoginForm.xaml`<br> - `Client_StreamLAN/Views/LoginForm.xaml.cs`<br>Mục tiêu: Thiết kế, xử lý UX/UI màn hình đăng nhập cho ứng dụng khách trước khi vào trang tính năng. |
| **Nguyễn Đạt Quốc Huy** | **Xác thực Hệ thống (Database Auth)**<br>Vị trí file:<br> - `Client_StreamLAN/Services/SupabaseAuthService.cs`<br>Mục tiêu: Gọi giao tiếp API/SDK để kết nối cơ sở dữ liệu (Supabase), thực thi truy vấn tài khoản, mật khẩu. |
| **Lê Thị Quỳnh Trang** | **Broadcast Phát sóng IP (Server Discovery)**<br>Vị trí file:<br> - `N12_StreamLAN/Services/DiscoveryService.cs`<br> - `N12_StreamLAN/Services/NetworkInfo.cs`<br>Mục tiêu: Lập trình để Server phát tín hiệu UDP Broadcast giới thiệu IP của mình ra toàn bộ mạng LAN, lấy cấu hình card mạng cục bộ. |
| **Phạm Tuấn Hưng** | **Kết nối LAN tự động (Client Discovery)**<br>Vị trí file:<br> - `Client_StreamLAN/Services/ServerDiscovery.cs`<br> - `Client_StreamLAN/Services/NetworkInfo.cs`<br>Mục tiêu: Lắng nghe tín hiệu từ LAN, tự động điền IP của server và cấu hình card mạng máy tính cho Client kết nối mà không cần tra cứu tay. |

---

### 3. Phân công công việc Đợt 3

| Thành viên | Công việc đợt 3 (Mở rộng app) Ghi lại video + Nhận diện khuôn mặt |
| --- | --- |
| **Dương Quốc Tản** | **AI - Phát hiện Khuôn mặt (Face Recognition Core)**<br>Vị trí file:<br> - `N12_StreamLAN/Views/MainWindow.xaml.cs`<br> - Các Utils tích hợp xử lý hình ảnh<br>Mục tiêu: Áp dụng thư viện xử lý ảnh (OpenCV/EmguCV...), tự động quét và vẽ Bounding Box (khung hình chữ nhật) theo sát khuôn mặt trên nền stream realtime. |
| **Nguyễn Tiến Thắng** | **Xây dựng Giao diện AI & Record (UI/UX)**<br>Vị trí file:<br> - `N12_StreamLAN/Views/MainWindow.xaml`<br> - `Client_StreamLAN/Views/MainWindow.xaml`<br>Mục tiêu: Thiết kế các Control (Nút bấm báo hiệu Record, Toggle bật Face Detection) trên WPF, cập nhật layout theme (AutumnTheme.xaml) tương thích. |
| **Nguyễn Đạt Quốc Huy** | **Capture & Ghi dữ liệu file Media (Video Record)**<br>Vị trí file:<br> - `N12_StreamLAN/Views/MainWindow.xaml.cs` (Khu vực xử lý Write media)<br> - Phân hệ Utils nén video<br>Mục tiêu: Tạo bộ đệm đọc các Bitmap trả về, biên dịch và ghép lại thành khung hình video chuẩn (AVI/MP4) và lưu trực tiếp xuống ổ cứng HDD/SSD. |
| **Lê Thị Quỳnh Trang** | **Tối ưu Băng thông (Optimization)**<br>Vị trí file:<br> - `N12_StreamLAN/Services/UdpReceiver.cs`<br> - `Client_StreamLAN/Services/UdpSender.cs`<br>Mục tiêu: Tối ưu hóa kích thước truyền Byte Array để ứng dụng không bị rớt mạng/mất frame khi cả Record và Nhận diện AI cùng hoạt động chiếm tài nguyên. |
| **Phạm Tuấn Hưng** | **Quản lý Tiện ích File System**<br>Vị trí file:<br> - `N12_StreamLAN/Views/MainWindow.xaml.cs`<br> - `N12_StreamLAN/App.xaml.cs`<br>Mục tiêu: Xử lý đếm thời lượng ghi video, bắt các lỗi vây quanh quyền truy cập File (Permission denied) khi lưu file hệ thống, Exception báo lỗi. |
