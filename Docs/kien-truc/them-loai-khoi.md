# Thêm một loại khối mới trên bàn

> Dành cho người code sau. Mọi điều riêng của một loại khối (luật, cách dựng hình, cách vỡ) nằm trong **một file** ở
> `Assets/_Project/Scripts/Gameplay/Board/Kinds/`. Các phần khác của game (`BoardBlock`, `BoardController`, `LevelParser`,
> bi, booster) chỉ hỏi loại khối, **không** tự rẽ nhánh theo loại. Vì vậy thêm loại mới không phải sửa code cũ.

## 1. Bức tranh tổng thể

```
LevelParser (đọc CSV)  ──tạo──▶  BoardItemData { Kind = BoardItemKind.X }
                                         │
BoardController.Spawn ──BlockKinds.Of(Kind)──▶ BlockKind (luật của loại X)
        │                                        ├─ Build(...)      dựng hình + nhãn số + collider
        ▼                                        ├─ Accepts(...)    bi màu nào trừ máu
   BoardBlock.Rules ─────────────────────────────├─ CountsForWin, HitByAnyColor, IsStatic, IsColorTarget
        │                                        ├─ ShowsHpLabel, HitPunch, ShearMesh, CubeLook, Material
        ▼                                        └─ OnBroken(...)   hiệu ứng / hệ quả khi vỡ
 bi va chạm → BoardController.HandleBallHit → BoardBlock.TakeHit → hết máu → DestroyBlock → Rules.OnBroken
```

| File | Vai trò |
| --- | --- |
| `Kinds/BlockKind.cs` | Lớp gốc. Mặc định = gạch thường: theo màu, số khi nhiều máu, vỡ ra giọt thạch cùng màu |
| `Kinds/BlockKinds.cs` | Bảng tra `BoardItemKind → BlockKind` (đăng ký mỗi loại một dòng) |
| `Kinds/CubeKind.cs` | Gạch 1 ô |
| `Kinds/CombinedCubeKind.cs` | Khối lớn nhiều ô, kể cả hình chữ L/T |
| `Kinds/CrateKind.cs` | Hòm gỗ (vỡ thì lộ vật bên trong) |
| `Kinds/WallKind.cs` | Vật cản tĩnh (cũng là chỗ rơi về của token chưa hỗ trợ) |
| `Kinds/ColumnKind.cs` | Cột nhà `/wl-hp-id` |

## 2. Các luật có thể chỉnh (override trong lớp con)

| Thuộc tính / hàm | Mặc định | Ý nghĩa |
| --- | --- | --- |
| `CountsForWin` | `true` | Phải phá hết mới thắng (tường, cột nhà: `false`) |
| `HitByAnyColor` | `false` | Bi màu nào cũng trừ máu, bi không bị tiêu hao, bi không chủ động ngắm (hòm, cột) |
| `IsStatic` | `false` | Không rơi khi mất chỗ đỡ (tường) |
| `IsColorTarget` | `false` | Bi cùng màu trừ máu và ngắm thẳng tới; máu tính vào ngân sách bi của level (gạch, khối gộp) |
| `Accepts(block, màu bi)` | theo 2 dòng trên | Muốn luật riêng (ví dụ chỉ bi đen) thì override |
| `ShowsHpLabel(block)` | khi nhiều máu hoặc `HitByAnyColor` | Có hiện số máu không |
| `HitPunch` | `0.04` | Độ nảy hình khi bị trúng |
| `HasLabel`, `ShearMesh`, `CubeLook`, `Material(...)` | | Chi tiết hình khi dùng đường dựng mặc định (prefab cube) |
| `Build(...)` | `board.Visuals.BuildDefaultView` | Dựng hình riêng (xem `CrateKind`, `ColumnKind`) |
| `OnBroken(...)` | giọt thạch cùng màu | Hiệu ứng / hệ quả khi vỡ (hòm lộ vật bên trong, cột văng mảnh…) |

`BoardController` mở sẵn cho lớp loại khối dùng: `Config`, `Colors`, `CellSize`, `CellToWorld`, `Spawn`, `DestroyBlock`, `Blocks`,
`Visuals` (`BlockVisualFactory`: `CreateVisual`, `CreateLabel`, `CreateSolidVisual`, `BuildDefaultView`) và `BreakFx` (`SpawnJellyBreak`).

## 3. Các bước thêm loại mới (ví dụ: khối bom)

1. **Enum:** thêm `Bomb` vào `BoardItemKind` (`Scripts/Gameplay/Data/LevelModel.cs`).
2. **Lớp luật:** tạo `Kinds/BombKind.cs`, chỉ viết những gì khác mặc định:

   ```csharp
   public class BombKind : BlockKind
   {
       public override BoardItemKind Kind => BoardItemKind.Bomb;
       public override bool HitByAnyColor => true;          // bi nào cũng kích nổ
       public override void OnBroken(BoardController board, BoardBlock block)
       {
           board.BreakFx.SpawnJellyBreak(block, perCell: false);
           // trừ 1 máu mọi khối kề bên (ví dụ – chưa làm trong game)
           RectInt area = block.Cells; area.xMin -= 1; area.yMin -= 1; area.xMax += 1; area.yMax += 1;
           foreach (BoardBlock n in new List<BoardBlock>(board.Blocks))
               if (n != block && n.IsDestructible && area.Overlaps(n.Cells) && n.TakeHit()) board.DestroyBlock(n);
       }
   }
   ```
3. **Đăng ký:** thêm `Register(new BombKind());` trong `BlockKinds`.
4. **Đọc level:** trong `LevelParser` (vòng lặp ô bàn), nhận token của game gốc (ví dụ `/bomb-…`) và tạo
   `new BoardItemData { Kind = BoardItemKind.Bomb, Hp = …, Cells = rect }`. Hiện các token `/…` chưa hỗ trợ đều thành `Wall`.
5. **Asset:** prefab / material của bom gốc nằm trong `Assets/_Archive/TinhNangChuaLam/` (kéo ra thư mục `Art/Board/`),
   gán vào `GameConfig` nếu cần, rồi dùng trong `Build`.
6. **Kiểm thử:** dùng công cụ cheat (Go to level) mở màn có token đó; kiểm tra luật bằng cách bắn thử, xem số khối còn lại.

Không cần sửa `BoardBlock`, `BoardController`, `Ball`, `BoosterManager`.

## 4. Những chỗ vẫn còn ngoài hệ thống này

- **Hộp đạn trong khay** (băng, cửa, tunnel) chưa theo mô hình này. Nếu thêm nhiều loại blocker khay nữa nên làm tương tự cho `AmmoBox`.
- `LevelParser` vẫn đọc token bằng `if`; đây là chỗ hợp lý (chuyển chữ trong CSV thành dữ liệu), không phải luật chơi.
