# Cấu trúc UI ĐÚNG của dự án (đích của refactor) – ĐỌC TRƯỚC KHI ĐỤNG VÀO UI

> File này là **đích đến**, không phải hiện trạng. Phần 6 ghi rõ hiện trạng đang lệch đích ở đâu.
> Nhật ký từng bước đã làm nằm ở `Docs/qua-trinh-khoi-phuc-va-core-gameplay.md` (mục 4–7).

---

## 1. Nguyên tắc (ngắn gọn, nhớ 5 dòng này)

1. **Mọi thứ nhìn thấy được trên UI là một prefab nằm sẵn trong Hierarchy** (`Systems/UI/...`), mở Editor là thấy, kéo thả sửa được ngay – KHÔNG dựng bằng `new GameObject()`.
2. **Mỗi prefab có đúng một script quản lý riêng** gắn trên gốc prefab, tên `XxxView` (`BoosterBarView`, `CapacityBarView`, `FailPopupView`…).
3. **Script đó giữ tham chiếu tới các con bằng Inspector** (`[SerializeField]`), KHÔNG đi tìm bằng `transform.Find("tên")` lúc chạy.
4. **Script đó phơi ra API nhỏ** (`Show/Hide/SetXxx`, `event Action OnXxxPressed`). Bên ngoài chỉ gọi API, không biết bên trong prefab có gì.
5. **`GameUI` chỉ là người điều phối mỏng**: giữ tham chiếu tới các View, nối sự kiện, ra lệnh. Không chứa `Find`, `Wire`, `SetText`.

Lợi ích: đổi tên / đổi chỗ / thêm bớt con trong prefab không làm vỡ code; nhìn Inspector là biết prefab cần những mảnh nào; mỗi View sửa độc lập, không đụng nhau.

---

## 2. Hierarchy đích

```
Systems
├── GameManager                  [GameManager]   ← ô "Ui" kéo tới Systems/UI
└── UI                           [GameUI]        ← điều phối
    │
    ├── OverlayCanvas            (Canvas Overlay – lớp phủ lên bàn chơi)
    │   ├── BoosterBar           [BoosterBarView]
    │   ├── Vignette
    │   └── Toast                [ToastView]
    │
    ├── HudCanvas                [HudView]       ← Lv badge + nút Settings   (HIỆN TẠI: chưa có View)
    ├── CapacityBar              [CapacityBarView]
    │
    ├── FailPopup                [FailPopupView]        (tắt sẵn)
    ├── SuccessPopup             [SuccessPopupView]     (tắt sẵn)
    ├── SettingsPopup            [SettingsPopupView]    (tắt sẵn)
    ├── OutOfCapacityPopup       [OutOfCapacityPopupView] (tắt sẵn; bọc OutOfCapacityPanel gốc)
    └── EventSystem                              (đặt sẵn)
```

- **Camera UI** (`UI Camera`, đứng xa bàn chơi ở x −1000) nằm trong `Camera & Lights`, gán vào ô `GameManager.uiCamera`. Mọi Canvas UI trong scene đã chọn sẵn `Screen Space - Camera` + camera này + `sortingOrder` (Overlay 5, HUD 10, popup 30) nên **Edit mode nhìn Game view giống hệt Play**. (Scene view vẽ chúng quanh x −1000, chọn UI rồi bấm `F` để tới.)
- Popup **tắt sẵn** (`SetActive(false)`) và được bật/tắt bằng `Show()/Hide()` của View. Không `Instantiate`/`Destroy` lúc chạy.
- Mỗi popup tự giữ Canvas riêng của nó (sortingOrder 30); `GameUI` chỉ gán camera cho Canvas (thứ không serialize được).

---

## 3. Khuôn mẫu một View (copy từ đây)

**Popup:** kế thừa `PopupViewBase` (`Scripts/Gameplay/UI/PopupViewBase.cs`) – lớp này lo sẵn bật popup, mở lại CanvasGroup, bật khung theo độ khó, hiệu ứng nảy vào, và `Hide()`. Popup con chỉ viết phần riêng: tham chiếu, sự kiện, `Show(...)`, `CollectMissing(...)`. Mẫu hoàn chỉnh: `FailPopupView.cs`.
**Không phải popup (HUD, thanh):** mẫu `HudView.cs`, `BoosterBarView.cs`, `CapacityBarView.cs`.

Khung chung tối thiểu:

Hai View đã làm đúng là mẫu: `Scripts/Gameplay/UI/BoosterBarView.cs`, `CapacityBarView.cs`.

```csharp
[DisallowMultipleComponent]
public class XxxPopupView : MonoBehaviour
{
    // 1) Tham chiếu con – kéo thả trong Inspector, KHÔNG Find theo tên
    [SerializeField] Button primaryButton;
    [SerializeField] Button closeButton;
    [SerializeField] TMP_Text levelText;
    [SerializeField] CanvasGroup group;

    // 2) Sự kiện ra ngoài
    public event Action OnPrimary;
    public event Action OnClose;

    void Awake()
    {
        primaryButton.onClick.AddListener(() => { AudioManager.Play(Sfx.Button); OnPrimary?.Invoke(); });
        closeButton.onClick.AddListener(() => OnClose?.Invoke());
    }

    // 3) API nhỏ
    public void Show(int level) { levelText.text = "Level " + level; gameObject.SetActive(true); /* anim vào */ }
    public void Hide()          { gameObject.SetActive(false); }

    // 4) Báo thiếu tham chiếu rõ ràng ngay khi dựng (không để null lặng lẽ)
    public string Validate() { /* liệt kê ô còn trống, trả null nếu đủ */ }
}
```

Quy tắc đi kèm:
- Có `Validate()`; `GameUI` gọi lúc dựng và `Debug.LogError` nếu thiếu → lỗi hiện sớm, ghi rõ ô nào.
- Thứ **không serialize được** (camera, font runtime…) thì nhận qua hàm `Bind(...)`, không tự `Find`.
- Tư thế (vị trí/xoay/cỡ) **nung sẵn trong prefab**, code không đặt lại lúc chạy (xem Docs 5.9 – từng hỏng vì để code chỉnh lúc chạy).
- Số tinh chỉnh (thời gian, góc, cỡ…) đặt trong `GameConfig` hoặc `[SerializeField]` của View, không `const` giấu trong code.

---

## 4. Cách thêm một panel / popup mới

1. Tạo prefab trong `Assets/_Project/UI/Prefabs/`, dựng giao diện trong Editor.
2. Viết `XxxView` theo khuôn mẫu mục 3, gắn lên gốc prefab, **kéo đủ tham chiếu**, bấm kiểm tra `Validate()`.
3. Kéo prefab vào `Systems/UI` trong scene, **để tắt sẵn** (nếu là popup).
4. Thêm vào `GameUI`: một trường `XxxView`, lấy bằng `GetComponentInChildren<XxxView>(true)`, nối sự kiện, và API `ShowXxx(...)`.
5. Thêm tên vào `GameUI.PopupNames` để chốt chặn "luôn tắt khi vào game" áp dụng cho nó.
6. Ghi một dòng vào Docs.

**Đừng:** `transform.Find("...")`, `Wire(...)`, `SetText(...)`, `new GameObject()` trong GameUI cho phần của panel mới.

---

## 5. Bẫy đã dính (để khỏi dính lại)

