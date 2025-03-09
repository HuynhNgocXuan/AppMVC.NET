# Cập nhật danh sách gói và nâng cấp hệ thống
sudo apt update -y && sudo apt upgrade -y

# Cài đặt máy chủ Apache
sudo apt -y install apache2

# Cài đặt Nginx
sudo apt -y install nginx

# Vô hiệu hóa AppArmor (thay thế cho SELinux trên Ubuntu)
sudo systemctl stop apparmor
sudo systemctl disable apparmor

# Đổi mật khẩu root thành '123' và cho phép đăng nhập SSH qua root
echo "root:123" | sudo chpasswd
sudo sed -i 's/^#PermitRootLogin prohibit-password/PermitRootLogin yes/' /etc/ssh/sshd_config
sudo sed -i 's/^PasswordAuthentication no/PasswordAuthentication yes/' /etc/ssh/sshd_config
sudo systemctl reload sshd

# Cài đặt .NET 6 SDK
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt update
sudo apt install -y dotnet-sdk-8.0

# Cài đặt MS SQL Server 2017 trên Ubuntu
wget -qO- https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -
sudo add-apt-repository "$(wget -qO- https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/mssql-server-2019.list)"
sudo apt update
sudo apt install -y mssql-server
sudo /opt/mssql/bin/mssql-conf setup

# Cài đặt các gói phụ thuộc cần thiết
sudo apt install -y python2 libssl1.0
