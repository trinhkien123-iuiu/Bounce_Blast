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
    └── OutOfCapacityPopup       [OutOfCapacityPopupView] (tắt sẵn)
EventSystem                                      (đặt sẵn, ở GỐC scene – không nằm trong UI)
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
| `MenuManager` | Mở/đóng popup Profile, nối tab ↔ trang | điều hướng |
| `MenuPager` | Vuốt ngang chuyển trang: dời `content` của trang theo ngón tay, trượt nốt rồi trả về x = 0; bật đúng một trang | điều hướng (thao tác người chơi) |
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
- **SuccessCanvas**: khối live-ops tắt sẵn, Dim xếp dưới cùng, Animator logo (bật bởi code khi thắng, UnscaledTime), cỡ chữ logo (sprite × 1.29), AreaGlow bật / AreaShine tắt, chỗ dừng đống xu (+45, ×0.85), thứ tự vẽ hạt, `preserveAspect` cho sprite bị cắt, pháo nổ có `UiConfetti` + Canvas lồng, mưa pháo nằm ở gốc canvas, pháo hoa SF_Basic (World, scale 134.4, y −1150, duration 0.45, radius 3.5), 3 ảnh chớp sáng `CoinFlash / CoinFlashCore / CoinRays`, `Coin/SparkleLayer` + 10 sao, `ShineMask/ShineBand` dưới Continue và X2.
- **CheatPanel**: `Panel` lưu tắt (mở bằng 5 chạm / 3 ngón / F1). Hàng nút cheat vẫn sinh từ `CheatRegistry` (danh sách viết bằng code) theo mẫu `buttonTemplate`/`stepperTemplate` đặt sẵn – đổi giao diện hàng thì sửa mẫu.
- Thêm nút/hiệu ứng mới: `UiShineSweep` ▸ chuột phải "Tạo lớp vệt sáng", `UiSparkles` ▸ "Tạo sao" – dựng sẵn trong Editor, lưu vào prefab.

### Vuốt ngang chuyển trang (MenuGame)
- `UI/MenuPager` (script `MenuPager`) giữ danh sách trang theo tab: `root` (Canvas gốc của trang), `content` (con phủ kín được trượt – Home: `MenuCanvas`, Shop: `IN APP PURCHASE`, Settings: `Content` mới gom `BG (1)` + `Settings Panel`), `group` (CanvasGroup trên gốc trang).
- Kéo ngang quá 25 px → trang đi theo ngón tay, trang kế lộ ra; thả quá 25% bề ngang hoặc vuốt nhanh → sang trang, không thì trượt về. Tab chưa có trang (Journey, Collection) bị bỏ qua. Hết trang thì chỉ kéo được 15% rồi bật lại. Bấm tab cũng trượt sang.
- Khi đang vuốt: `CanvasGroup.interactable = false` để cú thả không thành cú bấm nút; ScrollRect dọc của trang (Shop) tạm khoá. Kéo dọc trước thì là cuộn, không vuốt.
- Popup Profile mở → khoá vuốt. Thông số (ngưỡng, % chuyển trang, thời gian trượt, độ cản ở mép) chỉnh trong Inspector của `MenuPager`.
- Thanh tab: tên tab Pos Y 83 (prefab); icon tab đang chọn Pos Y 68, tab khác 0 (`BottomBarView.selectedIconY / iconY`).

### Dọn prefab MenuGame (2026-10-20)
Prefab tách từ game gốc mang theo nhánh của tính năng gốc không có trong bản dựng lại, lồng sâu và tên khó hiểu. Đã dọn bằng công cụ `Assets/_Project/Editor/UiTidy.cs`
(`Dump` in cây kèm cờ, `Delete` / `Flatten` / `RemoveNestedCanvas` / `Rename` đều kiểm tra trước, `ShotMenu` chụp 5 trạng thái để so):

