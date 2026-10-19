# Cấu trúc UI ĐÚNG của dự án (đích của refactor) – ĐỌC TRƯỚC KHI ĐỤNG VÀO UI

> File này là **đích đến**, không phải hiện trạng. Phần 6 ghi rõ hiện trạng đang lệch đích ở đâu.
> Nhật ký từng bước đã làm nằm ở `Docs/lich-su/qua-trinh-khoi-phuc-va-core-gameplay.md` (mục 4–7).

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

## Cập nhật cuối (sau tái cấu trúc)
Vignette/Toast đã do `OverlayView` quản lý; `Spawn()` đã bỏ; `GameUI` có 7 ô View kéo sẵn (Overlay, Hud, Capacity, Fail, Success, Settings, OutOfCapacity) và chỉ tự dựng từ prefab khi ô trống. Các bảng trạng thái cũ phía trên coi như **đã hoàn thành**. Runtime còn tự tạo: `ArenaWalls`, `FlattenStaticScene` (cố ý).

### Bẫy đã dính: popup đặt sẵn không bấm được nút
`GameUI.ShowXxx` trước đây nối sự kiện (`OnClose`, `OnRetry`, `OnContinue`…) trong khối `if (_view == null)`. Khi View được kéo sẵn vào ô `[SerializeField]` thì ô không null → khối không bao giờ chạy → nút bấm không làm gì. Đã đổi sang cờ `_failWired/_outWired/_settingsWired/_successWired` (nối đúng 1 lần). **Quy tắc:** nối sự kiện theo cờ "đã nối", không dựa vào "ô còn trống". Đã kiểm bằng click giả trong Play: Settings (mở/đóng/Restart), Fail Try Again, OutOfCapacity Play On, Success Continue đều chạy.

## MainMenuCanvas (scene MenuGame)
*(Lịch sử – bố cục hiện tại xem mục "Tách MainMenuCanvas…" bên dưới.)* Tách từ `MainMenuCanvas` của ManagerScene gốc bằng `Tools/UnityYaml/ui_extract.py` (tham số mới `--skip` bỏ nhánh Shop / Leaderboards / Collections; `--assets-out` mặc định `Art/UI/Menu/Extra`). Script KPFramework bị bỏ, logic viết lại trong `MainMenuView` (giữ ô tham chiếu bằng Inspector, `Validate()`, sự kiện `OnPlay/OnComingSoon/OnSoundChanged…`) và `MenuManager` (điều phối). 41 sprite/texture menu lấy từ `_Archive` ra `Art/UI/Menu/{Sprites,Textures}`. Toast sao chép từ OverlayCanvas. Canvas Screen Space - Camera trỏ sẵn Main Camera của scene (order giữ 3 như gốc – các canvas con override order tuyệt đối nên KHÔNG đổi order gốc). Nút bấm của View vẫn nối bằng `AddListener` trong `Awake`.

