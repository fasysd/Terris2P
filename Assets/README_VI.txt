TERRIS 2P — ĐỐI KHÁNG XẾP KHỐI 2 NGƯỜI
========================================

Trò chơi xếp khối đối kháng 2 người thời gian thực trên một Grid dùng chung (15 cột x 30 hàng).
Dự án được cấu trúc thành 2 Scene tách biệt (`MenuScene` và `GameplayScene`), hỗ trợ Đa ngôn ngữ (Tiếng Anh & Tiếng Việt), mặc định là Tiếng Anh.

1. TÙY CHỌN NGÔN NGỮ (LANGUAGE SELECTION)
-----------------------------------------
- Mặc định khởi động: Tiếng Anh (English).
- Trên màn hình `MenuScene`: Nút chọn ngôn ngữ màu tím "🌐 LANGUAGE: ENGLISH / NGÔN NGỮ: TIẾNG VIỆT" ở cột bên trái.
- Khi người chơi bấm nút này:
  * Ngôn ngữ chuyển đổi qua lại tức thì giữa Tiếng Anh (English) và Tiếng Việt.
  * Toàn bộ tiêu đề, phụ đề, nút bấm, hướng dẫn luật chơi, thẻ phím điều khiển, HUD trong trận đấu và hộp thoại tạm dừng sẽ được cập nhật ngôn ngữ đồng bộ.
  * Lựa chọn ngôn ngữ được lưu tự động (PlayerPrefs) để duy trì qua các phiên chơi tiếp theo.

2. VAI TRÒ NGƯỜI CHƠI (PLAYER ROLES)
------------------------------------
- Người chơi 1 (P1): Người ở trên (Top) — NGƯỜI TẤN CÔNG / ATTACKER, trọng lực rơi xuống (-Y).
- Người chơi 2 (P2): Người ở dưới (Bottom) — NGƯỜI PHÒNG THỦ / DEFENDER, trọng lực rơi lên (+Y).

3. ĐIỀU KHIỂN (CONTROLS)
------------------------
Người chơi 1 (Phía trên - Người Tấn Công / Attacker):
- A / D: Sang trái / Sang phải (Move Left / Right)
- W: Xoay khối (Rotate Piece)
- S: Tăng tốc độ rơi (Soft Drop / Fast Fall)
- Space (hoặc F / Left Shift): Thả khối tức thì (Hard Drop)

Người chơi 2 (Phía dưới - Người Phòng Thủ / Defender):
- Phím mũi tên Trái / Phải (← / →): Sang trái / Sang phải (Move Left / Right)
- Phím mũi tên Xuống (↓): Xoay khối (Rotate Piece)
- Phím mũi tên Lên (↑): Tăng tốc độ rơi (Soft Drop / Fast Fall)
- Phím Right Shift (hoặc Enter / Right Ctrl): Thả khối tức thì (Hard Drop)

Điều khiển chung:
- Tab hoặc V: Chuyển đổi giữa chế độ camera Split-Screen (Chia đôi màn hình) và Unified Full-Board (Toàn cảnh bàn cờ).
- Esc hoặc P: Tạm dừng trận đấu (Mở hộp thoại TẠM DỪNG / PAUSE).

4. HỆ THỐNG 2 SCENE & TÍNH NĂNG TẠM DỪNG (PAUSE)
------------------------------------------------
1. Scene Menu (`Assets/Scenes/MenuScene.unity` - Build Index 0):
   - Cột bên trái: Tiêu đề trò chơi, nút "▶ PLAY GAME / BẮT ĐẦU CHƠI", nút "📷 TOGGLE CAMERA VIEW", nút "🌐 LANGUAGE SELECTION", khung mẹo nhanh.
   - Cột bên phải: Hướng dẫn chi tiết luật chơi đối kháng và bảng điều khiển cho cả 2 người chơi.
   - Bấm nút Play sẽ chuyển trực tiếp sang `GameplayScene`.

2. Scene Trận đấu (`Assets/Scenes/GameplayScene.unity` - Build Index 1):
   - Tự động bắt đầu trận đấu với đếm ngược 3 - 2 - 1 - START / BẮT ĐẦU!
   - Nút "⏸ PAUSE / TẠM DỪNG" ở phía trên chính giữa màn hình (hoặc bấm phím ESC / P).
   - Khi tạm dừng, hộp thoại modal xuất hiện ở giữa màn hình:
     * Nút "RESUME / TIẾP TỤC": Tiếp tục trận đấu đang diễn ra.
     * Nút "RETURN TO MENU / QUAY LẠI MENU": Thoát trận đấu và quay trở lại `MenuScene`.

5. LUẬT CHƠI & CƠ CHẾ ĐỐI KHÁNG
--------------------------------
- Bàn cờ chuẩn kích thước 15 cột × 30 hàng.
- Luôn có 2 hàng rào chắn cố định ở giữa (hàng 14-15 trên bảng 15x30), luôn được lấp đầy hoàn toàn và KHÔNG THỂ BỊ HỦY.
- Hướng đẩy khi phá hàng: Khi người chơi phá hàng, toàn bộ cấu trúc (kèm 2 hàng rào chắn) sẽ ĐẨY VỀ PHÍA ĐỐI THỦ:
  * P1 (Top) phá hàng -> Đẩy dồn xuống phía P2 (-Y), thu hẹp diện tích đặt khối của P2.
  * P2 (Bottom) phá hàng -> Đẩy dồn lên phía P1 (+Y), thu hẹp diện tích đặt khối của P1.
- Cơ chế mất tim & Tái cân bằng (Rebalance):
  * Khi bất kỳ bên nào bị khối gạch chạm vào biên (P1 chạm cạnh trên hàng 29, hoặc P2 chạm cạnh dưới hàng 0) -> Bên đó bị trừ 1 tim!
  * 2 hàng rào chắn cố định di chuyển trở lại chính giữa bảng (hàng 14-15).
  * Bên bị trừ máu: XÓA HOÀN TOÀN 100% toàn bộ gạch của bên đó.
  * Bên không bị trừ máu (sống sót): XÓA 1/2 SỐ HÀNG GẠCH ở phía giáp hàng rào chắn; số gạch còn lại bị đẩy lùi sát vào giáp 2 hàng rào chắn cố định.
- Mỗi bên có 3 tim. Bên nào hết 3 tim trước thì đối phương giành chiến thắng.

6. HƯỚNG DẪN CHẠY GAME
----------------------
1. Mở Unity Editor và mở scene `Assets/Scenes/MenuScene.unity`.
2. Bấm nút Play (Ctrl + P).
3. Trò chơi khởi động với Tiếng Anh (English) mặc định.
4. Bấm nút "🌐 LANGUAGE" nếu muốn chuyển sang Tiếng Việt.
5. Bấm "▶ PLAY GAME" (hoặc "▶ BẮT ĐẦU CHƠI") để vào `GameplayScene`.
6. Trong trận đấu, bấm "⏸ PAUSE" hoặc phím ESC / P để kiểm tra hộp thoại tạm dừng.