| Prefab | Trước → sau (object) | Sâu nhất | Đã bỏ |
|---|---|---|---|
| MenuHomeCanvas | 113 → 52 (gồm StreakMainBar 40 → 16) | 12 → 10 | hiệu ứng xu bay, nút ưu đãi `MainPanelGrid`, thẻ sưu tập trên nút Play, nút Setting cũ, nền tiền tệ Hard/Super Hard, hoạt ảnh hoàn thành / x1–x100 của thanh Streak |
| MenuSettingsCanvas | 122 → 61 | 14 → 10 | khối popup gốc (header, "+20 Capacity", nút đóng), nút mở debugger + ô mật khẩu, thanh xu ẩn, icon tắt |
| ShopCanvas | 601 → 296 | 13 → 10 | 4 Canvas trạng thái IAP, hạt lấp lánh ở mọi ô thưởng, khung phụ / lớp mờ của nút mua, ruy băng, 2 thanh cuộn ẩn |
| MenuBottomBar | 32 → 27 | 7 → 4 | huy hiệu thông báo đang tắt |
| ProfileCanvas | 92 → 80 | 9 | ô avatar vàng, vòng quay tải, dấu tick, 2 thanh cuộn ẩn |

- Giữ lại: những thứ tắt nhưng code bật theo trạng thái (nút Play Hard/Super Hard, ô No Ads, nút đếm ngược Free Coin…) và tính năng sẽ làm (Support / Terms / Privacy / Restore).
- Canvas lồng chỉ còn cái override thứ tự vẽ (TopBar, StreakMainBar, thanh tab).
- Tên mới thống nhất (tiếng Anh, PascalCase): Home `Content/Viewport/SafeArea/{PlayButtons/{Normal,Hard,SuperHard}, TopBar/{Coins,Hearts,ProfileButton,StreakMainBar}}`;
  Streak `Bar/{Frame/{Fill/Progress, TimerTab/Timer}, Reward/{Icon,Banner/Red,Duration}, Medal/Icon}`;
  Settings `Content/{Background, Mask/{Header/TopBar, Popup/Panel/Window/Body/Rows/{Sound,Music,Vibration}/{Label,Toggle,On,Off}}}`;
  Shop `Content/{Body/ScrollView/Viewport/Items/{Header_*, Bundle_Starter…Mega, CoinPack_1…6, FreeCoins}/Card/{Background,BuyButton/Price,Title,Coins,Boosters/{MagicHand,Hammer,Shuffle,Capacity},Extras/{InfiniteHearts,NoAds}}, TopBar}`;
  thanh tab `Panel/Tabs/Tab_{Shop,Journey,Home,Collection,Settings}/{Icon,Label}`; Profile `Dim/Window/{Header,NameRow,AvatarGrid/…/Items/Avatar_01…20,SaveButton,CloseButton}`.
- Kiểm: chụp 5 trạng thái (Home, Shop đầu/cuối, Settings, Profile) trước/sau → trùng từng pixel; Edit = Play; thử Play đổi tab, vuốt, bật/tắt âm, mua gói, chọn avatar + Save.
- **Bẫy đã dính:** gỡ thanh cuộn khỏi ScrollRect (chế độ AutoHideAndExpandViewport) thì ScrollRect thôi điều khiển `Viewport`; override cũ trong scene đặt Viewport về 0 → mặt nạ cắt mất lưới avatar. Đã bỏ override (prefab lưu đúng kích thước). Gỡ thanh cuộn ở prefab khác nhớ kiểm Viewport.
- Prefab không còn giống cây gốc: muốn tách lại từ bản gốc bằng `ui_extract.py` thì ra prefab mới, không ghi đè.

### Dọn prefab popup Gameplay (2026-10-20)
Cùng công cụ `UiTidy` (thêm `ShotGame`: chụp từng popup qua UI Camera trên nền đen, màn Thắng 2 trạng thái logo / bảng):