### Trang Shop + thanh Bounce Streak (MenuGame)
- **Shop**: tách nguyên `Shop Panel` của ManagerScene gốc → prefab `Prefabs/UI/ShopCanvas.prefab` (2345 object; bỏ script IAP/KPFramework/UIParticle; gốc có Canvas riêng), đặt dưới `UI/`, là trang của tab 0. Mỗi gói (6 bundle + 6 gói xu, đúng thứ tự gốc) có component `ShopOfferItem` gắn ngay trên object của gói (`Content/IAP Content - …`): **dữ liệu** (tên, chuỗi giá, xu, số booster mỗi loại, phút tim vô hạn, No Ads) + tham chiếu hình. Chữ và ô bật/tắt của gói **lưu sẵn trong prefab**; sửa số trong Inspector thì chữ tự đổi theo (chỉ trong Editor, hoặc chuột phải ▸ "Ghi dữ liệu ra hình"). `ShopPanelView` chỉ gom các gói + số xu đầu trang + ô Free Coin. Ô cuối "Free Coin" (gốc là xem quảng cáo): 50 xu, hồi 24h, có đồng hồ đếm ngược.
- **Không có mua hàng thật**: bấm mua là `MenuManager.Buy` cộng luôn phần thưởng (xu → `PlayerData.Coins`, booster → `BoosterSystem.Add`, tim vô hạn → `PlayerData.AddInfiniteHearts`, cờ `PlayerData.NoAds`).
- Dữ liệu gói: Starter (3 000 xu, x1 booster, 1h, 137.000 ₫), Small (2 500, 30m, 156.000 ₫), Medium (7 500, x2, 2h, No Ads) lấy theo ảnh gốc; giá Medium và các gói còn lại là **tự đặt** (ảnh gốc không chụp tới) – sửa trong Inspector của `Shop Panel`.
- Nút "+" cạnh xu/tim ở Home (`HomeView.shopOpeners`) mở tab Shop như gốc; nút hồ sơ vẫn báo "coming soon".
- **Bounce Streak**: thanh tách từ `MainPanelCanvas/MainPanelBarMain` trong `Resources/asyncload/BounceStreakLoadAsync.prefab` gốc (đây mới là thanh trên Home; `RewardBarArea` là thanh trong popup – lần đầu lấy nhầm) → `Prefabs/UI/StreakMainBar.prefab`, đặt ở `Main Panel/Container/Canvas` (y −306 so với đỉnh vùng an toàn), script `StreakBarView` (thanh Filled, chữ x/y, đồng hồ đợt sự kiện, tim vô hạn + băng đỏ, bấm khi đầy để nhận). Luật trong `Gameplay/Core/BounceStreak.cs`: mốc 1/3/5/10 trận thắng liên tiếp → 10m/30m/1h/2h tim vô hạn; thua (`GameManager` lúc Lose) là về mốc đầu; mỗi đợt 4 ngày, hết đợt thì chuỗi về 0.
  - **Bẫy sprite bị cắt sát:** bản rip cắt sprite trong atlas sát mép (mất lề trong suốt của sprite gốc): `MainMenuBarGreen` còn 799×52, `RedBanner (1)` còn 618×242. Gốc kéo chúng phủ cả khung → ở bản rip thành khối chữ nhật / bị giãn. Cách sửa: đặt kích thước = kích thước sprite × tỉ lệ khung (1.1), đo vị trí theo ảnh gốc. Huy chương `capsule` lưu scale 1.8 (trạng thái hoạt ảnh) → đặt 1.15 theo ảnh.
  - **Safe area:** gốc gắn `KPSafeArea` (thân script mất khi rip) trên `Main Panel/Container` → viết lại `SafeAreaFitter`. Ảnh gốc chụp máy có tai thỏ (~124 đơn vị) nên hàng avatar/xu/tim và thanh Streak trong ảnh thấp hơn Editor đúng chừng đó.
- **Popup Profile**: tách `ProfileSystem/SelectProfileCanvas` gốc → `Prefabs/UI/ProfileCanvas.prefab` (order 30, lưu TẮT), đặt dưới `UI/`, script `ProfilePopupView`: ô tên (tối đa 14 ký tự như gốc), lưới 20 avatar (thứ tự theo mảng `avatarSprite` của `KPProfileEditPanel` gốc; ô chọn hiện khung cam + dấu tích, to 1.1), Save / Close. Ô avatar vàng (`GoldenProfile`, phần thưởng sự kiện) tắt. Lưu ở `PlayerData.ProfileName` / `PlayerData.Avatar`; avatar ở Home (`HomeView.avatarImage`) đổi theo. 19 ảnh `Profile-2…20` chép từ `Resources/profileicons` của bản rip vào `Art/UI/Profile/Sprites` (giữ GUID; game gốc nạp lúc chạy nên tool tách không tự kéo theo).
- `Prefabs/UI/StreakRewardBar.prefab` (thanh lấy nhầm) không còn ai dùng – xoá tay được (lệnh xoá qua MCP bị chặn).
- Ô đồng hồ tim ở Home (`heartTimerText`) hiện "Full", hoặc thời gian tim vô hạn còn lại. Avatar `Profile-1` đã bật (trước bị tắt `Panel`).
- 41 sprite/texture/anim Shop dùng tới đã chuyển từ `_Archive` ra `Art/UI/Shop/{Sprites,Textures,Animations}` (giữ GUID).
- Chưa làm: Journey, Collection (vẫn "coming soon"), Support/Terms/Privacy/Restore trong Settings, 6 nút sự kiện hai bên Home, hoạt ảnh tăng thanh Streak (cờ x25, pháo sáng).

### Tách MainMenuCanvas thành các prefab đặt cạnh nhau (theo đúng nguyên tắc mục 1)
Trước đây Home, Settings, thanh tab, Toast (và Shop lúc mới thêm) cùng nằm trong một prefab `MainMenuCanvas` với một script `MainMenuView` ôm hết – sai nguyên tắc "mỗi panel một prefab + một View". Đã tách:

| Prefab (`Prefabs/UI/`) | View | Nội dung | Sorting order |
|---|---|---|---|
| `MenuHomeCanvas` (đổi tên từ MainMenuCanvas, giữ GUID) | `HomeView` (đổi từ `MainMenuView`, giữ GUID script) | Nút Play, xu/tim, avatar, thanh Bounce Streak (`StreakMainBar` lồng – là một phần của trang Home), `SafeAreaFitter` trên Container | 3 |
| `ShopCanvas` | `ShopPanelView` | Trang Shop | 3 |
| `MenuSettingsCanvas` | `MenuSettingsView` | Nền caro `BG (1)` + trang Settings (3 công tắc) | 3 |
| `MenuBottomBar` | `BottomBarView` | Thanh 5 tab + dải sáng | 5 |
| `ProfileCanvas` | `ProfilePopupView` | Popup Profile (bấm avatar) – lưu TẮT | 30 |
| `MenuToast` | `ToastView` | Dòng thông báo | 50 |

- Ba trang (Home/Shop/Settings) cùng order 3 vì mỗi lúc chỉ bật một; thanh tab và Toast luôn ở trên.
- `MenuManager` giữ 5 ô View + mảng `pages` (cùng thứ tự tab: Shop, –, Home, –, Settings; ô trống = "coming soon") và tự bật/tắt trang; các View không biết nhau.
- Canvas gốc mỗi prefab: Screen Space - Camera, plane distance 1; ô camera nối trong scene. **Bẫy đã dính:** prefab gốc lưu Overlay (scene mới override sang Camera) → prefab mới chép phải Overlay, ảnh chụp camera không thấy gì. Giờ prefab đã lưu đúng Screen Space - Camera.

### Quy tắc: hình tĩnh nằm trong prefab, code chỉ đổi thứ thay đổi lúc chơi
User yêu cầu: mở Hierarchy phải thấy đúng UI sẽ hiện, không để code bật/tắt hay ghi chữ cố định lúc chạy (về sau update khó tìm). Vì vậy:
- **Lưu sẵn trong prefab** (sửa thẳng trong Editor): mọi sprite, kích thước, vị trí, chữ cố định, ô nào bật/tắt (ruy băng "Best Offers", mục Support/Terms/Privacy/Restore, ô avatar vàng, băng đỏ của thanh Streak, ô thưởng của từng gói Shop…), ảnh 20 avatar, alpha 0 của Toast.
- **Code chỉ đổi lúc chạy** – và chỉ những thứ này:

| View | Đổi lúc chạy | Vì sao phải là code |
|---|---|---|
| `HomeView` | Chữ "Level N", nút Play nào hiện (theo độ khó), số xu, số tim / "Full" / giờ tim vô hạn, ảnh avatar | theo dữ liệu người chơi |
| `StreakBarView` | Độ đầy thanh, chữ x/y, chữ phút thưởng, đồng hồ đợt, nhịp phóng to khi nhận được | theo tiến độ + thời gian thật |
| `ShopPanelView` | Số xu đầu trang, nút Claim ↔ đồng hồ của ô Free Coin | theo dữ liệu + thời gian thật |
| `ProfilePopupView` | Ô đang chọn (khung cam + dấu tích + ×1.1), ảnh lớn, ô tên | theo lựa chọn của người chơi |
| `BottomBarView` | Icon tab đang chọn phóng to, hiện tên tab, dải sáng trượt | theo tab đang chọn |
| `MenuSettingsView` | Hình bật/tắt của 3 công tắc | theo cài đặt đã lưu |
| `MenuManager` | Bật đúng một trang (Home/Shop/Settings), mở/đóng popup Profile | điều hướng |
| `SuccessPopupView` | Bật/tắt theo trình tự (logo → bảng), chữ Level/xu, nảy bảng, đống xu rơi, chớp sáng nhấp nháy, gọi pháo / sao / vệt sáng | trình tự thời gian |
| `UiConfetti` | Sinh **hạt** pháo giấy trong pool (giống ParticleSystem, không lưu vào scene – `HideFlags.DontSave`) | hạt hiệu ứng; mọi thông số (số mảnh, cỡ, tốc độ, sống, trọng lực, màu) ở Inspector |
| `UiSparkles` | Bật 10 ngôi sao (đặt sẵn trong prefab) ở vị trí/cỡ ngẫu nhiên, cho hiện–tắt | hiệu ứng |
| `UiShineSweep` | Bật + trượt dải sáng (đặt sẵn `ShineMask/ShineBand` dưới nút) | hiệu ứng |
| `BoosterBarView` | Hình 4 nút + ô số + màu dải theo độ khó (dữ liệu từng độ khó trong Inspector: Normal / Hard / Super Hard) | theo độ khó level |
| `HudView` | Badge Level, nút Settings theo độ khó (dữ liệu trong Inspector) | theo độ khó level |
| `CapacityBarView` | Độ đầy, màu thanh theo %, chữ %, icon vô hạn | theo tiến độ |
| `SafeAreaFitter` | Neo của `Main Panel/Container` – chạy cả trong **Edit mode** (`[ExecuteAlways]` + `DrivenRectTransformTracker`, như Layout Group): Edit và Play giống nhau, Inspector khoá xám ô neo, giá trị không lưu vào scene/prefab | vùng an toàn khác nhau theo từng máy |

