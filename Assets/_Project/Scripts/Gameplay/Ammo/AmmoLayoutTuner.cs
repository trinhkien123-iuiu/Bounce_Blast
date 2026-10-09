using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Bộ thông số tư thế hộp đạn trong khay – TOÀN BỘ hộp dùng chung, AmmoBox đọc mỗi frame.
    /// Giá trị mặc định = bộ số user chốt (Docs 3.28). Chỉnh bằng component <see cref="AmmoLayoutTuner"/>.
    /// </summary>
    public static class AmmoBoxTuning
    {
        /// <summary>Tư thế một nhóm hộp (active / chưa active).</summary>
        public struct Pose
        {
            public float tiltX;      // độ nghiêng quanh trục X (-90 = nắp thẳng mặt camera; xa -90 → lộ thân dưới nắp nhiều hơn)
            public float length;     // hệ số chiều dài cột jelly (1 = nguyên bản ~1.9 ô)
            public Vector3 offset;   // dịch (theo bề rộng 1 ô): x phải+, y lên+, z ra xa camera+
            public float scale;      // nhân thêm vào cỡ hộp (1 = theo GameConfig)
        }

        public static Pose Active = new Pose { tiltX = -55f, length = 0.6f, offset = new Vector3(-0.01f, 0.69f, 0f), scale = 1.038f };
        public static Pose Inactive = new Pose { tiltX = -78f, length = 0.4f, offset = new Vector3(0f, 0.07f, 0f), scale = 1.06f };
        /// <summary>Độ lún của hộp chưa active (theo ô, ra xa camera). Lớn = chìm sau mặt khay, nhỏ = nổi ngang mặt khay.</summary>
        public static float SinkDepth = 1.1f;

        /// <summary>Tăng mỗi lần chỉnh để AmmoBox biết phải dựng lại tư thế ngay (không đợi hộp đổi trạng thái).</summary>
        public static int Version;

        public static Pose Lerp(float kActive01) => new Pose
        {
            tiltX = Mathf.Lerp(Inactive.tiltX, Active.tiltX, kActive01),
            length = Mathf.Lerp(Inactive.length, Active.length, kActive01),
            offset = Vector3.Lerp(Inactive.offset, Active.offset, kActive01),
            scale = Mathf.Lerp(Inactive.scale, Active.scale, kActive01),
        };
    }

    /// <summary>
    /// CÁCH DÙNG: add component này vào bất kỳ GameObject nào (đã có sẵn trên object GameManager của scene Gameplay).
    /// Vào Play rồi kéo các thông số – mọi hộp trong khay đổi NGAY THEO. Thoát Play thì giá trị trên Inspector bị Unity trả về:
    /// muốn giữ thì Copy Component trong lúc Play rồi Paste Component Values sau khi dừng (hoặc chép số vào ô bên ngoài Play).
    /// Ngoài Play, giá trị lưu trong scene và được áp mỗi khi vào Play.
    /// Menu chuột phải tiêu đề component: "Reset về mặc định", "In giá trị ra Console".
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Bounce Blast/Ammo Layout Tuner")]
    public class AmmoLayoutTuner : MonoBehaviour
    {
        [Header("Hộp ACTIVE (bấm được)")]
        [Range(-90f, -45f)] public float activeTilt = -55f;
        [Range(0.1f, 1.2f)] public float activeLength = 0.6f;
        [Tooltip("x: phải+ | y: lên+ | z: ra xa camera+ (đơn vị = bề rộng 1 ô)")]
        public Vector3 activeOffset = new Vector3(-0.01f, 0.69f, 0f);
        [Range(0.5f, 1.3f)] public float activeScale = 1.038f;

        [Header("Hộp CHƯA active")]
        [Range(-90f, -45f)] public float inactiveTilt = -78f;
        [Range(0.1f, 1.2f)] public float inactiveLength = 0.4f;
        public Vector3 inactiveOffset = new Vector3(0f, 0.07f, 0f);
        [Range(0.5f, 1.3f)] public float inactiveScale = 1.06f;

        [Header("Độ lún (hộp chưa active)")]
        [Range(0f, 2f)] public float sinkDepth = 1.1f;

        [Header("Tuỳ chọn")]
        [Tooltip("Tắt = component không ghi đè, dùng giá trị mặc định trong code")]
        public bool apply = true;

        void OnEnable() => Push();
        void OnValidate() => Push();
        void Update() { if (apply && Changed()) Push(); }

        int _lastHash;

        bool Changed()
        {
            int h = HashValues();
            if (h == _lastHash) return false;
            _lastHash = h;
            return true;
        }

        int HashValues()
        {
            unchecked
            {
                int h = 17;
                foreach (float f in new[] { activeTilt, activeLength, activeScale, inactiveTilt, inactiveLength, inactiveScale, sinkDepth,
                                            activeOffset.x, activeOffset.y, activeOffset.z, inactiveOffset.x, inactiveOffset.y, inactiveOffset.z })
                    h = h * 31 + f.GetHashCode();
                return h;
            }
        }

        void Push()
        {
            if (!apply) return;
            AmmoBoxTuning.Active = new AmmoBoxTuning.Pose { tiltX = activeTilt, length = activeLength, offset = activeOffset, scale = activeScale };
            AmmoBoxTuning.Inactive = new AmmoBoxTuning.Pose { tiltX = inactiveTilt, length = inactiveLength, offset = inactiveOffset, scale = inactiveScale };
            AmmoBoxTuning.SinkDepth = sinkDepth;
            AmmoBoxTuning.Version++;
            _lastHash = HashValues();
        }

        [ContextMenu("Reset về mặc định")]
        void ResetDefaults()
        {
            activeTilt = -55f; activeLength = 0.6f; activeOffset = new Vector3(-0.01f, 0.69f, 0f); activeScale = 1.038f;
            inactiveTilt = -78f; inactiveLength = 0.4f; inactiveOffset = new Vector3(0f, 0.07f, 0f); inactiveScale = 1.06f;
            sinkDepth = 1.1f;
            Push();
        }

        [ContextMenu("In giá trị ra Console")]
        void Dump()
        {
            Debug.Log($"[AmmoLayoutTuner] Active tilt {activeTilt} len {activeLength} off {activeOffset} scale {activeScale} | " +
                      $"Inactive tilt {inactiveTilt} len {inactiveLength} off {inactiveOffset} scale {inactiveScale} | sink {sinkDepth}");
        }
    }
}
