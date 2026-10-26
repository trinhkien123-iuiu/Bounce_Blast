using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Mũi tên trôi trên mặt một máng</summary>
    public class LaneArrows
    {
        const float ArrowZ = -1.05f;

        readonly ConveyorLane _lane;
        readonly GameConfig _config;
        readonly List<SpriteRenderer> _arrows = new List<SpriteRenderer>();
        Color _color = Color.white;
        float _phase;

        float Spacing => _config != null ? _config.laneArrowSpacing : 1.25f;
        float Speed => _config != null ? _config.laneArrowSpeed : 1.5f;

        public LaneArrows(ConveyorLane lane, GameConfig config)
        {
            _lane = lane;
            _config = config;
        }

        /// <summary>Dựng các mũi tên trên máng</summary>
        public void Create(Sprite sprite, Color color, Vector3 scale, int sortingOrder)
        {
            float length = _lane.Length;
            if (sprite == null || length <= 0f) return;
            _color = color;
            int n = Mathf.FloorToInt(length / Spacing);
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject("Arrow");
                go.transform.SetParent(_lane.transform, false);
                go.transform.localScale = scale;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = color;
                sr.sortingOrder = sortingOrder;
                _arrows.Add(sr);
            }
            Tick(0f);
        }

        /// <summary>Xử lý đổi màu mũi tên</summary>
        public void SetColor(Color color)
        {
            _color = color;
            Tick(0f);
        }

        /// <summary>Xử lý cập nhật chuyển động mũi tên mỗi khung</summary>
        public void Tick(float dt)
        {
            if (_arrows.Count == 0) return;
            float length = _lane.Length;
            _phase = Mathf.Repeat(_phase + Speed * dt, Spacing);
            for (int i = 0; i < _arrows.Count; i++)
            {
                float s = _phase + i * Spacing;
                Transform t = _arrows[i].transform;
                bool show = s > 0.2f && s < length - 0.6f;
                _arrows[i].enabled = show;
                if (!show) continue;
                Color c = _color;
                c.a *= Alpha(s, length);
                _arrows[i].color = c;
                Vector3 p = _lane.EvalCenter(s);
                Vector3 d = _lane.EvalTangent(s);
                t.position = new Vector3(p.x, p.y, ArrowZ);
                if (d.sqrMagnitude > 1e-6f) t.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
            }
        }

        /// <summary>Hiện dần ở đầu máng, mờ dần sau góc cua</summary>
        float Alpha(float s, float length)
        {
            float a = Mathf.Clamp01(s / 0.8f);
            float corner = _lane.CornerDistance;
            if (s > corner)
                a *= Mathf.Lerp(1f, 0.2f, Mathf.Clamp01((s - corner) / Mathf.Max(0.01f, length - corner)));
            return a;
        }
    }
}
