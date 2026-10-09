using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Tam giác chia bi dưới cửa phun (Environment Base/Triangle). Như video gốc: mỗi lần bi đập vào thì
    /// loé hiệu ứng gốc "FlashExplosionBlue" (Ring/Rays/Stars/Glow) và tam giác nảy nhẹ.
    /// Giới hạn tần suất vì bi phun liên tục.
    /// </summary>
    public class TriangleFx : MonoBehaviour
    {
        const float MinInterval = 0.12f, PunchScale = 0.12f, PunchTime = 0.18f;

        ParticleSystem _flash;
        Transform _model;
        Vector3 _baseScale;
        float _lastFlash = -1f, _punch;

        public void Init(Transform triangleRoot)
        {
            _model = triangleRoot.Find("Model");
            if (_model != null) _baseScale = _model.localScale;
            Transform f = triangleRoot.Find("FlashExplosionBlue");
            if (f != null)
            {
                _flash = f.GetComponent<ParticleSystem>();
                var main = _flash.main;
                main.playOnAwake = false;
                _flash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.GetComponent<Ball>() == null) return;
            _punch = 1f;
            if (_flash != null && Time.time - _lastFlash >= MinInterval)
            {
                _lastFlash = Time.time;
                _flash.Play(true);
            }
        }

        void Update()
        {
            if (_model == null) return;
            if (_punch > 0f) _punch = Mathf.Max(0f, _punch - Time.deltaTime / PunchTime);
            float k = 1f + Mathf.Sin(_punch * Mathf.PI) * PunchScale;
            _model.localScale = _baseScale * k;
        }
    }
}