- Thêm UI mới: cái gì không phụ thuộc dữ liệu/thời gian thì đặt thẳng trong prefab, KHÔNG viết `SetActive`/`text =`/`sprite =` trong `Awake`.
- **Đã kiểm (2026-10-19):** chụp toàn bộ 869 RectTransform dưới `UI/` lúc Edit và lúc Play (Game view máy 1080×2280 có tai thỏ) → chỉ dải sáng tab (`hover`) lệch vài đơn vị do trượt theo tab; còn lại trùng khớp. Trước khi sửa, `Container` nhảy xuống ~5% khi Play vì `SafeAreaFitter` chỉ chạy lúc Play.
- Muốn xem UI trên máy không tai thỏ: đổi Game view sang độ phân giải thường (VD 1242×2688) – safe area là cả màn hình nên Container phủ kín.

### Đợt rà toàn dự án (2026-10-19) – "mọi UI chỉnh được trong Hierarchy"
Cách kiểm: chụp toàn bộ RectTransform của scene lúc Edit, lúc Play (đầu màn), lúc mở từng popup, lúc chạy màn Thắng → so khác nhau và đếm object mới sinh. Kết quả sau khi sửa:

| Scene / lúc | Object UI mới sinh | UI bị đổi khác Edit |
|---|---|---|
| MenuGame – Play | 0 | 0 (trừ dải sáng tab trượt vài đơn vị) |
| Gameplay – đầu màn | 0 (trước: 0) | 0 (trước: 6 – chữ thanh Capacity bị scale, CheatPanel bị tắt…) |
| Gameplay – màn Thắng | 0 (trước: 18 – lớp sao, mặt nạ/vệt sáng nút, ảnh chớp sáng) | chỉ trạng thái hoạt ảnh (trước: ~25 giá trị hình bị ghi đè) |

Đã chuyển từ code vào prefab/scene:
- **GameUI** không còn: tự tạo EventSystem, tự dựng popup từ prefab khi ô trống, đặt chế độ vẽ / camera / khoảng cách / thứ tự / CanvasScaler cho mọi Canvas. Tất cả lưu sẵn: chế độ vẽ + thứ tự + khoảng cách trong prefab, ô camera (UI Camera) trong scene. Ô View trống → báo lỗi rõ thay vì tự dựng.
- **GameManager** không tự tạo GameUI / UI Camera nữa (bắt buộc đặt sẵn). Các hệ thống bàn chơi (Board, Balls…) vẫn có dự phòng – không phải UI.
- **CapacityBar**: camera game + thứ tự 4 của Canvas thế giới (scene), font/màu chữ, bù dẹt chữ scale y 1.33, icon vô hạn tắt (prefab).
- **BoosterBar**: màu dải nền 3 độ khó vào `Style.stripColor` (trước viết cứng trong GameUI).
- **BoosterBuyPopup**: nút quảng cáo `RV` tắt sẵn; Canvas như các popup khác.
- **SuccessCanvas**: khối live-ops tắt sẵn, Dim xếp dưới cùng, Animator logo tắt, cỡ chữ logo (sprite × 1.29), AreaGlow bật / AreaShine tắt, chỗ dừng đống xu (+45, ×0.85), thứ tự vẽ hạt, `preserveAspect` cho sprite bị cắt, pháo nổ có `UiConfetti` + Canvas lồng, mưa pháo nằm ở gốc canvas, pháo hoa SF_Basic (World, scale 134.4, y −1150, duration 0.45, radius 3.5), 3 ảnh chớp sáng `CoinFlash / CoinFlashCore / CoinRays`, `Coin/SparkleLayer` + 10 sao, `ShineMask/ShineBand` dưới Continue và X2.
- **CheatPanel**: `Panel` lưu tắt (mở bằng 5 chạm / 3 ngón / F1). Hàng nút cheat vẫn sinh từ `CheatRegistry` (danh sách viết bằng code) theo mẫu `buttonTemplate`/`stepperTemplate` đặt sẵn – đổi giao diện hàng thì sửa mẫu.
- Thêm nút/hiệu ứng mới: `UiShineSweep` ▸ chuột phải "Tạo lớp vệt sáng", `UiSparkles` ▸ "Tạo sao" – dựng sẵn trong Editor, lưu vào prefab.
