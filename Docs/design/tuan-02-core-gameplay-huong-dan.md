# 📘 HƯỚNG DẪN THỰC HÀNH TUẦN 2: LẬP TRÌNH CORE GAMEPLAY BOUNCE BLAST (TỪ A - Z)

> 🗄️ **Tài liệu lưu trữ (2026-10-02).** Code bên dưới là bản nháp lúc viết hướng dẫn, **đã lệch so với code thật** trong `Assets/_Project/Scripts/` (VD: class thật tên `GameplayManager`; hộp đạn đứng cố định + mở khóa dấu `+`, không trượt lên). Luật chơi hiện hành xem `README.md` mục 2.

> **Mục tiêu Tuần 2 dành cho Thực tập sinh:**  
> Tự tay xây dựng trọn vẹn vòng lặp Gameplay cốt lõi (**Core Loop**) của **Bounce Blast** theo đúng chuẩn kiến trúc công nghiệp từ bộ kỹ năng `masterAI` (`unity-core-architecture`, `unity-puzzle-core-mvc`, `unity-ugui-virtual-navigation`):
> 1. **Click chọn Hộp Đạn (`Ammunation`)** ở hàng đầu tiên dưới chân bàn chơi.
> 2. **Đẩy hàng đợi theo cột**: Các hộp đạn phía sau tự động trượt tiến lên 1 nấc khi hộp phía trước bắn xong.
> 3. **Xả bi liên thanh (`Ball.prefab`) bằng Object Pool (Zero-GC)** từ ống phóng trên đỉnh bàn chơi.
> 4. **Vật lý nảy 2D & Phá gạch cùng màu (`Cube`)**: Bi chạm gạch cùng màu thì trừ máu gạch; chạm gạch khác màu thì nảy bật ra; rơi xuống đáy thì tuần hoàn lại lên ống phóng (`Conveyor Loop`).
> 5. **Kiểm tra Thắng / Qua Màn (`Win State`)**: Phá hết 100% gạch trên bàn sẽ báo thắng và tự chuyển sang Level tiếp theo.

---

## 🗺️ PHẦN 1: HIỂU RÕ LUỒNG HOẠT ĐỘNG TRƯỚC KHI CODE

Trước khi viết dòng code đầu tiên, bạn hãy nhìn sơ đồ dưới đây để nắm rõ **4 Script mới** mà bạn sẽ tự tạo trong tuần này liên kết với nhau như thế nào:

```mermaid
flowchart TD
    PlayerClick["Người chơi Click chuột / Chạm màn hình"] --> Manager["1. BounceBlastGameplayManager.cs\n(Bộ não điều phối & Object Pool)"]
    Manager -->|Raycast kiểm tra hộp đạn Hàng 0| AmmoBox["2. AmmoBoxUnit.cs\n(Hộp đạn dưới khay: Màu + Số lượng bi)"]
    AmmoBox -->|Báo xả N viên bi màu X| Manager
    AmmoBox -->|Xả xong -> Trượt các hộp dưới lên| AdvanceCol["Đẩy các hộp cùng cột tiến lên 1 hàng"]
    Manager -->|Lấy bi từ Pool & Bắn từ đỉnh bàn| Ball["3. BouncingBall.cs\n(Viên bi nảy vật lý 2D)"]
    Ball -->|Va chạm OnCollisionEnter2D| CheckColor{"Cùng màu với Gạch?"}
    CheckColor -->|Đúng (Cùng màu)| Cube["4. CubeUnit.cs\n(Trừ 1 HP -> Về 0 thì nổ)"]
    CheckColor -->|Sai (Khác màu)| Bounce["Nảy vật lý sang hướng khác"]
    Ball -->|Rơi xuống đáy bàn| Conveyor["Tuần hoàn lại lên đỉnh bàn (Conveyor)"]
```

### ⚠️ Một lưu ý Vật lý 2D cực kỳ quan trọng trong Unity (Bí quyết của Senior):
Trong Tuần 1, để mặt viên gạch `Cube` và chữ số quay ra phía Camera, chúng ta đã xoay cả cục `Cube` góc `(-90, 0, 0)`.  
Tuy nhiên, trong Unity: **`BoxCollider2D` (Khung va chạm 2D) luôn nằm trên mặt phẳng X-Y**. Nếu bạn xoay GameObject gốc đi `-90` độ quanh trục X, cái khung `BoxCollider2D` sẽ bị nằm ngang dẹt mỏng như tờ giấy $\rightarrow$ Viên bi 2D bắn xuống sẽ **bay xuyên qua gạch**!
* **Cách giải quyết chuẩn xác**: Giữ góc xoay của GameObject cha `Cube` là **`(0, 0, 0)`** (để `BoxCollider2D` luôn dựng đứng đón bóng), và **chỉ xoay phần hình ảnh con (`Model`) bên trong nó góc `(-90, 0, 0)`**!

---

## 🛠️ PHẦN 2: CÁC BƯỚC THỰC HÀNH CODE CHI TIẾT

### BƯỚC 1: Tạo Script `CubeUnit.cs` (Quản lý Máu & Màu của từng viên Gạch)