| Prefab | Trước → sau (object) | Sâu nhất | Đã bỏ |
|---|---|---|---|
| SuccessCanvas | 220 → 83 | 7 → 6 | `WinPanelLiveOpsManagement` (Sky Race), thanh xu ẩn, Well Done / New Feature / Next Feature, 3 ConfettiBurst thừa, `Confeti` + mưa pháo cũ, AreaShine, Trail/Left/Right tắt |
| OutOfCapacityPopup (đã gộp `OutOfCapacityPanel`) | 100 → 21 | 11 → 7 | khối "+20 CAPACITY", `Extra Rewards` (55 object hạt sáng/ô thưởng IAP), mũi tên + tim "-1" tắt, cụm nút quảng cáo `AB_EGP` |
| FailCanvas | 58 → 25 | 8 → 6 | xu / giải thưởng tắt, mặt buồn, thanh 5 tim, "YOU WILL RETURN", giá tiền + mũi tên tắt; làm phẳng `Inner Content` |
| SettingsCanvas | 84 → 44 | 10 → 7 | header tắt, khối "+20 Capacity / FREE", nút debugger + ô mật khẩu, nhãn "SOUND" tắt; làm phẳng `End Game - Play On Panel` |
| BoosterBuyPopup (đã gộp `BoosterBuyPanel`) | 44 → 21 | 8 → 7 | nút quảng cáo `RV`, nút "CARD SPECIAL OFFERS", nền tiền tệ / giảm giá tắt; bỏ Canvas lồng |

- **Gộp prefab lồng**: OutOfCapacity và BoosterBuy trước là 2 prefab (vỏ chỉ có Canvas + script, ruột ở `…Panel`) → đã unpack ruột vào vỏ, mỗi popup còn 1 prefab, con tên `Panel`. `OutOfCapacityPanel.prefab`, `BoosterBuyPanel.prefab` (và `StreakRewardBar.prefab`) không còn ai dùng – xoá tay được.
- Giữ: khung / bục theo độ khó (code bật theo độ khó), nút Support đang hiện ở Settings, hạt lấp lánh sau icon booster, các SubEmitter pháo hoa (ParticleSystem tham chiếu), chữ logo BOUNCE BLAST giữ tên (nằm trên đường dẫn hoạt ảnh của Animator `Logo`).
- Tên mới: Success `Stage/{Card/{Background,Content/{Podium_*,Coin/{CoinPile,SparkleLayer/Star_01…10},Prize/Amount},Level,Perfect/Title,ContinueButton,X2Button},LogoArea/{ConfettiBursts/Burst_1-2,Fireworks,Logo}}, ConfettiRain`;
  Fail `Panel/{Dim,Content/{Frame/{Frame_Normal|Hard|SuperHard,Level,Header},TryAgain/TryAgainButton,CloseButton}}`;
  Settings `Dim/Window/Content/{Frame,CloseButton,Title,Rows/{Support,Sound,Music,Vibration,Bottom/{RestartButton,HomeButton}}}`;
  OutOfCapacity `Panel/{Dim,Content/{Title,Body/{PlayOn/{PlayOnButton,Price},GiveUpButton}}}`; BoosterBuy `Panel/{Dim,Window/{CloseButton,Header/Title,Body/{Icon,Description},Buy/Price/BuyButton}}`.
- Kiểm: 6 ảnh trước/sau trùng từng pixel; mọi View `Validate` đủ; Play mở từng popup qua GameUI (độ khó Hard) + bấm Try Again / Add Capacity / mua / Close + chạy màn Thắng; Edit = Play (0 object mới).
- Bẫy của công cụ chụp: đừng tìm object theo tên trong `ShotGame` (tên đổi khi dọn) – lấy theo ô tham chiếu của View.

### Thư mục script UI (2026-10-20)
`Scripts/UI/` chia theo nơi dùng (namespace vẫn là `BounceBlast.UI`, GUID script giữ nguyên nên scene/prefab không đổi):
`Common/` (MirroredImage, UiButtonFeedback, SafeAreaFitter, UiSkin) · `Effects/` (UiConfetti, UiSparkles, UiShineSweep, UiSpin) ·
`Gameplay/` (GameUI, HudView, OverlayView, BoosterBarView, CapacityBarView) · `Gameplay/Popups/` (PopupViewBase + 5 popup) ·
`Menu/` (HomeView, BottomBarView, MenuPager, MenuSettingsView, ToastView, ProfilePopupView, StreakBarView) · `Menu/Shop/` (ShopPanelView, ShopOfferItem).
Thêm script UI mới: đặt vào thư mục theo scene/màn dùng nó; dùng ở cả hai scene thì vào `Common/`.

