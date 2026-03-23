# PROMPT BỔ SUNG BÁO CÁO – Nhóm 12 – Ứng dụng Stream Video qua LAN

## Bối cảnh
Bạn là AI hỗ trợ viết báo cáo môn Lập trình Mạng (LTM) cho nhóm 12.  
Dự án là ứng dụng **StreamLAN** — truyền video/audio thời gian thực qua mạng nội bộ (LAN) bằng C# WPF, gồm hai ứng dụng:
- **Server (N12_StreamLAN)**: nhận stream từ Client, hiển thị, nhận diện khuôn mặt, ghi video.
- **Client (Client_StreamLAN)**: chụp webcam + micro, mã hóa và truyền qua UDP đến Server.

Báo cáo hiện tại đã có phần tổng quan, phân công, và mô tả các tính năng chính.  
**Nhiệm vụ của bạn**: viết thêm các **phần còn thiếu** liệt kê bên dưới, bằng **tiếng Việt**, theo phong cách báo cáo đại học, đúng kỹ thuật, rõ ràng (khoảng 150–300 từ mỗi mục trừ khi có ghi chú khác).

---

## PHẦN 1 – Truyền Âm thanh Thời gian thực (Audio Streaming)

### Thông tin kỹ thuật từ source code:
- **Client:** `AudioCaptureService.cs` — dùng thư viện **NAudio**, thu âm từ micro mặc định.
  - Định dạng: **PCM 16 kHz, 16-bit, mono** (WaveFormat 16000, 16, 1).
  - Kích thước buffer: **40 ms/chunk** ≈ 1.280 bytes/gói — vừa khít UDP.
  - Dữ liệu được đóng gói qua `PacketProtocol.Pack(seqNo, FlagAudio, pcmBytes)` rồi gửi qua **UDP port 9002**.
  - Có cờ `Enabled` để tắt/bật micro phía Client (Mute Client).

- **Server:** `UdpAudioReceiver.cs` + `AudioPlaybackService.cs`
  - `UdpAudioReceiver` lắng nghe **port 9002** riêng biệt với video (9000).
  - `AudioPlaybackService` dùng `WaveOutEvent` của NAudio, **BufferedWaveProvider** ring buffer 2 giây, `DiscardOnBufferOverflow = true` để tránh trễ tích lũy.
  - Latency phát thanh: **100 ms** (cấu hình DesiredLatency).
  - Hỗ trợ **Mute** và điều chỉnh **Volume** (0.0 – 1.0) phía Server.
  - Format phải khớp chính xác giữa hai đầu: `AudioPlaybackService.AudioFormat == AudioCaptureService.AudioFormat`.

### Yêu cầu viết:
Viết mục **"3.x. Module Truyền Âm Thanh (Audio Streaming)"** mô tả:
1. Lý do chọn PCM thay vì compressed codec (độ trễ thấp, không cần giải mã phức tạp).
2. Kiến trúc luồng âm thanh: Client thu → đóng gói → UDP → Server nhận → đưa vào buffer → phát loa.
3. Vì sao dùng port riêng (9002) thay vì ghép chung port video.
4. Cơ chế ring buffer và `DiscardOnBufferOverflow` giúp tránh độ trễ tích lũy.

---

## PHẦN 2 – Điều Chỉnh Chất Lượng Tự động (Adaptive Bitrate Controller)

### Thông tin kỹ thuật từ source code (`AdaptiveBitrateController.cs` – phía Client):
- Dải chất lượng JPEG: **MinQ = 15** đến **MaxQ = 85**, bước điều chỉnh **Step = 5**.
- Giá trị khởi tạo: **Quality = 50** (trung bình).
- Logic điều chỉnh sau mỗi frame gửi:
  - **Giảm chất lượng ngay** nếu: gói tin > **52.000 bytes** (ngưỡng an toàn dưới 65 KB giới hạn UDP) HOẶC thời gian gửi > **50 ms**.
  - **Tăng chất lượng chậm** nếu: thời gian gửi < 20 ms VÀ kích thước < 32.000 bytes liên tục **30 frames** (`StabilizeFrames`).
  - **Giữ nguyên** trong trường hợp trung gian.
- Mục tiêu: giữ chất lượng cao nhất có thể mà không làm vỡ giới hạn UDP 65 KB.

### Yêu cầu viết:
Viết mục **"3.x. Điều Chỉnh Bitrate Tự Động (Adaptive Bitrate)"** mô tả:
1. Vấn đề: UDP bị giới hạn 65 KB/gói, JPEG lớn có thể vượt ngưỡng gây mất gói.
2. Giải pháp: controller tự động phản hồi theo kích thước và độ trễ thực tế.
3. Cơ chế "nhanh giảm – chậm tăng" (giảm ngay / tăng sau 30 frame ổn định) giúp ổn định luồng.
4. So sánh với cơ chế manual quality (người dùng cũng có thể chỉnh tay qua `ManualQuality`).

---

## PHẦN 3 – Giao Thức Đóng Gói Tùy Chỉnh (PacketProtocol)

