# Items Editor — sửa vật phẩm của mod bằng biểu mẫu

Items Editor sửa các vật phẩm của một mod khi game đang chạy: tên và mô tả bằng mọi ngôn ngữ của mod, giá, hình ảnh, loại vật phẩm và hành động khi dùng từ túi đồ. **Lưu** ghi các tệp của mod và game hiện thay đổi ngay — không cần khởi động lại, không cần tải lại.

Nó mở từ hộp công cụ (chiếc búa trên thanh công cụ) dưới tên **Vật phẩm**.

## Một chỉnh sửa và kết quả trong game

Trong RPG Plus, túi đồ có thuốc tiên của cửa hàng trong làng:

![Túi đồ với Thuốc tiên được chọn: Targeted, giá 10.](assets/bag-before.jpg)

Trong trình sửa: chọn mod `rpg_demo_game` ở trên, chọn **Thuốc tiên** trong danh sách, gõ một tên tiếng Anh mới rồi nhấn **Lưu** (Ctrl+S):

![Cửa sổ Vật phẩm trên rpg_demo_game: danh sách Củi khô, Đá, Nấm rừng và Dawn elixir; biểu mẫu của rpg_demo/elixir với khóa tên rpg_demo/item/elixir/name, văn bản (en) Dawn elixir và sáu ngôn ngữ còn lại giữ nguyên, mô tả bằng bảy ngôn ngữ, Giá 10, Biểu tượng rpg_demo/healing_elixir cùng hình của nó, Kiểu TargetUsed, Hành động action/heal. Dòng trạng thái: "Đã lưu: 0 tệp, 1 văn bản trong mod rpg_demo_game; game đã đọc lại dữ liệu".](assets/editor-saved.jpg)

Chỉ dòng tiếng Anh trong `locale/en.properties` của mod thay đổi; tệp vật phẩm không bị đụng tới. Cùng chai thuốc tiên trong túi đồ ngay sau đó:

![Túi đồ giờ hiện vật phẩm là Dawn elixir, vẫn Targeted, giá 10.](assets/bag-after.jpg)

## Biểu mẫu

- **Id** — id của vật phẩm; kịch bản, cửa hàng, bản đồ và mod khác tìm nó qua id này.
- **Tên** và **Mô tả** — khóa dịch và một dòng cho mỗi ngôn ngữ của mod. Vật phẩm mới nhận các khóa tạo từ id của nó; văn bản được ghi vào tệp bản địa hóa của mod.
- **Giá** — tính bằng đồng xu (cửa hàng có thể tự đặt giá).
- **Biểu tượng** — một hình trong nhóm vật phẩm; **…** mở lưới để chọn.
- **Kiểu** — Quest, Used, TargetUsed, Food, Resource, Recipe, CantDrop; có thể bật nhiều cái.
- **Hành động** — dùng vật phẩm thì chuyện gì xảy ra: `null` hoặc một trong các hành động vật phẩm mà các mod đã tải đăng ký (`action/heal`, `action/equip`…).
- **Trường của mod khác** — những trường mà mod khác mô tả trong lược đồ của họ cho vật phẩm (ví dụ kiểu trang bị, đặc tính và lớp được phép của Equipment) hiện trong mục riêng với cùng loại trình sửa.
- **Các trường khác** — mọi thứ còn lại trong JSON của vật phẩm, hiện và sửa dưới dạng JSON; được giữ nguyên như cũ.

Thanh công cụ: **Mới** (Ctrl+N) thêm `<mod>/new_item` — một tài nguyên giá một đồng xu, không có hành động; **Sao chép** (Ctrl+D), **Xóa**, **Hoàn tác**/**Làm lại** (Ctrl+Z / Ctrl+Y). Lưu chỉ ghi những tệp có văn bản thay đổi; vật phẩm mới vào `items/<mod>.json`.

## Những vật phẩm nào được sửa

Danh sách hiện các vật phẩm của một mod — những vật phẩm trong tệp của chính mod đó. Danh sách mod chỉ có các mod cài dưới dạng thư mục. Muốn đổi vật phẩm của mod khác mà không sửa mod đó, hãy mang cùng id trong mod của bạn và khai báo phụ thuộc vào chủ sở hữu; chính Items Editor chỉ sửa tệp của mod được chọn.

## Khi có trục trặc

1. **Danh sách mod trống.** Không có mod nào ghi được; mod đóng gói không sửa được.
2. **Túi đồ vẫn hiện tên cũ.** Vật phẩm được lưu bằng ngôn ngữ khác ngôn ngữ của game; hãy kiểm tra dòng của ngôn ngữ game.
3. **Danh sách hành động thiếu một hành động.** Mod đăng ký nó chưa được tải trong game này.