### EventSystem ở gốc scene (2026-10-20)
Cả MenuGame và Gameplay: `EventSystem` (EventSystem + StandaloneInputModule) chuyển từ `UI/EventSystem` ra gốc scene, ngay sau `UI`. Lý do: nó là hệ thống input của cả scene, không phải màn UI; nằm trong `UI` thì ai tắt cả nhóm UI là mất hết bấm/chạm (kể cả popup bật lại sau). Không object nào tham chiếu tới nó; `GameUI` tìm bằng `FindObjectOfType`. Đã kiểm bằng raycast thật qua EventSystem: nút Settings HUD, booster, Close popup (Gameplay); tab dưới đáy, trượt trang (MenuGame).

### Quy tắc: hiệu ứng / trạng thái tính TỪ giá trị prefab, không đặt cứng (2026-10-20)
User chỉnh scale icon tab trong prefab, bấm Play thì bị trả về – vì code ghi `localScale = Vector3.one * …`, `anchoredPosition = (0, y)`.
**Quy tắc:** code chỉ được nhân / cộng phần hiệu ứng lên **giá trị đọc từ prefab** (ghi lại ở `Awake` hoặc lần đầu dùng), không bao giờ đặt về hằng số (1, 0…).
Đã sửa theo quy tắc này:

| Script | Trước | Giờ |
|---|---|---|
| `BottomBarView` | icon = 1 / 1.2, Y = 0 / 68 | icon = cỡ prefab × `selectedIconScale`, Y prefab + `selectedIconLift` (tab đang chọn trong prefab được quy ngược về) |
| `ProfilePopupView` | ô chọn = 1.1, ô khác = 1 | cỡ prefab × `selectedScale` |
| `BoosterBarView` | nút đang dùng = 1.12; mũi tên = (0, y) | cỡ prefab × 1.12; mũi tên = vị trí prefab + nhấp nhô |
| `PopupViewBase` (mọi popup) | nảy 0 → 1 | nảy 0 → cỡ prefab của `content` |
| `SuccessPopupView` | bảng, PERFECT! nảy về 1 | nảy về cỡ prefab (chữ logo nay do clip gốc lo) |
| `MenuPager` | X trang = 0 khi đứng yên | X prefab + độ lệch khi vuốt |
| `UiSpin` | bật lại = góc 0 | bật lại = góc prefab |
| `UiShineSweep` | dải Y = 0 | dải giữ Y prefab |
| `HudView` | badge / chữ đặt theo ô kiểu dáng kể cả Normal | Normal = đúng như prefab; Hard / Super Hard theo ô kiểu dáng trong Inspector |

- **Bẫy thứ hai – override trong scene:** chỉnh trong Prefab Mode nhưng scene còn override cũ cho cùng thuộc tính (VD icon Journey scale 0.864 trong scene vs 0.612 trong prefab) thì Play dùng giá trị scene. Kiểm: chọn object trong scene, thuộc tính in đậm / vạch xanh bên trái = override → chuột phải ▸ Revert. Đã bỏ override đó.

