using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Hiệu ứng vỡ kiểu thạch cùng màu khối (dùng prefab VFX gốc đã chỉnh lại thông số hạt).</summary>
    public class BlockBreakFx
    {
        GameConfig _config;
        ColorLibrary _colors;
        BoardGrid _grid;
        float _cell;

        public void Setup(GameConfig config, ColorLibrary colors, BoardGrid grid)
        {
            _config = config;
            _colors = colors;
            _grid = grid;
            _cell = grid.Cell;
        }

        bool _breakTuned;

        const int MaxBigBreakBursts = 16;

        /// <summary>
        /// Mảnh vỡ gạch: prefab gốc chỉ 5 khối lập phương bay chậm 0.4s (và vật liệu cố định "Blue" bỏ qua màu tô).
        /// Bắn tung toé: 16–22 mảnh mọi hướng, nhanh, có trọng lực, xoay, nhỏ dần; vật liệu = vật liệu của viên gạch vỡ.
        /// </summary>
        void EnsureBreakTune()
        {
            if (_breakTuned || _config.cubeBreakVfx == null) return;
            _breakTuned = true;
            // Kiểu "thạch": giọt tròn mềm (mesh cầu), ít & chậm, trọng lực nhẹ, phồng nhẹ rồi teo dần – không vỡ vụn như bê tông
            Mesh blob = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            Vfx.Tune(_config.cubeBreakVfx, ps =>
            {
                var main = ps.main;
                main.duration = 0.6f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 4.2f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.42f); // ×cỡ ô
                main.gravityModifier = 0.9f;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.startRotation3D = false;
                main.startRotation = 0f;
                var em = ps.emission;
                em.rateOverTime = 0f;
                em.SetBursts(new[] { new ParticleSystem.Burst(0f, 8, 12) });
                var sh = ps.shape;
                sh.enabled = true;
                sh.shapeType = ParticleSystemShapeType.Sphere;
                sh.radius = 0.2f;
                var rot = ps.rotationOverLifetime;
                rot.enabled = false;
                var vel = ps.limitVelocityOverLifetime;
                vel.enabled = true;
                vel.limit = 3.5f;
                vel.dampen = 0.15f; // chậm dần, mềm như thạch
                var sz = ps.sizeOverLifetime;
                sz.enabled = true;
                sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                    new Keyframe(0f, 0.55f), new Keyframe(0.18f, 1.05f), new Keyframe(0.35f, 0.92f), new Keyframe(1f, 0f)));
                var pr = ps.GetComponent<ParticleSystemRenderer>();
                if (pr != null && blob != null) { pr.renderMode = ParticleSystemRenderMode.Mesh; pr.mesh = blob; }
            });
        }

        /// <summary>Vỡ kiểu thạch cùng màu khối. <paramref name="perCell"/>: khối lớn nổ mỗi ô một cụm (tối đa MaxBigBreakBursts) thay vì phóng to một hiệu ứng.</summary>
        public void SpawnJellyBreak(BoardBlock block, bool perCell)
        {
            EnsureBreakTune();
            Color tint = _colors.ColorOf(block.ColorCode);
            Material mat = _colors.CubeMat(block.ColorCode);
            if (perCell)
            {
                var cells = new List<Vector2Int>(block.OccupiedCells());
                int step = Mathf.Max(1, Mathf.CeilToInt(cells.Count / (float)MaxBigBreakBursts));
                for (int i = 0; i < cells.Count; i += step)
                    Vfx.Spawn(_config.cubeBreakVfx, _grid.CellToWorld(new RectInt(cells[i].x, cells[i].y, 1, 1)), tint, _cell, mat);
                AudioManager.Play(Sfx.BigBlockBreak);
            }
            else
                Vfx.Spawn(_config.cubeBreakVfx, block.transform.position, tint, Mathf.Max(block.Cells.width, block.Cells.height) * _cell, mat);
        }
    }
}
