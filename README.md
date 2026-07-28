#Tutor Platform Backend API

Hệ thống Backend cung cấp nền tảng kết nối Gia sư và Học viên, hỗ trợ đặt lịch học, quản lý tiến độ, thanh toán và đánh giá (Review). Dự án được thiết kế theo chuẩn **Clean Architecture** và mô hình **CQRS**.

##Công nghệ sử dụng
* **Framework:** .NET 8 / C# 10
* **Architecture:** Clean Architecture, CQRS (MediatR), Repository Pattern
* **Database:** SQL Server, Entity Framework Core (Fluent API)
* **Authentication:** JWT (JSON Web Tokens), Role-based Authorization
* **Realtime:** SignalR (Notifications)

##Cấu trúc dự án
* `TutorPlatform.Domain`: Chứa các Entities, Enums và Interfaces.
* `TutorPlatform.Application`: Chứa Business Logic (Commands/Queries) và DTOs.
* `TutorPlatform.Infrastructure`: Triển khai DbContext, Repositories và các External Services.
* `TutorPlatform.API`: Chứa Controllers, SignalR Hubs và cấu hình khởi chạy.

##Hướng dẫn chạy dự án (Local)
1. Clone dự án về máy.
2. Mở file `TutorPlatform.sln` bằng Visual Studio.
3. Thiết lập **`TutorPlatform.API`** làm Startup Project.
4. Mở file `appsettings.json` tại thư mục API và cấu hình chuỗi kết nối `DefaultConnection`.

##Hướng dẫn Migration Database
Mở cửa sổ **Package Manager Console** (trong Visual Studio) và đảm bảo **Default project** đang chọn là `TutorPlatform.Infrastructure`. Sau đó chạy các lệnh sau:

* **Tạo Migration mới:** (Thay đổi `InitialCreate` thành tên bạn muốn)
  ```powershell
  Add-Migration InitialCreate -StartupProject TutorPlatform.API
  Cập nhật Database:
  Update-Database -StartupProject TutorPlatform.API
Cuối cùng, nhấn F5 để khởi động Server và Swagger UI.