### Hiệu ứng chuyển trang (MenuGame – 2026-10-20)
- `UiAppear` (Scripts/UI/Effects) gắn trên từng thành phần: mờ → rõ, nảy từ `fromScale` lên đúng cỡ (`overshoot` = nảy quá đà), trượt từ `fromOffset` về đúng chỗ, trong `duration` giây. `followDirection` = đổi chiều X của `fromOffset` theo hướng chuyển trang (sang trang bên phải → vào từ phải, sang trái → vào từ trái). Tính từ giá trị prefab (đứng yên = đúng như prefab). Cần CanvasGroup (tự thêm khi gắn).
- `PageTransition` gắn trên gốc trang, giữ danh sách `UiAppear` theo thứ tự xuất hiện + `stagger` (giây giữa hai thành phần); chuột phải ▸ "Lấy các UiAppear con" để điền lại; bỏ tick `playOnEnter` để tắt hiệu ứng trang đó.
- `MenuPager`: trang vừa lộ ra (kéo hoặc bấm tab) → `Prepare(hướng)` (thành phần ẩn); tới nơi → `Play(hướng)`. Mở Menu lần đầu cũng chạy cho Home. Kéo dở rồi trả về → thành phần về đúng prefab (OnDisable).
- Đang đặt:
  - **Shop – trượt ngang**: 2 nhãn + 13 thẻ gói (`Card`) trượt 500 đơn vị theo hướng chuyển trang, không nảy, 0.32 s, cách nhau 0.05 s. Thanh tiêu đề Shop đứng yên.
  - **Home – popup bé → to**: avatar, xu, tim, thanh Streak, nút Play bật từ 20% lên đúng cỡ có nảy, 0.4 s, cách nhau 0.05 s.
  - **Settings**: (bản thử đầu) thanh tiêu đề trượt xuống, khung công tắc nảy lên.
- Không gắn `UiAppear` lên con trực tiếp của LayoutGroup (layout sẽ đè vị trí) – gắn lên con bên trong (như `Card` của gói Shop).

### Sửa lỗi avatar mất sau khi chuyển trang + vệt sáng thẻ Shop (2026-10-20)
- **Lỗi:** quay lại Home thì avatar biến mất. Nguyên nhân: `UiButtonFeedback` (lún / nảy khi bấm) ghi lại scale gốc MỖI LẦN object bật và đặt scale MỖI FRAME. Trang vừa lộ ra thì `UiAppear` đang thu avatar xuống 20% → `UiButtonFeedback` ghi nhầm 20% làm gốc và ép mãi. (Avatar là chỗ duy nhất hai script nằm trên cùng object.)
- **Sửa:** `UiButtonFeedback` ghi cỡ gốc một lần ở Awake (lấy `UiAppear.BaseScale` nếu có) và chỉ đặt scale khi đang lún / nảy; đứng yên không đụng tới → không tranh với `UiAppear` hay code trạng thái khác (VD nút booster phóng ×1.12 khi đang dùng).
- **Bài học:** hai script cùng chỉnh một thuộc tính trên cùng object phải phối hợp (một bên chỉ ghi khi đang hoạt động, cỡ gốc lấy từ prefab), không được ghi mỗi frame.
- **Vệt sáng thẻ Shop:** `UiShineSweep` (thêm tuỳ chọn `playOnEnable`) gắn trên `Card/Background` của 13 gói, lớp `ShineMask/ShineBand` đặt sẵn dưới nền thẻ (trên nền, dưới chữ / nút). Loé lần lượt như làn sóng: trễ 0.6 s + 0.15 s mỗi thẻ, quét 0.7 s, 3.2 s một vòng; dải sáng `fx_glow` màu kem alpha 0.85. Chỉnh màu / độ rộng / góc trên `ShineBand`, nhịp trên `UiShineSweep`.

### Hover cho nút Shop (2026-10-20)
13 nút (12 `BuyButton` + `FreeCoins/ClaimButton`) có 3 lớp, đều đặt sẵn trên nút trong prefab:
- `UiButtonFeedback` (hoverScale 1.08, pressScale 0.92): phóng khi rê chuột, lún / nảy khi bấm – như các nút khác trong game.
- `UiHoverHighlight` (Scripts/UI/Effects, mới): rê chuột vào → nhấc nút lên `lift` (8) đơn vị + gọi vệt sáng lướt một lượt; rời chuột → hạ về vị trí prefab. Đứng yên không đụng vị trí.
- `UiShineSweep` + `ShineMask/ShineBand` riêng của nút (`playOnEnable` tắt; `SweepOnce()` mới = quét đúng một lượt, 0.45 s).
Hover chỉ có khi dùng chuột (Editor / PC); trên điện thoại còn lún / nảy khi chạm.

