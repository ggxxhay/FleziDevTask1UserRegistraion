# User Registration & Login — Yêu cầu

Tóm tắt từ `Task1_User_Registration_Login 1.pdf`. Tài liệu gốc có 4 trang nội dung được lặp lại lần hai trong PDF.

## 1. Mục tiêu

Xây dựng một luồng xác thực full stack cho phép khách mới đăng ký tài khoản và người dùng hiện có đăng nhập. Phạm vi gồm xác thực đầu vào, băm mật khẩu, cấp token/session và xử lý các trạng thái thành công, lỗi.

## 2. Đăng ký và dữ liệu người dùng

- Email phải đúng định dạng, được `trim` và chuyển thành chữ thường trước khi lưu.
- Email phải duy nhất. Email khác chữ hoa/thường hoặc có khoảng trắng thừa vẫn là cùng một tài khoản.
- Mật khẩu tối thiểu 8 ký tự, có ít nhất một chữ cái và một chữ số.
- Băm mật khẩu bằng thuật toán thích nghi an toàn và salt riêng cho mỗi người dùng (tài liệu nêu bcrypt, Argon2 hoặc scrypt làm ví dụ).
- Không lưu mật khẩu dạng rõ hoặc dùng mã hóa có thể giải ngược.
- Mô hình dữ liệu gợi ý:
  - `id`: khóa chính.
  - `email`: chuỗi duy nhất, có index, đã chuẩn hóa.
  - `passwordHash`: hash mật khẩu; không trả trong response.
  - `createdAt`: thời điểm tạo.
- Đăng ký thành công trả HTTP `201` cùng thông tin user an toàn, không gồm `passwordHash`.
- Email đã tồn tại trả HTTP `409` với thông báo rõ ràng.
- Đầu vào không hợp lệ trả HTTP `400` với lỗi theo từng trường.

Ví dụ request:

```json
{ "email": "a@b.com", "password": "Secret123" }
```

Ví dụ response thành công:

```json
{ "id": "...", "email": "a@b.com", "createdAt": "..." }
```

## 3. Đăng nhập và bảo mật

- Đăng nhập thành công trả HTTP `200`, gồm token hoặc session, thời hạn hết hạn và thông tin user an toàn.
- Sai thông tin đăng nhập trả HTTP `401` với thông báo chung, ví dụ `Invalid email or password`, để tránh tiết lộ email có tồn tại hay không.
- Cấp token đã ký hoặc session phía server với thời hạn hợp lý.
- So sánh mật khẩu chống timing attack bằng phép so sánh constant-time.
- Không ghi mật khẩu hoặc token dạng rõ vào log.
- Có thể ghi chú thiết kế về rate limit hoặc khóa tạm khi đăng nhập sai nhiều lần.

Ví dụ response đăng nhập:

```json
{
  "token": "...",
  "expiresIn": 3600,
  "user": { "id": "...", "email": "..." }
}
```

## 4. Giao diện

- Có form đăng ký và đăng nhập; kiểm tra định dạng email và quy tắc mật khẩu.
- Vô hiệu hóa nút gửi trong lúc request chạy và hiển thị trạng thái tải.
- Khi thành công, lưu token/session an toàn và chuyển tới trang được bảo vệ.
- Khi thất bại, hiển thị thông báo máy chủ thân thiện, ví dụ email đã được sử dụng.
- Có liên kết chuyển đổi giữa màn hình đăng ký và đăng nhập.

## 5. Kịch bản nghiệm thu

1. Đăng ký email hợp lệ và mật khẩu đạt yêu cầu: tạo user, mật khẩu được băm.
2. Đăng ký email đã tồn tại: trả `409`, không tạo bản ghi trùng.
3. Email không hợp lệ hoặc mật khẩu quá ngắn: trả `400` cùng lỗi trường tương ứng.
4. Đăng nhập bằng mật khẩu đúng: trả token/session có thời hạn.
5. Đăng nhập bằng mật khẩu sai: trả `401` với thông báo chung.
6. Xác minh mật khẩu đúng với hash đã lưu: kết quả xác minh là thành công.

## 6. Bàn giao

- Backend endpoints có validation, băm mật khẩu và cấp token/session.
- Frontend đăng ký/đăng nhập kết nối với API.
- Unit tests bao phủ các kịch bản nghiệm thu, gồm cả trường hợp lỗi.
- README ngắn hướng dẫn chạy ứng dụng và test.
- Bảng ước lượng user story theo Fibonacci (`1, 2, 3, 5, 8, 13, ...`) kèm giải thích dựa trên độ phức tạp, công sức, rủi ro và mức độ chưa chắc chắn.

## 7. Những điểm tài liệu không ấn định

Tài liệu không chọn framework, cơ sở dữ liệu hay URL endpoint cụ thể. Đây là các quyết định cần dựa trên cấu trúc repo hiện có hoặc thống nhất khi bắt đầu triển khai.
