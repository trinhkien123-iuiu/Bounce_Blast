# QUY ƯỚC CODE — Bounce Blast

Ngắn gọn, áp dụng cho mọi script trong `Assets/_Project/Scripts` và `Assets/_Project/Editor`.

## 1. Định dạng
- Thụt lề 4 dấu cách, ngoặc nhọn xuống dòng (Allman). Mỗi file một lớp chính, tên file = tên lớp.
- Namespace theo thư mục: `BounceBlast.Gameplay` (Scripts/Gameplay), `BounceBlast.UI` (Scripts/UI), `BounceBlast.Menu`, `BounceBlast.Cheats` (Scripts/Debug – KHÔNG đặt tên `…Debug` vì che mất `UnityEngine.Debug`), `BounceBlast.EditorTools` (Editor).
- Mỗi `MonoBehaviour`/`ScriptableObject` một file, tên file = tên lớp (hai lớp chung file thì Unity không gắn được script vào prefab). File quá ~600 dòng thì chia `partial` theo mảng việc (`Lop.MangViec.cs`).
- Code chỉ dùng khi test (cheat) bọc `#if UNITY_EDITOR || DEVELOPMENT_BUILD || BB_CHEATS`. Lớp, hàm, thuộc tính: `PascalCase`; biến private: `_camelCase`; biến cục bộ/tham số: `camelCase`; hằng: `PascalCase`.
- Trường public/Inspector của ScriptableObject cấu hình (`GameConfig`) có thể là `camelCase` công khai (đã dùng sẵn).

## 2. Chú thích
- Mọi lớp và hàm public có `/// <summary>` (tiếng Việt, nói **vì sao / làm gì**, không chép lại tên).
- Trường chỉnh trong Inspector phải có `[Tooltip]` (hoặc `[Header]` nhóm) để người sửa hiểu ngay.
- Lý do cho "số lạ" ghi ngay cạnh số, hoặc đưa ra `GameConfig`.

## 3. Không hard-code asset
- Cấm `Resources.Load`, cấm đường dẫn chuỗi tới asset. Mọi prefab/material/sprite/âm thanh là ô `[SerializeField]`/public trong `GameConfig` hoặc `UiSkin` (kéo thả trong Inspector).
- Số tinh chỉnh (kích thước, tốc độ, màu…) → `GameConfig` hoặc `[SerializeField]`; không giấu bằng `const` trong code, và code không được ghi đè giá trị đã chỉnh trong scene.

## 4. Mẫu View cho UI
- Mỗi panel = 1 prefab đặt sẵn trong `Systems/UI` + 1 script `XxxView`:
  - `[SerializeField]` giữ mọi tham chiếu con; `Validate()` trả về chuỗi tên ô còn thiếu (hoặc null).
  - Chỉ lộ API/sự kiện (`OnXxxPressed`, `SetXxx`), không để bên ngoài mò vào con.
  - Popup kế thừa `PopupViewBase`; popup đặt sẵn phải được lưu ở trạng thái **tắt**.
- `GameUI` chỉ điều phối (gán camera Canvas, nối sự kiện), không chứa chi tiết bên trong panel.
- Không `Find("tên")` / `transform.Find("đường/dẫn")` để lấy con của prefab của mình; kéo tham chiếu.

## 5. Hiệu năng
- Không cấp phát trong `Update` (không `new List`, LINQ, nối chuỗi); cache `Camera.main`, `GetComponent`.
- Đối tượng sinh/huỷ liên tục (bi, hạt) dùng pool hoặc tái dùng; popup tái dùng (tắt thay vì `Destroy`).
- Vật liệu dùng chung: dùng `sharedMaterial`; chỉ `material` khi thật sự cần bản riêng, và nhớ huỷ.

## 6. Assembly
- `BounceBlast.Runtime` cho `Scripts/`, `BounceBlast.Editor` cho `Editor/` (chỉ nền tảng Editor). Code runtime **không** tham chiếu `UnityEditor` (bọc `#if UNITY_EDITOR` nếu bắt buộc).
- Thêm package mới cần dùng trong script → thêm vào `references` của asmdef.

## 7. Công cụ Editor
- Công cụ chạy một lần thì bọc `#if <CỜ>` và ghi chú đầu file là đã chạy; menu ở `Tools/Bounce Blast/...`.
- Di chuyển/đổi tên asset bằng `AssetDatabase.MoveAsset` hoặc kéo trong Unity để giữ GUID.

## 8. Git & tài liệu
- Mỗi thay đổi lớn: ghi vào `Docs/` (vì sao, đo đạc, cách kiểm). Số liệu "đã kiểm" phải đo lại từ asset đã lưu.
- Commit nhỏ theo chủ đề; không commit `Library/`, `Captures/`, file tạm.

## Comment
- Không comment trong thân hàm hay cuối dòng.
- Mỗi class / hàm chính chỉ một dòng `/// <summary>` nói nó xử lý chức năng gì hoặc dùng cho cái gì.
- Chi tiết dài (luật, số đo từ game gốc) ghi vào `Docs/`, không ghi trong code.
