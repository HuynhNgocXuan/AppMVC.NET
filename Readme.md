


# WebMVC Project (AppMVC.NET)

## 📌 Giới thiệu

Đây là dự án **ASP.NET Core MVC** mẫu, được xây dựng nhằm thực hành kiến trúc MVC (Model–View–Controller) trên nền tảng .NET.
Project bao gồm các tính năng cơ bản:

* Trang chủ (Home)
* Quản lý sản phẩm (Product)
* Đăng nhập, đăng xuất (Identity)

## 🚀 Công nghệ sử dụng

* **ASP.NET Core MVC**
* **Entity Framework Core** với **SQL Server**
* **ASP.NET Core Identity** (đăng nhập/đăng xuất, xác thực Google/Facebook)
* **Razor Pages**
* **LibMan** để quản lý front-end libraries (jQuery, Bootstrap, …)
* **Docker** (SQL Server trong container)
* HTML, CSS, JavaScript

## 📂 Cấu trúc chính

* `webMVC.sln` : Solution file
* `webMVC.csproj` : File cấu hình project ASP.NET MVC
* `Controllers/` : Chứa các controller (điều hướng route)
* `Views/` : Razor views hiển thị giao diện
* `Models/` : Khai báo các lớp dữ liệu, entity
* `wwwroot/` : Static files (CSS, JS, images)
* `package.json` : Quản lý packages front-end

## ▶️ Chạy project

1. Clone repo về máy:

   ```bash
   git clone https://github.com/HuynhNgocXuan/AppMVC.NET.git
   ```
2. Mở project bằng **Visual Studio** hoặc **Visual Studio Code**.
3. Khởi tạo database (nếu có dùng `Update-Database` với EF Core).
4. Chạy project:

   ```bash
   dotnet run
   ```
5. Truy cập trình duyệt:

   * Trang chủ: `http://localhost:5000/`
   * Trang login: `http://localhost:5000/login`
   