| Bẫy | Hậu quả | Cách tránh |
|---|---|---|
| Popup đặt sẵn trong scene nhưng lưu ở trạng thái **bật** (đã được `PopupSaveGuard` chặn khi lưu scene) | Prefab gốc có CanvasGroup alpha 0 nhưng `blocksRaycasts` vẫn bật → popup trong suốt phủ cả màn hình, **nuốt mọi cú bấm vào khay** (Docs 7.8c) | `GameUI.HideScenePopups()` tắt mọi `PopupViewBase` trong scene khi vào game (tự áp cho popup mới kế thừa lớp nền); vẫn nên lưu scene với popup tắt |
| Code chỉnh prefab lúc chạy (vị trí, tắt con…) | Edit mode thấy bản thô: thanh trôi giữa màn, lớp xám phủ cả màn, icon đè chữ (Docs 5.9) | Nung giá trị vào prefab; code chỉ gán thứ không serialize được |
| Code ghi đè bố cục/camera của UI lúc chạy (`UseCamera`, `Apply` đặt cỡ badge…) | Edit mode và Play lệch nhau: badge Lv 348×174 ở (137,−99) trong prefab nhưng 203×90 ở (40,−43) lúc chạy; canvas là Overlay trong Editor nhưng Screen Space-Camera lúc chạy (Docs 7.16) | Giá trị mặc định phải NUNG trong prefab/scene khớp với giá trị code đặt; code chỉ được đổi theo ngữ cảnh (độ khó…). Sau khi sửa: đo toạ độ màn hình ở Edit mode và Play, phải bằng nhau |
| Dò chỗ bấm theo toạ độ ô lưới / gốc xoay model | Hộp vẽ lệch khỏi ô → bấm hụt (Docs 7.8a, b) | Dò theo khung hình thật (`AmmoBox.TryGetVisibleBounds`) |
| `LoadLevel` ngay khung hình đầu | Canvas chưa có kích thước thật → `FitCamera` đo sai, bàn nhỏ hơn mức cần (Docs 5.9) | `Start()` là coroutine, `yield return null` trước `LoadLevel` |
| `GetPopupView<T>` / `Spawn()` dựng thêm từ prefab nếu không thấy bản đặt sẵn | Xoá nhầm popup khỏi scene → âm thầm dựng bản thứ hai lúc chạy (vẫn chạy được nhưng ẩn lỗi) | Chạy xong kiểm tra mỗi loại View chỉ có đúng 1 bản trong scene |
| Popup đặt sẵn được DÙNG LẠI qua nhiều lần mở | Code hiệu ứng cũ giả định mỗi lần là bản mới: vị trí đống xu cộng dồn `CoinLift`, mưa pháo bị `Destroy` nên lần thắng sau mất | Ghi giá trị gốc ở `Awake`, đặt lại từ đó; tắt (`SetActive(false)`) thay vì `Destroy` |
| Nối tham chiếu bằng script Editor rồi tin chuỗi tự kiểm tra | Chuỗi in `False` dù đã nối đúng (và ngược lại) | Đọc lại `SerializedObject` của **asset đã lưu** mới là bằng chứng |

---

## 6. Hiện trạng so với đích (cập nhật mục này mỗi khi làm xong một cái)

| Thành phần | Có trong Hierarchy | Có View riêng | Còn `Find`/`Wire` trong GameUI |
|---|---|---|---|
| OverlayCanvas + BoosterBar | ✅ | ✅ `BoosterBarView` | không |
| CapacityBar | ✅ | ✅ `CapacityBarView` | không |
| Vignette | ✅ (con của Overlay) | ❌ (GameUI giữ `Image _vignette`) | không |
| Toast | ✅ (con của Overlay) | ❌ (GameUI giữ `TMP _toast`) | không |
| HUD (Lv + Settings) | ✅ đặt sẵn | ✅ `HudView` | không |
| FailPopup | ✅ đặt sẵn, tắt | ✅ `FailPopupView` (kế thừa `PopupViewBase`) | không |
| SuccessPopup | ✅ đặt sẵn, tắt | ✅ `SuccessPopupView` (sở hữu cả chuỗi màn thắng) | không |
| SettingsPopup | ✅ đặt sẵn, tắt | ✅ `SettingsPopupView` | không |
| OutOfCapacityPopup | ✅ đặt sẵn, tắt (prefab bọc Canvas + panel gốc lồng bên trong) | ✅ `OutOfCapacityPopupView` | không |

Số liệu hiện tại: `GameUI.cs` **419 dòng** (từ 920). Không còn `Wire(`/`SetText(`/`Find(` đệ quy. Còn 3 chỗ tìm theo tên để lấy *vật chứa* (không phải để chọc vào bên trong): `Spawn()` cho HUD/CapacityBar/OverlayCanvas, và `Vignette`/`Toast` trong `OverlayCanvas`.

### Cách XEM / SỬA một popup trong Editor
Popup nằm sẵn trong Hierarchy nhưng **tắt**, nên chọn vào thì Scene view trống – đó là thiết kế (chỉ hiện khi game gọi `Show`). Để xem:
1. Chọn popup (vd `FailPopup`) → ở Inspector của View bấm **"Xem trước"** (bật + đưa Scene view tới đúng chỗ, chuyển 2D). Hoặc mở Game view: Canvas Overlay hiện đầy màn hình ở đó.
2. Sửa xong bấm **"Tắt xem trước"**. Quên cũng không sao: **lưu scene sẽ tự tắt mọi popup đang bật** (`PopupSaveGuard`, ghi log `[PopupSaveGuard]`).
3. Hoặc mở prefab (mũi tên bên phải tên trong Hierarchy, hay double-click prefab trong Project) để sửa trong Prefab Mode.
Vì sao trước đây "bật lên vẫn không thấy": (a) `FailCanvas`/`SuccessCanvas` gốc để `CanvasGroup` alpha = 0 – nay đã nung alpha 1 (runtime `OpenBase` vẫn ép lại 1 nên không đổi hành vi); (b) Canvas Overlay vẽ trong Scene view thành khung khổng lồ (1273×2688 đơn vị) ở gốc toạ độ, còn bàn chơi chỉ ~10 đơn vị nên phải `F`/nút "Xem trước" mới thấy.
Code: `Assets/_Project/Editor/PopupViewEditor.cs`.