### Thông tin kỹ thuật từ source code (`PacketProtocol.cs` – dùng chung cả Server và Client):
- **Header 5 bytes** đặt trước mỗi gói UDP:
  - [0–3]: `seqNo` (uint32, little-endian) — số thứ tự gói, dùng phát hiện mất gói.
  - [4]: `flags` (1 byte) — loại dữ liệu:
    - `0x01` = `FlagKeyFrame` (frame quan trọng)
    - `0x02` = `FlagPaused` (trạng thái tạm dừng)
    - `0x04` = `FlagAudio` (gói âm thanh)
- Cả hai đầu dùng chung `PacketProtocol.Pack()` / `PacketProtocol.Unpack()`.
- `ClientSession` phía Server dựa vào `seqNo` để **đếm số gói bị mất** (`PacketLostCount`).

### Yêu cầu viết:
Viết mục **"3.x. Giao Thức Đóng Gói Dữ Liệu (Packet Protocol)"** mô tả:
1. Lý do cần header tự chế trên nền UDP thô (UDP không có thứ tự, không biết loại dữ liệu).
2. Cấu trúc 5-byte header: seqNo + flags.
3. Cách `seqNo` giúp Server phát hiện mất gói và tính tỉ lệ mất gói.
4. Cờ `flags` để phân biệt audio/video/paused mà không cần port riêng cho mỗi loại tín hiệu điều khiển.

---

## PHẦN 4 – Quản Lý Trạng Thái Stream (StreamController)

### Thông tin kỹ thuật từ source code (`StreamController.cs` – phía Client):
- Enum `StreamState`: `Stopped`, `Running`, `Paused`, `Reconnecting`.
- Event `StateChanged` thông báo UI cập nhật khi state thay đổi.
- Cấu hình stream được tập trung tại đây:
  - `Resolution`: mặc định 640×480.
  - `FrameDelayMs`: 33 ms (~30 FPS).
  - `ManualQuality`: 50 (thủ công, khi tắt Adaptive).
  - `UseAdaptive`: bật/tắt AdaptiveBitrateController.
  - Camera controls: `FlipH`, `FlipV`, `Brightness`, `Contrast`, v.v.

### Yêu cầu viết:
Viết mục **"3.x. Quản Lý Trạng Thái Luồng (Stream State Management)"** mô tả:
1. Vì sao cần quản lý state tập trung thay vì dùng cờ bool rải rác.
2. Các trạng thái và chuyển tiếp: Stopped ↔ Running ↔ Paused, Reconnecting → Running.
3. Cách event StateChanged giúp UI phản ứng ngay mà không cần polling.

---

## PHẦN 5 – Xác Thực Phía Server (ServerAuthService)

### Thông tin kỹ thuật từ source code (`ServerAuthService.cs`):
- Mặc định: tài khoản admin lấy từ **biến môi trường** `SERVER_ADMIN_USER` / `SERVER_ADMIN_PWD` (default: `admin` / `admin123`).
- Hỗ trợ xác thực qua **Supabase Password Grant** (`/auth/v1/token?grant_type=password`):
  - Gửi `email` + `password` lên Supabase REST API.
  - Đọc `access_token` từ response JSON để xác nhận thành công.
  - Cấu hình qua biến môi trường: `SERVER_SUPABASE_URL`, `SERVER_SUPABASE_ANON_KEY`.
- Hoạt động song song với `SupabaseAuthService.cs` phía Client.

### Yêu cầu viết:
Viết mục **"3.x. Xác Thực Hai Phía (Client + Server Authentication)"** mô tả:
1. Client dùng `SupabaseAuthService` xác thực người dùng trước khi stream.
2. Server dùng `ServerAuthService` xác thực tài khoản quản trị để truy cập bảng điều khiển.
3. Cả hai tích hợp Supabase — ưu điểm: không cần tự dựng backend auth.
4. Cơ chế fallback: nếu không có config Supabase, dùng tài khoản local từ biến môi trường.

---

## PHẦN 6 – Theo Dõi Phiên Làm Việc Client (ClientSession)

### Thông tin kỹ thuật từ source code (`ClientSession.cs` – phía Server):
- Mỗi Client kết nối có 1 đối tượng `ClientSession` riêng, lưu trữ:
  - `EndPoint` (IP:Port), `LastSeen`, `FrameCount`, `LastSeqNo`.
  - `PacketLostCount`: đếm số gói bị bỏ dựa vào gap trong `seqNo`.
  - `CurrentFps`: FPS thực tế tính theo thời gian giữa các frame (cập nhật mỗi giây).

### Yêu cầu viết:
Viết mục **"3.x. Giám Sát Chất Lượng Kết Nối (Client Session Monitoring)"** mô tả:
1. Mỗi client được Server theo dõi riêng biệt qua đối tượng ClientSession.
2. Tính FPS thực tế và tỉ lệ mất gói UDP trực tiếp từ dữ liệu nhận được.
3. Thông tin này hiển thị lên giao diện Server để người dùng giám sát chất lượng stream.

---

## PHẦN 7 – Nhận Diện Khuôn Mặt (FaceDetectionService)