#### 1.1. Thao tác trên Unity Editor:
1. Nhìn vào cửa sổ **Project** (góc dưới bên trái Unity), bấm vào thư mục **`Assets/Scripts`**.
2. Click **Chuột phải** vào khoảng trống trong thư mục `Assets/Scripts` $\rightarrow$ chọn **`Create` $\rightarrow$ `C# Script`**.
3. Đặt tên file chính xác là: **`CubeUnit`** (chú ý viết hoa chữ `C` và `U`, không có dấu cách) rồi nhấn `Enter`.
4. Nháy đúp (Double-click) vào file `CubeUnit.cs` để mở bằng VS Code / Visual Studio.

#### 1.2. Thao tác Code:
Xóa toàn bộ code mặc định bên trong `CubeUnit.cs` và gõ (hoặc dán) đoạn code sau vào, sau đó bấm **`Ctrl + S`** để lưu:

```csharp
using TMPro;
using UnityEngine;

/// <summary>
/// Gắn lên từng viên gạch (Cube) trên bàn chơi.
/// Quản lý: Mã màu (colorCode), Máu hiện tại (hp), và Hiển thị số máu (TextMeshPro).
/// </summary>
public class CubeUnit : MonoBehaviour
{
    [Header("Dữ liệu Gạch (Model)")]
    public string colorCode;
    public int hp = 1;

    private TextMeshPro _hpText;
    private Vector3 _initialScale;

    /// <summary>
    /// Hàm khởi tạo được gọi ngay khi Spawner sinh ra viên gạch
    /// </summary>
    public void Setup(string code, int initialHp, TextMeshPro tmp)
    {
        colorCode = code;
        hp = Mathf.Max(1, initialHp);
        _hpText = tmp;
        _initialScale = transform.localScale;

        // Đảm bảo Collider 2D luôn là vật cản rắn để bóng nảy
        BoxCollider2D col2D = GetComponent<BoxCollider2D>();
        if (col2D != null)
        {
            col2D.isTrigger = false;
        }

        UpdateVisualText();
    }

    /// <summary>
    /// Khi bị viên bi CÙNG MÀU đập trúng -> Trừ 1 máu
    /// </summary>
    public void TakeDamage(int damage = 1)
    {
        if (hp <= 0) return;

        hp -= damage;

        if (hp <= 0)
        {
            // Báo cho GameplayManager biết 1 viên gạch vừa bị phá hủy
            if (BounceBlastGameplayManager.Instance != null)
            {
                BounceBlastGameplayManager.Instance.OnCubeDestroyed(this);
            }

            // Ẩn viên gạch đi (Zero-GC: tắt GameObject thay vì Destroy liên tục)
            gameObject.SetActive(false);
        }
        else
        {
            UpdateVisualText();
        }
    }

    private void UpdateVisualText()
    {
        if (_hpText == null) return;

        if (hp > 1)
        {
            _hpText.gameObject.SetActive(true);
            // Quy tắc Zero-GC (unity-core-architecture): Dùng SetText thay vì hp.ToString()
            _hpText.SetText("{0}", hp);
        }
        else
        {
            _hpText.gameObject.SetActive(false);
        }
    }
}
```

---

### BƯỚC 2: Tạo Script `AmmoBoxUnit.cs` (Quản lý Hộp Đạn Dưới Khay)

#### 2.1. Thao tác trên Unity Editor:
1. Trong thư mục **`Assets/Scripts`**, click **Chuột phải** $\rightarrow$ **`Create` $\rightarrow$ `C# Script`**.
2. Đặt tên file chính xác là: **`AmmoBoxUnit`** rồi nhấn `Enter`.
3. Mở file `AmmoBoxUnit.cs` lên.

#### 2.2. Thao tác Code:
Dán nội dung sau vào `AmmoBoxUnit.cs` và bấm **`Ctrl + S`** để lưu:

```csharp
using TMPro;
using UnityEngine;

/// <summary>
/// Gắn lên từng Hộp Đạn (Ammunation / Jelly Box) ở lưới phía dưới bàn chơi.
/// Cơ chế chuẩn Bounce Blast (KPGridManager & Ammunation):
/// - Các hộp đạn đứng CỐ ĐỊNH tại vị trí (rowIndex, colIndex), không trượt đổi chỗ.
/// - Khi bắt đầu màn chơi: Hàng đầu tiên (rowIndex == 0) luôn Active (sáng rõ, hiện số, bấm được); các hàng sau (rowIndex > 0) bị ẩn số/làm tối (isActive = false).
/// - Khi click vào 1 hộp đạn đang Active -> Mở khóa (Active) 4 hộp đạn lân cận theo hình dấu cộng '+' (Trên, Dưới, Trái, Phải).
/// </summary>
public class AmmoBoxUnit : MonoBehaviour
{
    [Header("Vị trí Cố định trong Lưới (Grid Coord)")]
    public int rowIndex;
    public int colIndex;

    [Header("Trạng thái & Thông số Đạn")]
    public bool isActive;
    public bool isDischarging;
    public string colorCode;
    public int ballCount;
    public Color boxColor;

    private TextMeshPro _countText;
    private Vector3 _baseScale;
    private Vector3 _baseLocalPos;

    public void Setup(int row, int col, string code, int count, Color color, TextMeshPro tmp)
    {
        rowIndex = row;
        colIndex = col;
        colorCode = code;
        ballCount = count;
        boxColor = color;
        _countText = tmp;
        isDischarging = false;

        _baseScale = transform.localScale;
        _baseLocalPos = transform.localPosition;

        BoxCollider2D col2D = GetComponent<BoxCollider2D>();
        if (col2D == null)
        {
            col2D = gameObject.AddComponent<BoxCollider2D>();
        }
        col2D.size = new Vector2(1.8f, 1.8f);
        col2D.isTrigger = true;

        UpdateCountText();
        SetActiveState(row == 0);
    }

    public void SetActiveState(bool active)
    {
        isActive = active;

        if (isActive)
        {
            transform.localScale = _baseScale;
            transform.localPosition = _baseLocalPos;
            ApplyVisualColor(boxColor);
            if (_countText != null)
            {
                _countText.gameObject.SetActive(true);
                _countText.alpha = 1.0f;
            }
        }
        else
        {
            transform.localScale = _baseScale * 0.90f;
            transform.localPosition = new Vector3(_baseLocalPos.x, _baseLocalPos.y, _baseLocalPos.z + 0.15f);
            Color dimmedColor = Color.Lerp(boxColor, new Color(0.10f, 0.12f, 0.20f, 1f), 0.68f);
            ApplyVisualColor(dimmedColor);
            if (_countText != null)
            {
                _countText.gameObject.SetActive(false);
            }
        }
    }

    private void ApplyVisualColor(Color c)
    {
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        Color hColor = Color.Lerp(c, Color.white, 0.25f);
        Color sColor = Color.Lerp(c, Color.black, 0.35f);

        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            if (!r.gameObject.activeInHierarchy) continue;
            if (r is ParticleSystemRenderer || r.GetComponent<TextMeshPro>() != null) continue;
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", c);
            mpb.SetColor("_Color", c);
            mpb.SetColor("_HColor", hColor);
            mpb.SetColor("_SColor", sColor);
            r.SetPropertyBlock(mpb);
        }
    }

    public bool CanBeClicked()
    {
        return isActive && !isDischarging && ballCount > 0 && gameObject.activeInHierarchy;
    }

    public void UpdateCountText()
    {
        if (_countText != null)
        {
            _countText.gameObject.SetActive(true);
            _countText.SetText("{0}", ballCount);
        }
    }
}
```

---

### BƯỚC 3: Tạo Script `BouncingBall.cs` (Viên Bi Nảy Vật Lý 2D)

#### 3.1. Thao tác trên Unity Editor:
1. Trong thư mục **`Assets/Scripts`**, click **Chuột phải** $\rightarrow$ **`Create` $\rightarrow$ `C# Script`**.
2. Đặt tên file chính xác là: **`BouncingBall`** rồi nhấn `Enter`.
3. Mở file `BouncingBall.cs` lên.

#### 3.2. Thao tác Code:
Dán nội dung sau vào `BouncingBall.cs` và bấm **`Ctrl + S`**:

```csharp
using System;
using UnityEngine;

/// <summary>
/// Điều khiển viên bi (Ball.prefab) bay và nảy bằng Vật lý 2D.
/// - Chạm Gạch CÙNG MÀU -> Trừ máu gạch & Thu hồi bi về Pool.
/// - Chạm Gạch KHÁC MÀU hoặc Tường -> Nảy bật ra tiếp tục tìm mục tiêu.
/// - Rơi xuống đáy bàn -> Tự động bay vòng lên đỉnh bàn (Conveyor) nếu trên bàn vẫn còn gạch cùng màu.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class BouncingBall : MonoBehaviour
{
    public string colorCode;
    public Color ballColor;

    [Header("Cấu hình Vật lý Bi")]
    public float targetSpeed = 14f;
    public float bottomRecycleY = -9.8f;

    private Rigidbody2D _rb;
    private CircleCollider2D _col;
    private bool _isActive;
    private float _stuckTimer;

    private static PhysicsMaterial2D _bouncyMaterial;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<CircleCollider2D>();

        // Tạo PhysicsMaterial2D có độ nảy 100% (Bounciness = 1, Friction = 0) để bi nảy cực sung
        if (_bouncyMaterial == null)
        {
            _bouncyMaterial = new PhysicsMaterial2D("BallPerfectBounce")
            {
                bounciness = 1.0f,
                friction = 0.0f
            };
        }

        _rb.gravityScale = 0.35f; // Trọng lực nhẹ kéo bi rơi dần xuống dưới bàn
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Chống xuyên gạch khi bay nhanh
        _rb.sharedMaterial = _bouncyMaterial;
        _col.sharedMaterial = _bouncyMaterial;
        _col.radius = 0.24f;
    }

    /// <summary>
    /// Kích hoạt viên bi từ Object Pool và phóng đi
    /// </summary>
    public void Launch(string code, Color color, Vector3 spawnWorldPos, Vector2 launchDirection)
    {
        colorCode = code;
        ballColor = color;
        _isActive = true;
        _stuckTimer = 0f;

        transform.position = new Vector3(spawnWorldPos.x, spawnWorldPos.y, -0.65f);
        gameObject.SetActive(true);

        // Tô màu cho viên bi và vệt đuôi (Trail)
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        Color hColor = Color.Lerp(color, Color.white, 0.25f);
        Color sColor = Color.Lerp(color, Color.black, 0.35f);

        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", color);
            mpb.SetColor("_Color", color);
            mpb.SetColor("_HColor", hColor);
            mpb.SetColor("_SColor", sColor);
            r.SetPropertyBlock(mpb);
        }

        _rb.velocity = launchDirection.normalized * targetSpeed;
    }

    private void FixedUpdate()
    {
        if (!_isActive) return;

        // 1. Giữ tốc độ bi luôn ổn định để cảm giác bắn nhanh, dứt khoát
        Vector2 vel = _rb.velocity;
        float speed = vel.magnitude;
        if (speed < targetSpeed * 0.75f)
        {
            // Nếu bi bị kẹt hoặc bay quá chậm, đẩy nhẹ xuống dưới
            Vector2 dir = speed > 0.1f ? vel.normalized : new Vector2(UnityEngine.Random.Range(-0.5f, 0.5f), -1f).normalized;
            if (Mathf.Abs(dir.y) < 0.2f) dir.y = -0.4f;
            _rb.velocity = dir.normalized * targetSpeed;
        }
        else if (speed > targetSpeed * 1.35f)
        {
            _rb.velocity = vel.normalized * targetSpeed;
        }

        // 2. Chống kẹt ngang (bi nảy qua lại mãi theo phương ngang)
        if (Mathf.Abs(_rb.velocity.y) < 0.8f)
        {
            _stuckTimer += Time.fixedDeltaTime;
            if (_stuckTimer > 0.6f)
            {
                _rb.velocity = new Vector2(_rb.velocity.x, -4.5f).normalized * targetSpeed;
                _stuckTimer = 0f;
            }
        }
        else
        {
            _stuckTimer = 0f;
        }

        // 3. Kiểm tra nếu bi rơi xuống đáy bàn chơi (hoặc bay ra ngoài biên)
        Vector3 localPos = transform.parent != null ? transform.parent.InverseTransformPoint(transform.position) : transform.position;
        if (localPos.y < bottomRecycleY || localPos.y > 13.5f || localPos.x < -2f || localPos.x > 13f)
        {
            if (BounceBlastGameplayManager.Instance != null)
            {
                BounceBlastGameplayManager.Instance.OnBallReachedBottom(this);
            }
            else
            {
                DeactivateToPool();
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_isActive) return;

        CubeUnit cube = collision.collider.GetComponent<CubeUnit>();
        if (cube != null && cube.gameObject.activeInHierarchy)
        {
            // Kiểm tra CÙNG MÀU (không phân biệt chữ hoa/thường)
            if (string.Equals(cube.colorCode, colorCode, StringComparison.OrdinalIgnoreCase))
            {
                cube.TakeDamage(1);
                if (BounceBlastGameplayManager.Instance != null)
                {
                    BounceBlastGameplayManager.Instance.RecycleBall(this);
                }
                else
                {
                    DeactivateToPool();
                }
            }
        }
    }

    public void DeactivateToPool()
    {
        _isActive = false;
        if (_rb != null) _rb.velocity = Vector2.zero;
        gameObject.SetActive(false);
    }
}
```

---

### BƯỚC 4: Tạo Script `BounceBlastGameplayManager.cs` (Bộ Não Điều Phối Toàn Bộ Gameplay)

#### 4.1. Thao tác trên Unity Editor:
1. Trong thư mục **`Assets/Scripts`**, click **Chuột phải** $\rightarrow$ **`Create` $\rightarrow$ `C# Script`**.
2. Đặt tên file chính xác là: **`BounceBlastGameplayManager`** rồi nhấn `Enter`.
3. Mở file `BounceBlastGameplayManager.cs` lên.

#### 4.2. Thao tác Code:
Dán toàn bộ đoạn code sau vào `BounceBlastGameplayManager.cs` và bấm **`Ctrl + S`**:

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Bộ điều phối Gameplay chính của Tuần 2 (Core MVC + Object Pool + State Check):
/// - Bắt sự kiện Click chuột vào Hộp đạn Hàng 0 (có chặn Click xuyên UI).
/// - Quản lý Object Pool tái sử dụng các viên bi (Ball.prefab).
/// - Tự động đẩy các hộp đạn phía dưới tiến lên khi hộp Hàng 0 bắn xong.
/// - Tuần hoàn bi qua ống Conveyor khi bi rơi xuống đáy bàn mà chưa trúng gạch cùng màu.
/// - Kiểm tra Chiến thắng (Win Level) khi phá sạch gạch trên bàn.
/// </summary>
public class BounceBlastGameplayManager : MonoBehaviour
{
    public static BounceBlastGameplayManager Instance { get; private set; }

    [Header("Tham chiếu Hệ thống")]
    public BounceBlastLevelSpawner levelSpawner;
    public GameObject ballPrefab;

    [Header("Thông số Bắn Bi & Hàng Đợi")]
    public float delayBetweenShots = 0.055f;
    public Vector3 ballSpawnLocalPos = new Vector3(5.30f, 6.20f, -0.65f);
    public float ammoTopRowY = -12.2f;
    public float ammoRowSpacingY = 1.25f;

    [Header("Trạng thái Màn chơi (Read Only)")]
    public int remainingCubesCount;
    public int activeBallsCount;
    public bool isLevelWon;

    // Object Pool chứa các viên bi tái sử dụng (Zero-GC)
    private readonly Queue<BouncingBall> _ballPool = new Queue<BouncingBall>();
    private readonly List<CubeUnit> _activeCubes = new List<CubeUnit>();
    private readonly List<AmmoBoxUnit> _activeAmmoBoxes = new List<AmmoBoxUnit>();
    private Transform _ballsContainer;