## Logo thắng BOUNCE BLAST dùng hoạt ảnh gốc

Video `win.mp4` cho thấy chữ BLAST nhảy vọt lên to rồi đập xuống. Game gốc đã có sẵn hoạt ảnh này: `Art/UI/Win/Animation/Banner Animation.anim` (1.75 s, `Banner.controller`) trên Animator của `Stage/LogoArea/Logo`, trước đây bị tắt và thay bằng code tự nảy chữ.
- Nay `SuccessPopupView.AnimateLogo` chỉ bật Animator, `Play("Banner Animation", 0, 0)` rồi chờ hết clip. Code không còn tự đặt cỡ chữ.
- Diễn biến của clip: b → e bật lần lượt (0 → 0.93 s), BLAST bật lên to 1.5 lần ở Y = 0 lúc 1.07 s, đập về Y = −94 lúc 1.23 s, sau đó BLAST nảy 1.2 (1.37 s).
- Đáy tròn (badge) khi BLAST đập: **đã chỉnh trong clip** cho khớp video (frame 447–456). Clip gốc chỉ nảy 1.2 nên nhìn như rung nhẹ. Nay badge phồng 1.5 (1.37 s), co 0.92 (1.45 s), nảy 1.05 (1.55 s), về 1 (1.65 s). Chỉ đường cong `Animation/badge` scale x/y đổi; mọi keyframe khác giữ nguyên bản gốc.
- Tiếng phát theo mốc thời gian của clip, chỉnh trong Inspector: `letterSoundTimes` (tiếng bật chữ) và `blastSlamTime` (tiếng pháo khi đập).
- Animator chạy `UnscaledTime` nên vẫn chạy khi `timeScale = 0`.
- **Tốc độ**: Speed của state `Banner Animation` trong `Banner.controller` = **0.7** (cả đoạn logo dài 2.5 s thực thay vì 1.75 s). Muốn nhanh hay chậm hơn thì chỉ đổi số này. Code đọc vị trí của clip (`normalizedTime`), nên mốc tiếng tự khớp. Riêng pháo nổ và mưa pháo hẹn theo giây thực, phải đổi theo cho khớp (hiện `burstDelay` 1.35 và `rainDelay` 1.4 trên SuccessPopupView, ứng với lúc BLAST bật lên).
- Muốn chỉnh hoạt ảnh thì sửa clip trong cửa sổ Animation, không sửa code.

## Màn loading (mở app + vào màn chơi)

Lấy nguyên từ game gốc: `TSPreloadScene` / `LoadingCanvas Preload Scene Variant`, tách bằng `Tools/UnityYaml/ui_extract.py` thành `Prefabs/UI/LoadingCanvas.prefab`. Ảnh gốc giữ GUID trong `Art/UI/Loading/{Sprites,Textures}`: `Splash` (1920² gồm nền khối màu, logo và Garawell & Voodoo), `LoadingBar_mir` (nền thanh, MirroredImage), `LoadingFullBar (2)` (thanh đầy, Filled Horizontal).