### Thông tin kỹ thuật từ source code (`FaceDetectionService.cs` – phía Server):
- Dùng **OpenCvSharp** (wrapper .NET của OpenCV) + file `haarcascade_frontalface_default.xml`.
- `DetectMultiScale`: scaleFactor = 1.1, minNeighbors = 5, minSize = 30×30.
- Vẽ **khung màu xanh lá (Green, 0, 255, 0)** quanh mỗi khuôn mặt phát hiện được.
- Xử lý **in-place** trực tiếp trên `Mat` frame — không tạo frame mới, tiết kiệm bộ nhớ.
- Tự động tải Cascade từ thư mục `Data/` hoặc thư mục exe.
- Nếu không tìm thấy file XML: `IsAvailable = false`, chức năng bị tắt nhẹ nhàng (không crash).

### Yêu cầu viết:
Viết mục **"3.x. Tính Năng Nhận Diện Khuôn Mặt (Face Detection)"** mô tả:
1. Thuật toán Haar Cascade: nguyên lý "sliding window" + "boosted classifiers".
2. Tham số DetectMultiScale và ảnh hưởng đến độ chính xác vs. tốc độ.
3. Vì sao chọn xử lý phía Server thay vì Client (Server có toàn bộ frame đã nhận, không tốn bandwidth thêm).
4. Cơ chế graceful degradation khi thiếu file model.

---

## PHẦN 8 – Ghi Video (RecordingService)

### Thông tin kỹ thuật từ source code (`RecordingService.cs` – phía Server):
- Dùng `OpenCvSharp.VideoWriter`, codec **mp4v (FourCC)**, container **.MP4**.
- FPS ghi cố định: **25.0 FPS**.
- Lưu vào thư mục `Captures/` trong thư mục chạy exe, tên file: `StreamLAN_yyyyMMdd_HHmmss.mp4`.
- Khởi tạo VideoWriter **lazy** (lần ghi frame đầu tiên) để lấy kích thước frame thực tế.
- Nếu frame đến có kích thước khác: tự động **resize** trước khi ghi.
- Thread-safe: dùng `lock (_lock)` bảo vệ toàn bộ `Start`, `WriteFrame`, `Stop`.
- `IsRecording` = true ngay từ `Start()` (trước khi có frame), đảm bảo nút "Stop" hoạt động đúng.

### Yêu cầu viết:
Viết mục **"3.x. Tính Năng Ghi Hình (Video Recording)"** mô tả:
1. Luồng ghi: nhận frame từ UdpReceiver → FaceDetectionService vẽ khung → RecordingService ghi vào file.
2. Vì sao chọn codec mp4v / container MP4 (tương thích rộng, kích thước hợp lý).
3. Kỹ thuật lazy-init VideoWriter và auto-resize giải quyết vấn đề kích thước frame không cố định.
4. Thread-safety quan trọng vì frame đến liên tục từ thread UDP riêng.

---

## PHẦN 9 – Tự động Cấu Hình Tường Lửa (FirewallHelper)

### Thông tin kỹ thuật từ source code (`FirewallHelper.cs` – phía Server):
- Tự động thêm **3 Windows Firewall rules** khi lần đầu chạy (cần UAC một lần):
  - `StreamLAN-Video` → port **9000** (UDP, Inbound)
  - `StreamLAN-Discovery` → port **9001** (UDP, Inbound/Outbound)
  - `StreamLAN-Audio` → port **9002** (UDP, Inbound)
- Dùng `netsh advfirewall firewall add rule` qua `Process.Start`.
- Lưu flag file để không chạy lại lần sau.
- Nếu từ chối UAC: chỉ hiện cảnh báo, không crash ứng dụng.

### Yêu cầu viết:
Viết mục **"3.x. Tự Động Mở Cổng Tường Lửa (Auto Firewall Configuration)"** mô tả:
1. Vấn đề: Windows Firewall mặc định chặn UDP inbound từ máy lạ.
2. Giải pháp: chạy `netsh` một lần, lưu flag để không hỏi lại.
3. Nguyên tắc least-privilege: chỉ mở đúng 3 port cần thiết, không tắt hoàn toàn firewall.
4. Graceful fallback nếu user từ chối.

---

## HƯỚNG DẪN VIẾT

- Ngôn ngữ: **Tiếng Việt**, văn phong báo cáo đại học kỹ thuật.
- Mỗi mục đặt tiêu đề cấp độ **3** (###), có thể có tiêu đề phụ cấp **4** (####).
- Dùng bullet point khi liệt kê kỹ thuật, dùng văn xuôi để giải thích nguyên lý.
- Kèm **sơ đồ mô tả luồng dữ liệu** (dạng text/ASCII hoặc mô tả để người đọc tự vẽ) ở các mục Audio, Packet Protocol và Recording.
- Tham chiếu tên file nguồn khi cần xác minh (`AudioCaptureService.cs`, v.v.).
- **Không** bịa thêm tính năng chưa có trong mô tả kỹ thuật.
- Đánh số mục theo thứ tự thực tế của báo cáo (có thể dùng `3.7`, `3.8`, v.v. hoặc để AI tự sắp xếp).
