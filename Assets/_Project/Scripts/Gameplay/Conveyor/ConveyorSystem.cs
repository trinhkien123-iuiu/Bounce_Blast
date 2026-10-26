using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Hai máng trái/phải. Chọn máng cho hộp vừa bấm và cho hộp bay từ khay vào chân máng</summary>
    public class ConveyorSystem : MonoBehaviour
    {
        ConveyorLane _left;
        ConveyorLane _right;
        GameConfig _config;

        public int PendingBalls => _left.PendingBalls + _right.PendingBalls;
        public bool IsBusy => _left.IsBusy || _right.IsBusy;

        /// <summary>Xử lý khởi tạo hệ máng</summary>
        public void Init(GameConfig config, BallManager balls)
        {
            _config = config;
            _left = CreateLane("Lane_Left", balls, false);
            _right = CreateLane("Lane_Right", balls, true);
            CreateArrows();
        }

        /// <summary>Mũi tên trôi trên mặt máng</summary>
        void CreateArrows()
        {
            SpriteRenderer template = null;
            GameObject conv = GameObject.Find("Conveyor_Container");
            if (conv != null)
                foreach (SpriteRenderer sr in conv.GetComponentsInChildren<SpriteRenderer>(true))
                    if (sr.name == "ArrowL")
                    {
                        if (template == null) template = sr;
                        sr.gameObject.SetActive(false);
                    }
            if (template == null) return;
            foreach (MonoBehaviour mb in conv.GetComponentsInChildren<MonoBehaviour>(true))
                if (mb != null && mb.GetType().Name == "ConveyorArrowScroller") mb.enabled = false;
            Vector3 scale = template.transform.lossyScale;
            Color c = _config.normalTheme != null ? _config.normalTheme.arrowColor : template.color;
            _left.CreateArrows(template.sprite, c, scale, 2);
            _right.CreateArrows(template.sprite, c, scale, 2);
        }

        /// <summary>Xử lý đổi màu mũi tên theo độ khó</summary>
        public void SetArrowColor(Color c)
        {
            _left.SetArrowColor(c);
            _right.SetArrowColor(c);
        }

        ConveyorLane CreateLane(string name, BallManager balls, bool mirror)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lane = go.AddComponent<ConveyorLane>();

            const float laneX = 0.49f, topY = 8.09f, outletX = 5.30f, r = 0.62f, z = -1.15f;
            GameObject crusherGo = GameObject.Find(mirror ? "Crusher RIGHT" : "Crusher LEFT");
            CrusherFx crusher = null;
            float bottomY = _config.arena.yMin + 0.55f;
            if (crusherGo != null)
            {
                crusher = crusherGo.GetComponent<CrusherFx>();
                if (crusher == null) crusher = crusherGo.AddComponent<CrusherFx>();
                crusher.Config = _config;
                bottomY = -5.89f + 0.55f;
            }
            var pts = new List<Vector3> { new Vector3(laneX, bottomY, z), new Vector3(laneX, topY - r, z) };
            AddArc(pts, new Vector2(laneX + r, topY - r), r, 180f, 90f, z);
            pts.Add(new Vector3(outletX - r, topY, z));
            AddArc(pts, new Vector2(outletX - r, topY - r), r, 90f, 0f, z);
            pts.Add(new Vector3(outletX, 7.05f, z));

            const float laneDy = 7.86f - topY;
            for (int i = 0; i < pts.Count; i++) pts[i] += new Vector3(0f, laneDy, 0f);
            float mirrorAxis = outletX * 2f;
            if (mirror)
                for (int i = 0; i < pts.Count; i++) pts[i] = new Vector3(mirrorAxis - pts[i].x, pts[i].y, pts[i].z);

            Vector3 dock = new Vector3(mirror ? mirrorAxis - laneX : laneX, bottomY - 0.15f, z);
            if (crusherGo != null)
            {
                Transform c1 = FindDeep(crusherGo.transform, "Cutter1"), c2 = FindDeep(crusherGo.transform, "Cutter2");
                if (c1 != null && c2 != null)
                {
                    Vector3 cc = (c1.position + c2.position) * 0.5f;
                    dock = cc;
                }
                Transform frame = FindDeep(crusherGo.transform, "Plane.028");
                Renderer fr = frame != null ? frame.GetComponent<Renderer>() : null;
                if (fr != null) lane.DockWidth = fr.bounds.size.x * 0.72f;
                lane.DockRotation = crusherGo.transform.rotation;
            }
            lane.Init(_config, balls, pts.ToArray(), dock);
            lane.Crusher = crusher;
            return lane;
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        static void AddArc(List<Vector3> pts, Vector2 c, float r, float from, float to, float z)
        {
            const int steps = 16;
            for (int i = 1; i < steps; i++)
            {
                float a = Mathf.Lerp(from, to, i / (float)steps) * Mathf.Deg2Rad;
                pts.Add(new Vector3(c.x + Mathf.Cos(a) * r, c.y + Mathf.Sin(a) * r, z));
            }
        }

        /// <summary>Gửi hộp lên máng: chọn máng ít bi chờ hơn</summary>
        ConveyorLane PickLane(AmmoBox box)
        {
            int l = _left.PendingBalls, r = _right.PendingBalls;
            bool useLeft = l == r ? box.transform.position.x <= _config.dispenserOutlet.x : l < r;
            return useLeft ? _left : _right;
        }

        /// <summary>Hộp vượt sức chứa: cho hộp chạy hoạt ảnh bay tới máng sẽ dùng rồi DỪNG giữa chừng</summary>
        public void HoldBoxMidFlight(AmmoBox box)
        {
            ConveyorLane lane = PickLane(box);
            box.Flight.HeldLane = lane;
            box.HoldFlightAt(lane.DockPosition, lane.DockRotation, lane.DockWidth, _config.boxFlyDuration, _config.hoverHoldProgress);
        }

        /// <summary>Xử lý đưa hộp lên máng</summary>
        public void Send(AmmoBox box)
        {
            ConveyorLane lane = box.Flight.HeldLane != null ? box.Flight.HeldLane : PickLane(box);
            box.Flight.HeldLane = null;
            box.transform.SetParent(lane.transform, true);
            box.Flight.DockWidth = lane.DockWidth;
            box.Flight.DockRotation = lane.DockRotation;
            Vector3 target = lane.Reserve(box);
            box.Flight.FlyTo(target, _config.boxFlyDuration, () => lane.Arrived(box), () => lane.HeadArrived(box));
        }

        /// <summary>Xử lý xoá toàn bộ hộp và bi đang trên máng</summary>
        public void ClearAll()
        {
            _left.ClearAll();
            _right.ClearAll();
        }
    }
}
