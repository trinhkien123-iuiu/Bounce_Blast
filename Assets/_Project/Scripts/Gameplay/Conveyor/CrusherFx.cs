using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Máy xay ở chân mỗi máng (mesh gốc "Crusher" trong Board - Default: khung Plane.028 + lưỡi Cutter1/Cutter2).
    /// Hộp đạn đậu trên máy xay; khi nhả bi thì lưỡi quay và cả khối rung
    /// (thông số gốc KPConveyorCrusher: shakeMagnitude 0.05, shakeFrequency 25, dampening 5).
    /// </summary>
    public class CrusherFx : MonoBehaviour
    {
        const float ShakeMagnitude = 0.06f, ShakeFrequency = 28f, Dampening = 6f, SpinSpeed = 800f;

        Transform _cutter1, _cutter2;
        Vector3 _basePos;
        float _shake;
        float _spinTimer;

        ParticleSystem _chunkParticles;
        ParticleSystem _sparkParticles;

        void Awake()
        {
            _basePos = transform.localPosition;
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Cutter1") _cutter1 = t;
                else if (t.name == "Cutter2") _cutter2 = t;
            }

            SetupParticleSystems();
        }

        static Texture2D _circleTex;
        static Texture2D _starTex;

        static Texture2D GetCircleTexture()
        {
            if (_circleTex != null) return _circleTex;
            int size = 64;
            _circleTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            _circleTex.wrapMode = TextureWrapMode.Clamp;
            _circleTex.filterMode = FilterMode.Bilinear;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.45f;
            Color[] cols = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(1f - Mathf.SmoothStep(radius * 0.5f, radius, dist));
                    cols[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            _circleTex.SetPixels(cols);
            _circleTex.Apply();
            return _circleTex;
        }

        static Texture2D GetStarTexture()
        {
            if (_starTex != null) return _starTex;
            int size = 64;
            _starTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            _starTex.wrapMode = TextureWrapMode.Clamp;
            _starTex.filterMode = FilterMode.Bilinear;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            Color[] cols = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - center.x) / (size * 0.5f);
                    float dy = Mathf.Abs(y - center.y) / (size * 0.5f);
                    float star = Mathf.Pow(Mathf.Clamp01(1f - dx), 4f) * Mathf.Pow(Mathf.Clamp01(1f - dy), 4f);
                    float core = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), center) / (size * 0.25f));
                    float a = Mathf.Clamp01(star * 1.2f + core * 0.8f);
                    cols[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            _starTex.SetPixels(cols);
            _starTex.Apply();
            return _starTex;
        }

        void SetupParticleSystems()
        {
            // 1. Hệ thống hạt vụn thạch tròn (Jelly Drops / Splatter)
            var chunkGo = new GameObject("CrusherChunks_VFX");
            chunkGo.transform.SetParent(transform, false);
            chunkGo.transform.localPosition = new Vector3(0f, 0.25f, -0.6f);
            chunkGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            _chunkParticles = chunkGo.AddComponent<ParticleSystem>();
            var chunkRend = chunkGo.GetComponent<ParticleSystemRenderer>();
            chunkRend.sortingOrder = 105;
            Material chunkMat = new Material(Shader.Find("Sprites/Default"));
            chunkMat.mainTexture = GetCircleTexture();
            chunkRend.material = chunkMat;

            var main = _chunkParticles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 7.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.26f);
            main.gravityModifier = 3.5f;

            var shape = _chunkParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 45f;
            shape.radius = 0.25f;

            var emission = _chunkParticles.emission;
            emission.rateOverTime = 0;

            var sizeOverLife = _chunkParticles.sizeOverLifetime;
            sizeOverLife.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 1f);
            sizeCurve.AddKey(0.7f, 0.8f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // 2. Hệ thống tia lửa sao lấp lánh (Sparks / Stars)
            var sparkGo = new GameObject("CrusherSparks_VFX");
            sparkGo.transform.SetParent(transform, false);
            sparkGo.transform.localPosition = new Vector3(0f, 0.25f, -0.65f);
            sparkGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            _sparkParticles = sparkGo.AddComponent<ParticleSystem>();
            var sparkRend = sparkGo.GetComponent<ParticleSystemRenderer>();
            sparkRend.sortingOrder = 106;
            Material sparkMat = new Material(Shader.Find("BounceBlast/Particles/Additive") ?? Shader.Find("Sprites/Default"));
            sparkMat.mainTexture = GetStarTexture();
            sparkRend.material = sparkMat;

            var sparkMain = _sparkParticles.main;
            sparkMain.playOnAwake = false;
            sparkMain.loop = false;
            sparkMain.simulationSpace = ParticleSystemSimulationSpace.World;
            sparkMain.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            sparkMain.startSpeed = new ParticleSystem.MinMaxCurve(6f, 11f);
            sparkMain.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.22f);
            sparkMain.gravityModifier = 1.5f;

            var sparkShape = _sparkParticles.shape;
            sparkShape.shapeType = ParticleSystemShapeType.Cone;
            sparkShape.angle = 60f;
            sparkShape.radius = 0.15f;

            var sparkEmission = _sparkParticles.emission;
            sparkEmission.rateOverTime = 0;

            var sparkSizeOverLife = _sparkParticles.sizeOverLifetime;
            sparkSizeOverLife.enabled = true;
            sparkSizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        }

        /// <summary>Kích hoạt khi một hộp đạn mới vừa đáp vào máy nghiền (nổ tia lớn).</summary>
        public void OnBoxEnter(Color color)
        {
            _shake = 1.4f;
            _spinTimer = 0.35f;
            EmitBurst(color, 24, 16);
        }

        /// <summary>Gọi mỗi lần một viên bi được xay ra (phun tia liên tục).</summary>
        public void Pulse(Color color)
        {
            _shake = Mathf.Max(_shake, 0.9f);
            _spinTimer = 0.25f;
            EmitBurst(color, Random.Range(3, 6), Random.Range(2, 4));
        }

        public void Pulse() => Pulse(Color.white);

        void EmitBurst(Color color, int chunkCount, int sparkCount)
        {
            if (_chunkParticles != null && chunkCount > 0)
            {
                var emitParams = new ParticleSystem.EmitParams();
                for (int i = 0; i < chunkCount; i++)
                {
                    float variation = Random.Range(-0.06f, 0.06f);
                    Color c = new Color(
                        Mathf.Clamp01(color.r + variation),
                        Mathf.Clamp01(color.g + variation),
                        Mathf.Clamp01(color.b + variation),
                        1f
                    );
                    emitParams.startColor = c;
                    _chunkParticles.Emit(emitParams, 1);
                }
            }

            if (_sparkParticles != null && sparkCount > 0)
            {
                var emitParams = new ParticleSystem.EmitParams();
                Color brightColor = Color.Lerp(color, Color.white, 0.35f);
                for (int i = 0; i < sparkCount; i++)
                {
                    emitParams.startColor = brightColor;
                    _sparkParticles.Emit(emitParams, 1);
                }
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (_spinTimer > 0f)
            {
                _spinTimer -= dt;
                // hai lưỡi quay ngược chiều nhau như máy nghiền
                if (_cutter1 != null) _cutter1.Rotate(SpinSpeed * dt, 0f, 0f, Space.Self);
                if (_cutter2 != null) _cutter2.Rotate(-SpinSpeed * dt, 0f, 0f, Space.Self);
            }
            if (_shake > 0.001f)
            {
                _shake = Mathf.Max(0f, _shake - _shake * Dampening * dt);
                float s = Mathf.Sin(Time.time * ShakeFrequency * Mathf.PI * 2f) * ShakeMagnitude * _shake;
                transform.localPosition = _basePos + new Vector3(s, s * 0.5f, 0f);
            }
            else transform.localPosition = _basePos;
        }
    }
}
