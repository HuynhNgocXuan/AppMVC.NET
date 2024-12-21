// snowfall.js
document.addEventListener("DOMContentLoaded", () => {
  const snowflakeCount = 50; // Số lượng bông tuyết
  const snowContainer = document.body; // Lấy phần tử body làm container cho bông tuyết

  // Hàm tạo bông tuyết
  function createSnowflake() {
    const snowflake = document.createElement("span");
    snowflake.classList.add("snowflake");
    snowflake.textContent = "❄"; // Biểu tượng bông tuyết

    // Vị trí ngẫu nhiên của bông tuyết
    snowflake.style.left = `${Math.random() * 100}vw`; // Tạo vị trí ngẫu nhiên trên trục X
    snowflake.style.animationDuration = `${Math.random() * 10 + 5}s`; // Đặt thời gian rơi ngẫu nhiên
    snowflake.style.fontSize = `${Math.random() * 10 + 10}px`;

    snowflake.style.setProperty(
      "--wind-direction",
      Math.random() > 0.5 ? "1" : "-1"
    );

    snowContainer.appendChild(snowflake);

    // Xóa bông tuyết khi nó rơi xong
    snowflake.addEventListener("animationend", () => {
      snowflake.remove();
    });
  }

  // Tạo bông tuyết mỗi giây
  // setInterval(createSnowflake, 200);
});