    private void Awake()
    {
        Instance = this;
        Application.targetFrameRate = 60; // Chuẩn 60 FPS từ unity-puzzle-build-optimization
    }

    private void Start()
    {
        if (levelSpawner == null) levelSpawner = GetComponent<BounceBlastLevelSpawner>();
        if (ballPrefab == null) ballPrefab = Resources.Load<GameObject>("prefabs/Ball");
    }

    /// <summary>
    /// Được gọi từ BounceBlastLevelSpawner ngay sau khi sinh xong bàn cờ
    /// </summary>
    public void RegisterSpawnedLevel(
        List<CubeUnit> cubes,
        List<AmmoBoxUnit> ammoBoxes,
        float topY,
        float spacingY,
        Transform spawnedContainer)
    {
        StopAllCoroutines();

        // Thu hồi toàn bộ bi đang bay (nếu có)
        if (_ballsContainer != null)
        {
            foreach (BouncingBall b in _ballsContainer.GetComponentsInChildren<BouncingBall>(true))
            {
                b.DeactivateToPool();
            }
        }

        _ballPool.Clear();
        _activeCubes.Clear();
        _activeCubes.AddRange(cubes);

        _activeAmmoBoxes.Clear();
        _activeAmmoBoxes.AddRange(ammoBoxes);

        ammoTopRowY = topY;
        ammoRowSpacingY = spacingY;

        remainingCubesCount = _activeCubes.Count;
        activeBallsCount = 0;
        isLevelWon = false;

        // Tạo thư mục chứa Pool Bi bên trong _SpawnedLevelObjects
        GameObject poolObj = new GameObject("_BallPoolContainer");
        _ballsContainer = poolObj.transform;
        _ballsContainer.SetParent(spawnedContainer, false);

        // Đảm bảo có khung tường vật lý 2D bao quanh bàn cờ để bi không bao giờ lọt ra ngoài
        CreateSafetyBoardWalls(spawnedContainer);
    }

    private void Update()
    {
        if (isLevelWon) return;

        // Kiểm tra Click chuột trái (PC) hoặc Chạm tay (Mobile)
        if (Input.GetMouseButtonDown(0))
        {
            // Quy tắc bắt buộc từ unity-ugui-virtual-navigation: Chặn click xuyên qua giao diện UI
            if (IsPointerOverUI()) return;

            TrySelectAmmoBoxAtPointer(Input.mousePosition);
        }
    }