### Bản đồ: mỗi UI nằm ở ĐÂU khi nhìn trong Scene view (Edit mode)
Không phải UI nào cũng nằm trong "khung Canvas trắng" ở gốc toạ độ – có 3 loại:
| Loại | Gồm | Nhìn thấy ở đâu |
|---|---|---|
| **Overlay** (phủ màn hình) | `OverlayCanvas` (BoosterBar, Vignette, Toast), `HUD (GameCanvas)` (badge Lv + nút Settings) | Trong khung Canvas trắng ở gốc toạ độ (1273×2688 đơn vị) và đầy màn ở Game view |
| **World Space** | `CapacityBar` (thanh sức chứa) | KHÔNG nằm trong khung Canvas trắng: nó là vật thể 3D ở toạ độ thế giới (5.30, −6.77), ngay dưới bàn chơi – chọn `Level` rồi bấm `F` để tới đó. Cố ý (game gốc đặt nó dưới đáy bàn, co giãn theo camera); đổi sang Overlay sẽ làm đổi bố cục và phép cân camera |
| **Popup tắt sẵn** | `FailPopup`, `SuccessPopup`, `Settings`, `OutOfCapacityPopup` | Không vẽ gì cho tới khi bấm **Xem trước** (xem mục trên) |
Một số phần tử Overlay cố ý trong suốt/rỗng ở Edit mode: `Vignette` đỏ (alpha màu 0, chỉ hiện khi sức chứa ≥ 75%) và `Toast` (chữ rỗng, chỉ hiện khi có thông báo).
Cách kiểm nhanh vì sao một UI "không hiện": đi từ chính nó lên gốc, tìm node nào **tắt** hoặc có `CanvasGroup` **alpha 0** (prefab rip từ game gốc hay để alpha 0 vì script gốc tự fade-in, còn ta không có script đó).

### Thứ tự làm đề xuất (nhỏ → lớn)
1. ~~`HudView`~~ ✅ xong.
2. ~~`FailPopupView`~~ ✅ xong (đồng thời tạo lớp nền `PopupViewBase` cho 3 popup còn lại).
3. ~~`OutOfCapacityPopupView`~~ ✅ xong (kèm prefab bọc `OutOfCapacityPopup.prefab` để Canvas nằm trong prefab, không dựng host bằng code).
4. ~~`SettingsPopupView`~~ ✅ xong (3 công tắc tự giữ trạng thái, báo ra ngoài bằng sự kiện).
5. ~~`SuccessPopupView`~~ ✅ xong (cả chuỗi logo → pháo → bảng thắng → xu rơi nay nằm trong View).
6. ~~Dọn `GameUI`~~ ✅ xong phần popup: xoá `OpenPopup`/`PopIn`/`Wire`/`SetText`/`Find`/`Img`/`Txt`… (920 → 419 dòng). **Còn lại:** `Spawn()` vẫn tìm HUD/CapacityBar/Overlay theo tên.
7. Đưa `Vignette` và `Toast` thành View (`ToastView.Show(msg, sec)`).

Mỗi bước xong: compile sạch → vào Play mở panel đó → `Validate()` trả null → chụp ảnh đối chiếu → ghi Docs → tick lại bảng ở mục 6.

---

## 7. Những thứ KHÔNG nên tách (có lý do)

- **Mesh sinh theo level** (khối gộp chữ L/T, mặt khay, miệng hố, tường): hình dạng phụ thuộc dữ liệu level nên không đặt sẵn prefab được. Thông số hình khối đã ra `GameConfig`.
- **Ống thạch gắn xương** (`AmmoFlightTube`): uốn theo quỹ đạo lúc chạy.
- **`FitCamera`**: toán theo kích thước màn hình.
- **`EventSystem`**: hạ tầng, chỉ tạo khi scene chưa có.
- **Particle/confetti dựng bằng code** (`UiConfetti`, `UiSparkles`): gốc dựng lại theo thông số game gốc vì prefab gốc đã mất component; nếu tách thì làm sau cùng.