- **Scene `Loading.unity`** đứng đầu Build Settings. Gồm Main Camera (nền đen), `LoadingCanvas` (instance của prefab) và `LoadingController` (`LoadingSceneController`).
- **Luồng**: mở app ▶ Loading ▶ MenuGame. Bấm Play ở Menu → `SceneFlow.GoToGameplay` nhớ đích rồi nạp Loading ▶ Gameplay. Về Menu vẫn nạp thẳng.
- **`LoadingScreenView`** (Scripts/UI/Loading) chỉ đổ `fillAmount` + chữ `N%`, cùng chữ phiên bản (`versionFormat` "v{0}" + Application.version). Chữ build (`b1`) là chữ tĩnh trong prefab.
- **`LoadingSceneController`**: nạp nền (`LoadSceneAsync`). Thanh hiện min(tiến độ thật, thời gian tối thiểu): `bootSeconds` 2 s khi mở app, `levelSeconds` 1 s khi vào màn. Hai số này chỉnh trên object `LoadingController`.
- Hai dòng `UI Text - Trivial` ("DID YOU KNOW…") và `UI Text - Info` ("INITIALIZING VOODOO SDK…") có trong bản gốc nhưng tắt sẵn, vì ảnh game gốc không hiện chúng. Muốn dùng thì bật trong prefab.
- Bấm Play thẳng MenuGame hoặc Gameplay trong Editor vẫn chạy như trước (không qua Loading).
- **Không lộ bàn trống khi vào màn chơi** (video "lỗi loading"): `LevelManager.Start` cố ý đợi 1 khung rồi mới dựng level, nên trước đây có một khung bàn trống, kèm cú khựng lúc dựng level. Nay màn loading được giữ lại khi đổi scene (`DontDestroyOnLoad`) và đợi `SceneFlow.LevelReady`, cờ này do `LevelManager.LoadLevel` báo sau khi dựng xong. Sau đó đợi thêm 2 khung rồi mờ dần trong `fadeSeconds` (0.25 s) và tự huỷ. Nếu không ai báo thì tối đa chờ `readyTimeout` (5 s).
- **Sửa `MirroredImage` (lật dọc)**: sprite `_mir` lật cả ngang lẫn dọc là **góc dưới-trái** của hình (`LoadingBar_mir`, `off_mir`). Bản khôi phục trước đặt nó ở nửa trên, làm nền thanh loading bị bóp nhọn hai đầu. Nay góc gốc nằm dưới, phần lật dọc nằm trên. Đã kiểm tra lại các nút tròn ở Settings (Support / Sound / Music / Vibration), cũng dùng `off_mir`.

## Viền trắng khi tăng tốc (SpeedUpCanvas)

Lấy từ game gốc: `ManagerScene/TapToSpeedUp`, tách bằng `ui_extract.py` thành `Prefabs/UI/SpeedUpCanvas.prefab`. Ảnh `Art/UI/SpeedUp/Textures/Vignette_mir.png` giữ GUID gốc.
- `Canvas Base`: Canvas Overlay có CanvasGroup (alpha 0 lúc thường, không chặn chạm).
- `Vignette Speed`: MirroredImage trắng, alpha 0.28 (đã giảm từ 0.47 của bản gốc cho đỡ gắt), lật ngang + dọc.
- `Label` (thêm mới, cùng font và material chữ MenuToast): dòng "SPEED UP x1.5" cỡ nhỏ (46) ở góc trên-trái bàn chơi (anchor 0.185, 0.89, căn trái), không nháy, nảy nhẹ lúc bắt đầu giữ (`popSeconds`); mẫu chữ `labelFormat`, số lấy từ `holdSpeed`.
- `UI Button + Text + Image`: dòng gốc "HOLD WALL TO SPEED UP!" kèm bàn tay. Bản gốc để tắt, mình cũng để tắt; bật lên nếu muốn làm hướng dẫn.
- `SpeedUpView` làm mờ CanvasGroup 0 ↔ 1 trong `fadeSeconds` (0.15 s). Trong lúc giữ thì **viền nháy** (CanvasGroup của `Vignette Speed`, không nháy chữ): độ đậm dao động `blinkMin` (0.1) ↔ 1, `blinkPerSecond` (1) lần/giây, cùng nhịp viền đỏ cảnh báo capacity; mỗi lần giữ bắt đầu từ lúc sáng nhất. `GameUI.SetSpeedUp(bool)` do `InputManager` gọi khi bắt đầu hoặc thôi nhấn giữ chỗ trống trong khu khay đạn (tốc độ `holdSpeed` = 1.5, chỉ khi bàn đang có bi).
- Instance đặt trong scene Gameplay tại `UI/HUD/SpeedUpCanvas`, cạnh `OverlayCanvas`.