    private static bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            return true;
        return EventSystem.current.IsPointerOverGameObject();
    }

    private void TrySelectAmmoBoxAtPointer(Vector3 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 worldPoint = cam.ScreenToWorldPoint(screenPos);
        Vector2 point2D = new Vector2(worldPoint.x, worldPoint.y);

        // Kiểm tra tất cả Collider2D tại điểm vừa click
        Collider2D[] hits = Physics2D.OverlapPointAll(point2D);
        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;
            AmmoBoxUnit ammo = hit.GetComponent<AmmoBoxUnit>();
            if (ammo != null && ammo.CanBeClicked())
            {
                StartCoroutine(DischargeAmmoBoxRoutine(ammo));
                return;
            }
        }
    }

    /// <summary>
    /// Xả lần lượt từng viên bi từ Hộp đạn được chọn, và mở khóa (Active) 4 ô đạn lân cận theo hình dấu cộng '+'
    /// </summary>
    private IEnumerator DischargeAmmoBoxRoutine(AmmoBoxUnit ammo)
    {
        ammo.isDischarging = true;
        int shotsToFire = ammo.ballCount;
        string code = ammo.colorCode;
        Color color = ammo.boxColor;
        int r = ammo.rowIndex;
        int c = ammo.colIndex;

        // Mở khóa ngay 4 ô đạn xung quanh theo hình dấu '+' (Trên, Dưới, Trái, Phải)
        UnlockCrossNeighbors(r, c);

        // Hiệu ứng nhún nhẹ khi được bấm chọn
        ammo.transform.localScale *= 1.12f;

        for (int i = 0; i < shotsToFire; i++)
        {
            ammo.ballCount--;
            ammo.UpdateCountText();

            SpawnAndLaunchSingleBall(code, color);
            yield return new WaitForSeconds(delayBetweenShots);
        }

        // Hộp đạn đã xả hết -> Biến mất tại chỗ (các ô khác giữ nguyên vị trí!)
        ammo.gameObject.SetActive(false);
    }

    private void UnlockCrossNeighbors(int row, int col)
    {
        for (int i = 0; i < _activeAmmoBoxes.Count; i++)
        {
            AmmoBoxUnit candidate = _activeAmmoBoxes[i];
            if (candidate == null || !candidate.gameObject.activeInHierarchy || candidate.isActive)
                continue;

            int dr = Mathf.Abs(candidate.rowIndex - row);
            int dc = Mathf.Abs(candidate.colIndex - col);

            // Điều kiện hình dấu cộng (+): Cách đúng 1 ô theo chiều dọc HOẶC chiều ngang (dr + dc == 1)
            if (dr + dc == 1)
            {
                candidate.SetActiveState(true);
            }
        }
    }

    private void SpawnAndLaunchSingleBall(string colorCode, Color color)
    {
        BouncingBall ball = GetBallFromPool();
        if (ball == null) return;

        Transform root = levelSpawner != null && levelSpawner.boardRoot != null ? levelSpawner.boardRoot : transform;
        Vector3 spawnWorldPos = root.TransformPoint(ballSpawnLocalPos);

        // Góc bắn ngẫu nhiên tỏa đều sang trái/phải từ miệng ống phóng trên đỉnh bàn
        float angleDeg = UnityEngine.Random.Range(-52f, 52f);
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector2 launchDir = new Vector2(Mathf.Sin(rad), -Mathf.Cos(rad));

        activeBallsCount++;
        ball.Launch(colorCode, color, spawnWorldPos, launchDir);
    }

    private BouncingBall GetBallFromPool()
    {
        while (_ballPool.Count > 0)
        {
            BouncingBall pooled = _ballPool.Dequeue();
            if (pooled != null) return pooled;
        }

        if (ballPrefab == null) ballPrefab = Resources.Load<GameObject>("prefabs/Ball");
        if (ballPrefab == null) return null;

        GameObject ballObj = Instantiate(ballPrefab, _ballsContainer);
        ballObj.transform.localScale = Vector3.one * 0.85f;
        BouncingBall ballComp = ballObj.GetComponent<BouncingBall>();
        if (ballComp == null) ballComp = ballObj.AddComponent<BouncingBall>();
        return ballComp;
    }

    /// <summary>
    /// Khi viên bi rơi xuống đáy bàn mà chưa chạm được gạch cùng màu:
    /// Nếu trên bàn VẪN CÒN gạch cùng màu -> Tuần hoàn viên bi lên lại ống phóng ở đỉnh bàn (Conveyor Loop)!
    /// Nếu trên bàn ĐÃ HẾT gạch màu đó -> Thu hồi bi về Pool.
    /// </summary>
    public void OnBallReachedBottom(BouncingBall ball)
    {
        if (HasRemainingCubeOfColor(ball.colorCode))
        {
            Transform root = levelSpawner != null && levelSpawner.boardRoot != null ? levelSpawner.boardRoot : transform;
            Vector3 spawnWorldPos = root.TransformPoint(ballSpawnLocalPos);
            float angleDeg = UnityEngine.Random.Range(-48f, 48f);
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector2 launchDir = new Vector2(Mathf.Sin(rad), -Mathf.Cos(rad));

            ball.Launch(ball.colorCode, ball.ballColor, spawnWorldPos, launchDir);
        }
        else
        {
            RecycleBall(ball);
        }
    }

    private bool HasRemainingCubeOfColor(string code)
    {
        for (int i = 0; i < _activeCubes.Count; i++)
        {
            CubeUnit c = _activeCubes[i];
            if (c != null && c.gameObject.activeInHierarchy && c.hp > 0 &&
                string.Equals(c.colorCode, code, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    public void RecycleBall(BouncingBall ball)
    {
        if (ball == null) return;
        ball.DeactivateToPool();
        _ballPool.Enqueue(ball);
        activeBallsCount = Mathf.Max(0, activeBallsCount - 1);
    }

    public void OnCubeDestroyed(CubeUnit cube)
    {
        remainingCubesCount = Mathf.Max(0, remainingCubesCount - 1);

        if (remainingCubesCount <= 0 && !isLevelWon)
        {
            isLevelWon = true;
            Debug.Log($"<color=green>🎉 [CHIẾN THẮNG LEVEL {levelSpawner.levelNumber}] Bạn đã phá sạch toàn bộ gạch trên bàn!</color>");
        }
    }

    /// <summary>
    /// Tạo 3 bức tường vật lý 2D vô hình (Trái, Phải, Đỉnh) ôm sát khung bàn chơi để bi nảy chuẩn xác
    /// </summary>
    private static void CreateSafetyBoardWalls(Transform container)
    {
        GameObject wallsRoot = new GameObject("_PhysicsBoardWalls2D");
        wallsRoot.transform.SetParent(container, false);

        PhysicsMaterial2D wallMat = new PhysicsMaterial2D("BoardWallBounce")
        {
            bounciness = 1.0f,
            friction = 0.0f
        };

        // Tường Trái (x = 0.35), Tường Phải (x = 10.25), Tường Đỉnh (y = 7.2)
        AddWallBox(wallsRoot.transform, "Wall_Left",  new Vector2(0.15f, -1.5f), new Vector2(0.8f, 20f), wallMat);
        AddWallBox(wallsRoot.transform, "Wall_Right", new Vector2(10.45f, -1.5f), new Vector2(0.8f, 20f), wallMat);
        AddWallBox(wallsRoot.transform, "Wall_Top",   new Vector2(5.30f, 7.35f), new Vector2(11.5f, 0.8f), wallMat);
    }

    private static void AddWallBox(Transform parent, string wallName, Vector2 center, Vector2 size, PhysicsMaterial2D mat)
    {
        GameObject w = new GameObject(wallName);
        w.transform.SetParent(parent, false);
        w.transform.localPosition = new Vector3(center.x, center.y, -0.65f);
        BoxCollider2D col = w.AddComponent<BoxCollider2D>();
        col.size = size;
        col.sharedMaterial = mat;
    }

    // Hiển thị bảng thông báo Chiến thắng & Nút qua màn trực tiếp trên màn hình Game khi đang Play
    private void OnGUI()
    {
        GUIStyle infoStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        infoStyle.normal.textColor = Color.white;

        int currentLv = levelSpawner != null ? levelSpawner.levelNumber : 1;
        GUI.Box(new Rect(15, 15, 310, 36), $"LEVEL {currentLv}  |  Gạch còn lại: {remainingCubesCount}", infoStyle);

        if (isLevelWon)
        {
            Rect winBox = new Rect(Screen.width * 0.5f - 160f, Screen.height * 0.5f - 70f, 320f, 140f);
            GUI.Box(winBox, $"🎉 HOÀN THÀNH LEVEL {currentLv}! 🎉", infoStyle);

            if (GUI.Button(new Rect(Screen.width * 0.5f - 110f, Screen.height * 0.5f - 10f, 220f, 50f), "CHƠI LEVEL TIẾP THEO >>"))
            {
                if (levelSpawner != null)
                {
                    levelSpawner.levelNumber++;
                    levelSpawner.SpawnLevel(levelSpawner.levelNumber);
                }
            }
        }
    }
}
```

---

### BƯỚC 5: Cập nhật `BounceBlastLevelSpawner.cs` để Kết nối với 4 Script vừa tạo

Bây giờ bạn hãy mở file **`Assets/Scripts/BounceBlastLevelSpawner.cs`** lên để thực hiện **3 nâng cấp nhỏ** giúp bàn cờ vừa sinh ra là có thể bắn được ngay:

#### 5.1. Thêm 2 danh sách lưu Gạch & Hộp đạn vừa sinh:
Tìm đến ngay phía trên hàm `private void Start()` (khoảng dòng 75) trong `BounceBlastLevelSpawner.cs`, thêm 2 dòng khai báo này:

```csharp
    private readonly List<CubeUnit> _spawnedCubeUnits = new List<CubeUnit>();
    private readonly List<AmmoBoxUnit> _spawnedAmmoUnits = new List<AmmoBoxUnit>();
```

#### 5.2. Xoay riêng phần hình ảnh `Model` bên trong `SpawnCubeCell` và gắn `CubeUnit`:
Trong hàm **`SpawnCubeCell(...)`** của `BounceBlastLevelSpawner.cs`:
* Thay vì xoay cả cục `cube.transform.localRotation = FaceCameraRotation;` (làm dẹt `BoxCollider2D`), bạn đổi thành:
  * Đặt `cube.transform.localRotation = Quaternion.identity;`
  * Xoay riêng object con `Model` góc `FaceCameraRotation` (`-90, 0, 0`).
  * Gắn `CubeUnit` lên viên gạch.

Cụ thể, sửa hàm **`SpawnCubeCell`** thành như sau:

```csharp
    private void SpawnCubeCell(string cellCode, Vector3 localPos, float scale, Dictionary<string, int> cubeHpDict)
    {
        if (cubePrefab == null) return;

        string cleanCode = cellCode.StartsWith("/") ? cellCode.Substring(1) : cellCode;
        string mainPart = cleanCode.Split('/')[0];
        string[] tokens = mainPart.Split('-');
        string colorCode = tokens[0];
        int hp = (tokens.Length > 1 && int.TryParse(tokens[1], out int parsedHp)) ? parsedHp : 1;

        GameObject cube = Instantiate(cubePrefab, spawnedContainer);
        cube.name = $"Cube_{cellCode}";
        cube.transform.localPosition = localPos;
        // QUAN TRỌNG: Giữ góc xoay gốc (0,0,0) để BoxCollider2D nằm chuẩn trên mặt phẳng X-Y đón bóng!
        cube.transform.localRotation = Quaternion.identity;
        cube.transform.localScale = Vector3.one * scale;

        // Chỉ xoay phần hình ảnh 3D 'Model' bên trong hướng ra Camera
        Transform modelChild = cube.transform.Find("Model");
        if (modelChild != null)
        {
            modelChild.localRotation = FaceCameraRotation;
        }

        if (ColorMap.TryGetValue(colorCode, out Color col))
        {
            ApplyColorToRenderers(cube, col);
        }

        TextMeshPro tmp = cube.GetComponentInChildren<TextMeshPro>(true);

        // Gắn component CubeUnit (Tuần 2)
        CubeUnit unit = cube.GetComponent<CubeUnit>();
        if (unit == null) unit = cube.AddComponent<CubeUnit>();
        unit.Setup(colorCode, hp, tmp);
        _spawnedCubeUnits.Add(unit);

        totalCubesSpawned++;
        if (IsStandardColor(colorCode))
        {
            totalCubeHP += hp;
            cubeHpDict.TryGetValue(colorCode, out int existingHp);
            cubeHpDict[colorCode] = existingHp + hp;
        }
    }
```

#### 5.3. Truyền tọa độ Hàng (`r`), Cột (`c`) vào `SpawnAmmoOrTunnelCell` và đăng ký với `BounceBlastGameplayManager`:
1. Trong hàm `ParseAndSpawnFromCsv`, ngay đầu hàm (sau `totalTunnelAmmoBoxes = 0;`), thêm:
   ```csharp
   _spawnedCubeUnits.Clear();
   _spawnedAmmoUnits.Clear();
   float ammoTopY = -12.2f;
   float ammoSpacingY = 1.25f;
   ```
2. Trong vòng lặp sinh `AMMUNITIONS`, truyền thêm chỉ số hàng `r` và cột `c - minAmmoCol` vào `SpawnAmmoOrTunnelCell`:
   ```csharp
   SpawnAmmoOrTunnelCell(r, c - minAmmoCol, cell, new Vector3(x, y, -0.8f), ammoScale, ammoTunnels, ammoCountDict);
   ```
3. Ở cuối hàm `SpawnAmmoOrTunnelCell` (ngay sau khi tạo `ammo`), gắn `AmmoBoxUnit`:
   ```csharp
   AmmoBoxUnit ammoUnit = ammo.GetComponent<AmmoBoxUnit>();
   if (ammoUnit == null) ammoUnit = ammo.AddComponent<AmmoBoxUnit>();
   ammoUnit.Setup(rowIndex, colIndex, colorCode, ballCount, displayColor, tmp);
   _spawnedAmmoUnits.Add(ammoUnit);
   ```
4. Ở cuối hàm `ParseAndSpawnFromCsv`, tự động gắn và đăng ký với `BounceBlastGameplayManager`:
   ```csharp
   if (Application.isPlaying)
   {
       BounceBlastGameplayManager gm = GetComponent<BounceBlastGameplayManager>();
       if (gm == null) gm = gameObject.AddComponent<BounceBlastGameplayManager>();
       gm.levelSpawner = this;
       gm.RegisterSpawnedLevel(_spawnedCubeUnits, _spawnedAmmoUnits, ammoTopY, ammoSpacingY, spawnedContainer);
   }
   ```

---

## 🎮 PHẦN 3: THAO TÁC TRÊN UNITY EDITOR ĐỂ CHẠY THỬ (TEST GAMEPLAY)

Sau khi bạn đã tạo xong 4 file script (`CubeUnit.cs`, `AmmoBoxUnit.cs`, `BouncingBall.cs`, `BounceBlastGameplayManager.cs`) và lưu lại:

1. **Bước 1 — Kiểm tra Console**:
   * Quay lại cửa sổ **Unity Editor**, chờ 2–3 giây để Unity biên dịch.
   * Nhìn xuống tab **Console**: Đảm bảo không có dòng báo lỗi màu đỏ nào.
2. **Bước 2 — Gắn `BounceBlastGameplayManager` lên `Board - Default`**:
   * Trên cửa sổ **Hierarchy** (bên trái), click chọn **`Board - Default`**.
   * Nhìn sang cửa sổ **Inspector** (bên phải), cuộn xuống dưới cùng bấm nút **`Add Component`**.
   * Gõ tìm **`BounceBlastGameplayManager`** và nhấn `Enter` để gắn vào `Board - Default`.
3. **Bước 3 — Bấm Play và Trải nghiệm thành quả Tuần 2!**:
   * Chuyển sang tab **`Game`** (hoặc **`Simulator`**) ở giữa màn hình.
   * Bấm nút **`▶ Play`** ở chính giữa phía trên cùng của Unity.
   * **Thao tác chơi thử**:
     * Dùng chuột **Click vào các Hộp Đạn ở hàng trên cùng** (ví dụ hộp màu Hồng `8` hoặc Xanh lơ `8` dưới chân bàn chơi).
     * Quan sát hộp đạn đếm ngược `8 -> 7 -> 6... -> 0`, đồng thời 8 viên bi cùng màu bắn ra từ đỉnh bàn chơi, **nảy vật lý 2D tưng bừng** và phá vỡ từng viên gạch cùng màu!
     * Khi hộp đạn hàng đầu bắn xong, hộp đạn ở hàng dưới tự động **trượt mượt mà lên thế chỗ**!
     * Khi phá hết 48 viên gạch, bảng **`🎉 HOÀN THÀNH LEVEL 1!`** sẽ hiện ra kèm nút **`CHƠI LEVEL TIẾP THEO >>`** để bạn bấm qua thẳng Level 2, Level 3, Level 4...!

---

## ❓ PHẦN 4: XỬ LÝ SỰ CỐ THƯỜNG GẶP (TROUBLESHOOTING CHO INTERN)

| Hiện tượng khi bấm Play | Nguyên nhân | Cách khắc phục |
| :--- | :--- | :--- |
| **Bấm vào hộp đạn dưới bàn mà không thấy gì xảy ra** | Bạn đang bấm vào hộp đạn ở **hàng thứ 2** (chỉ hàng trên cùng `rowIndex == 0` mới được phép bấm), hoặc `Main Camera` chưa để `Orthographic`. | Bấm vào hộp đạn nằm ở **hàng cao nhất** của cột đạn. Nếu vẫn không nhận, bấm menu `Tools -> Bounce Blast -> 3. Dựng Bàn Chơi & Nạp Level 1 lên Scene` để chuẩn hóa lại Camera. |
| **Bi bắn ra nhưng bay xuyên qua viên gạch không nảy** | Trong `SpawnCubeCell`, dòng `cube.transform.localRotation` vẫn đang để `FaceCameraRotation` (`-90, 0, 0`) làm dẹt `BoxCollider2D`. | Kiểm tra lại Bước 5.2: Phải để `cube.transform.localRotation = Quaternion.identity;` và chỉ xoay object con `Model` bên trong! |
| **Gõ tên file C# bị lệch với tên `public class`** | Ví dụ đặt tên file là `Cubeunit.cs` (chữ `u` thường) nhưng trong code là `public class CubeUnit`. | Đổi lại tên file trong cửa sổ Project của Unity cho trùng khớp 100% từng chữ hoa/thường với tên `class`. |
