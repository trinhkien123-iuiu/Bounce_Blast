using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Phần BAY & ĐẬU của một hộp đạn (gắn cùng GameObject với <see cref="AmmoBox"/>, AmmoBox tự thêm lúc Init):
    /// bay lên máng như ống thạch dẻo (<see cref="AmmoFlightTube"/>), dừng giữa chuyến bay khi hết sức chứa, đậu trong
    /// máy xay và co dần khi nhả bi, nhãn số bi còn lại. Máng (ConveyorLane/ConveyorSystem) điều khiển qua box.Flight.
    /// </summary>
    public class AmmoBoxFlight : MonoBehaviour
    {
        AmmoBox _box;
        GameConfig _config;
        Renderer _jelly;
        MaterialPropertyBlock _mpb;
        Transform _visual;
        float _cell;
        Coroutine _move;

        public void Init(AmmoBox box, GameConfig config, Renderer jelly, MaterialPropertyBlock mpb, Transform visual, float cell)
        {
            _box = box;
            _config = config;
            _jelly = jelly;
            _mpb = mpb;
            _visual = visual;
            _cell = cell;
            EnsureDockLabel();
            _dockLabel.gameObject.SetActive(false);   // dựng sẵn: tạo TextMeshPro lúc ống chạm máy xay gây khựng trên máy yếu
        }

        bool _holdingFlight;         // chuyến bay đang bị giữ ở tiến độ _holdU
        bool _flightReleased;
        float _holdU;
        Action _doneAfterHold;

        /// <summary>Máng đã chọn cho hộp đang dừng giữa chuyến bay (Send dùng lại đúng máng này khi bay tiếp).</summary>
        public ConveyorLane HeldLane { get; set; }
        public bool IsHoldingFlight => _holdingFlight;

        /// <summary>
        /// Bấm hộp làm vượt sức chứa (video gốc lose_coche): hộp chạy ĐÚNG hoạt ảnh bay lên máng như lúc chơi bình thường
        /// (cùng ống thạch, cùng quỹ đạo, cùng chiều) rồi DỪNG giữa chừng ở tiến độ <paramref name="holdU"/> trong lúc hiện
        /// Out Of Capacity. Chọn Play On → <see cref="FlyTo"/> thả giữ, hộp bay tiếp từ chỗ đó; Give Up → hộp đứng yên (đã dừng).
        /// Phần chuẩn bị trong khay (tắt nảy, bỏ cửa…) do <see cref="AmmoBox.HoldFlightAt"/> làm trước.
        /// </summary>
        public void HoldAt(Vector3 target, Quaternion dockRotation, float dockWidth, float duration, float holdU)
        {
            DockRotation = dockRotation;
            DockWidth = dockWidth;
            _holdingFlight = true;
            _flightReleased = false;
            _holdU = Mathf.Clamp(holdU, 0.02f, 0.95f);
            _move = StartCoroutine(FlyRoutine(target, duration, null));
        }

        /// <summary>Hộp vừa được bắn: huỷ di chuyển cũ – trừ khi đang dừng giữa chuyến bay (hết sức chứa) thì để nó bay tiếp.</summary>
        public void CancelUnlessHolding()
        {
            if (_holdingFlight) return;
            StopAllCoroutines();
            _move = null;
        }

        /// <param name="done">gọi khi ống đã yên vị hẳn (sau khi lò xo đuôi lắng)</param>
        /// <param name="headArrived">gọi sớm hơn: ngay khi ĐẦU ống chạm máy xay (tiến độ <see cref="HeadArriveU"/>) – để máy xay chạy luôn, không đợi đuôi lắng</param>
        public void FlyTo(Vector3 target, float duration, Action done, Action headArrived = null)
        {
            _headArrived = headArrived;
            if (_holdingFlight)    // đang dừng giữa chuyến bay → thả giữ, bay tiếp, xong thì gọi done
            {
                _doneAfterHold = done;
                _flightReleased = true;
                return;
            }
            if (_move != null) StopCoroutine(_move);
            _move = StartCoroutine(FlyRoutine(target, duration, done));
        }

        /// <summary>Tiến độ chuyến bay mà tại đó đầu ống coi như đã vào máy xay (easing phóng nhanh: ≈ 0.8 → đầu đã ~98% quãng đường).</summary>
        const float HeadArriveU = 0.8f;
        Action _headArrived;
        Vector3 _flightTarget;   // chỗ đậu của chuyến bay hiện tại – nhãn số bi neo vào đây khi ống còn đang bay
        bool _hasPendingMove;
        Vector3 _pendingMoveTarget;
        float _pendingMoveDuration;
        bool _fractionSet;   // đã có ai đặt độ cao thanh đậu (máy xay đã bắt đầu xay khi ống còn đang lắng) → cuối chuyến bay đừng đặt lại 1

        /// <summary>
        /// Video gốc: hộp bay lên máng như một ỐNG THẠCH DẺO – đầu đi trước theo quỹ đạo (lên khỏi hố → chéo tới trên máng
        /// → cắm xuống chân máng), đuôi bám đúng đường đầu đã đi nên rời hố thì gập khuỷu, vào máng thì cong mượt.
        /// Cuối hành trình ống co về đúng thanh đậu ở máng (ShowDocked: dài 1.5, đáy dưới dock 0.3).
        /// </summary>
        IEnumerator FlyRoutine(Vector3 target, float duration, Action done)
        {
            // ống bay ở mặt phẳng z = -2 (trước bàn chơi); điểm đầu/cuối được dời theo trục up của camera nghiêng
            // để trên màn hình vẫn trùng đúng ô trong khay và tâm máy xay
            Vector3 start = transform.position;
            Vector3 startUp = AmmoBox.GridTilt * Vector3.up;
            Vector3 dock = target;
            Vector3 dockUp = DockRotation * Vector3.up;
            float sBox = _cell * _config.ammoActiveScale;
            float dw = DockWidthResolved;
            float DockLen = dw * DockHeightRatio, DockBottom = dw * DockBottomRatio, DockR = dw * 0.5f;

            if (_jelly != null) _jelly.GetPropertyBlock(_mpb);
            if (_body != null) Destroy(_body.gameObject);
            AmmoFlightTube tube = _body = AmmoFlightTube.Create(_jelly, _mpb);
            _flying = true;
            _fractionSet = false;
            bool headFired = false;
            tube.SetPath(new[]
            {
                start,
                start + startUp * (_cell * 0.9f),
                dock + dockUp * (DockLen + 1.2f), // vào máng cắm theo đúng trục nghiêng của máy xay
                dock - dockUp * DockBottom,
            }, Mathf.Max(sBox, dw) * 1.3f);
            _visual.gameObject.SetActive(false);
            transform.rotation = DockRotation;

            // Lò xo (stretch & squash): nén lấy đà → ĐẦU phóng đi trước trong khi đuôi còn kẹt trong hố (kéo giãn từ đầu),
            // đuôi là khối lượng nối lò xo đuổi theo (giãn dần tới cuối); đầu dừng ở máng thì đuôi lao tới quá đà → ống nén
            // ngắn & phình, bật lại rồi yên. Bán kính giữ thể tích: r ~ sqrt(dài nghỉ / dài hiện tại).
            float total = tube.Length, h0 = sBox * 0.9f;
            const float Anticip = 0.035f, TailHold = 0.05f, K = 300f, Damp = 0.45f;
            // Pha LẮNG bắt đầu từ lúc đầu ống còn đang tiến vào (u = 0.6) và xong chỉ 0.1 s sau khi hết chuyến bay: ống được kéo MƯỢT về
            // đúng dáng + cỡ thanh đậu (khớp miệng máy xay) NGAY trong lúc máy xay bắt đầu chạy, thay vì còn nghiêng/phình ~0.4 s sau đó.
            // Lò xo tích phân bước cố định 1/120 s: không phụ thuộc fps máy.
            const float SettleStartU = 0.6f, SettleTail = 0.1f, Sub = 1f / 120f;
            float c = 2f * Mathf.Sqrt(K) * Damp;
            float tail = 0f, tailV = 0f, time = 0f, flyEnd = Anticip + duration, acc = 0f;
            float restTail = total - DockLen;
            _flightTarget = target;
            while (true)
            {
                float dt = Mathf.Min(Time.deltaTime, 0.05f);
                // dừng giữa chuyến bay (hết sức chứa): thời gian của hoạt ảnh đứng yên → ống giữ nguyên dáng đang có
                if (_holdingFlight && !_flightReleased && time >= Anticip + _holdU * duration) dt = 0f;
                float head, rest;
                if (time < Anticip)
                {
                    float k = Mathf.Sin(time / Anticip * Mathf.PI);
                    head = h0 * (1f - 0.15f * k);   // nén nhẹ lấy đà
                    rest = h0;
                }
                else
                {
                    float u = Mathf.Clamp01((time - Anticip) / duration);
                    float e = 1f - Mathf.Pow(1f - u, 2.4f); // phóng nhanh ngay khi bấm (không khựng), chậm dần khi tới máng
                    head = Mathf.Lerp(h0, total, e);
                    rest = Mathf.Lerp(h0, DockLen, u);
                    // đuôi: lò xo hướng về (đầu - dài nghỉ), kẹt trong hố một nhịp đầu
                    if (time - Anticip > TailHold * duration)
                    {
                        acc += dt;
                        while (acc >= Sub)
                        {
                            float a = K * ((head - rest) - tail) - c * tailV;
                            tailV += a * Sub;
                            tail += tailV * Sub;
                            acc -= Sub;
                        }
                    }
                    tail = Mathf.Clamp(tail, 0f, head - 0.25f * rest);
                }
                float settleStart = Anticip + SettleStartU * duration;
                float settle = time <= settleStart ? 0f : Mathf.Clamp01((time - settleStart) / (flyEnd + SettleTail - settleStart));
                float sm = settle * settle * (3f - 2f * settle);
                if (settle > 0f) { head = Mathf.Lerp(head, total, sm); tail = Mathf.Lerp(tail, restTail, sm); }
                if (!headFired && time >= Anticip + HeadArriveU * duration)
                {
                    headFired = true;
                    Action ha = _headArrived; _headArrived = null;
                    ha?.Invoke();
                }
                float len = head - tail;
                float p01 = Mathf.Clamp01((time - Anticip) / duration);
                float r0 = Mathf.Lerp(sBox * 0.5f, DockR, p01 * p01);
                float r = r0 * Mathf.Clamp(Mathf.Pow(rest / Mathf.Max(0.05f, len), 0.25f), 0.85f, 1.12f); // bề ngang gần như giữ nguyên, chỉ phình/thon nhẹ khi nén/giãn
                if (settle > 0f) r = Mathf.Lerp(r, DockR, sm);
                float toEnd = Mathf.Max(Mathf.SmoothStep(0.6f, 1f, p01) * 0.8f, sm);
                tube.Build(tail, head, r * 2f, AmmoBox.GridTilt, DockRotation, toEnd);
                if (settle > 0f)
                {
                    float h = DockLen * (_fractionSet ? _dockFraction : 1f);
                    tube.BlendToStraight(target - dockUp * DockBottom, Mathf.Max(0.03f, h), DockRotation, sm);
                }
                Vector3 bodyCenter = tube.PointAt(tail + len * 0.5f);
                transform.position = settle > 0f ? Vector3.Lerp(bodyCenter, target, sm) : bodyCenter;
                if (time >= flyEnd + SettleTail) break;
                yield return null;
                time += dt;
            }
            // giữ thân đã gắn xương làm thanh đậu ở máy xay
            transform.position = target;
            transform.rotation = DockRotation;
            _flying = false;
            if (!_fractionSet) _dockFraction = 1f;
            _move = null;
            _holdingFlight = false;
            if (!headFired) { Action ha = _headArrived; _headArrived = null; ha?.Invoke(); }
            if (_hasPendingMove) { _hasPendingMove = false; _move = StartCoroutine(MoveRoutine(_pendingMoveTarget, _pendingMoveDuration)); }
            done?.Invoke();
            if (_doneAfterHold != null) { Action d = _doneAfterHold; _doneAfterHold = null; d(); }
        }

        public void MoveTo(Vector3 target, float duration)
        {
            if (_flying)   // đang bay: đừng cắt chuyến bay – dời chỗ ngay sau khi ống yên vị
            {
                _hasPendingMove = true; _pendingMoveTarget = target; _pendingMoveDuration = duration;
                return;
            }
            _hasPendingMove = false;
            if (_move != null) StopCoroutine(_move);
            _move = StartCoroutine(MoveRoutine(target, duration));
        }

        IEnumerator MoveRoutine(Vector3 target, float duration)
        {
            Vector3 start = transform.position;
            transform.rotation = DockRotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(duration, 0.01f))
            {
                transform.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            transform.position = target;
            _move = null;
        }

        AmmoFlightTube _body;
        bool _flying;
        float _dockFraction = 1f;

        /// <summary>Bề ngang hộp trong khay.</summary>
        public float BoxWidth => _cell * _config.ammoActiveScale;
        /// <summary>Bề ngang khi đậu = lòng khung máy xay (ConveyorSystem đo từ mesh khung gốc); 0 = dùng BoxWidth.</summary>
        public float DockWidth { get; set; }
        /// <summary>Góc xoay / nghiêng của máy xay tại dock.</summary>
        public Quaternion DockRotation { get; set; } = Quaternion.identity;
        float DockWidthResolved => DockWidth > 0f ? DockWidth : BoxWidth;
        /// <summary>Chiều cao thanh đậu / bề ngang lòng máy xay (GameConfig.dockHeightRatio).</summary>
        public float DockHeightRatio => _config != null ? _config.dockHeightRatio : DefaultDockHeightRatio;
        /// <summary>Đáy thanh đậu nằm dưới tâm 2 lưỡi cắt bao nhiêu lần bề ngang (GameConfig.dockBottomRatio).</summary>
        public float DockBottomRatio => _config != null ? _config.dockBottomRatio : 0.3f;
        /// <summary>Mặc định khi chưa có config (ConveyorLane tính chỗ xếp hàng trước khi Init).</summary>
        public const float DefaultDockHeightRatio = 2.6f;

        TextMeshPro _dockLabel;

        /// <summary>
        /// Hộp đậu trong lòng khung máy xay (tâm = tâm 2 lưỡi cắt, viền khung vẫn lộ 2 bên & dưới); đáy cố định ở
        /// dock − W/2, chiều cao giảm theo số bi còn lại → hộp lún dần vào máy xay (video gốc).
        /// </summary>
        public void ShowDocked(float fraction, int remainingCount = -1)
        {
            _fractionSet = true;
            _dockFraction = Mathf.Clamp01(fraction);
            PoseDocked();
            UpdateDockLabel(remainingCount);
        }

        /// <summary>Tạo sẵn nhãn số bi (tắt) lúc dựng hộp: tạo TextMeshPro ngay lúc ống chạm máy xay gây khựng trên máy yếu.</summary>
        public void EnsureDockLabel()
        {
            if (_dockLabel == null)
            {
                var labelGo = new GameObject("DockCount");
                labelGo.transform.SetParent(transform, false);
                _dockLabel = labelGo.AddComponent<TextMeshPro>();
                if (_config != null && _config.worldFont != null) _dockLabel.font = _config.worldFont;
                _dockLabel.alignment = TextAlignmentOptions.Center;
                float w = DockWidthResolved;
                _dockLabel.fontSize = Mathf.Max(w * 3.5f, 3.2f);
                _dockLabel.color = Color.white;
                _dockLabel.fontStyle = FontStyles.Bold;
                _dockLabel.outlineColor = new Color32(0, 0, 0, 220);
                _dockLabel.outlineWidth = 0.22f;
                _dockLabel.sortingOrder = 105;
            }
        }

        void UpdateDockLabel(int count)
        {
            if (count <= 0)
            {
                if (_dockLabel != null) _dockLabel.gameObject.SetActive(false);
                return;
            }
            EnsureDockLabel();
            _dockLabel.fontSize = Mathf.Max(DockWidthResolved * 3.5f, 3.2f);   // bề ngang máy xay chỉ biết sau khi hộp được gán máng
            _dockLabel.gameObject.SetActive(true);
            _dockLabel.SetText("{0}", count);
            _dockLabel.transform.rotation = DockRotation;
            float dw = DockWidthResolved;
            Vector3 dockUp = DockRotation * Vector3.up;
            Vector3 dockFwd = DockRotation * Vector3.forward;
            _dockLabel.transform.position = LabelAnchor + dockUp * (dw * 0.15f) - dockFwd * 0.08f;
        }

        Vector3 LabelAnchor => _flying ? _flightTarget : transform.position;

        void PoseDocked()
        {
            if (_body == null || _flying) return;
            float w = DockWidthResolved;
            float h = Mathf.Max(0.03f, w * DockHeightRatio * _dockFraction);
            Vector3 dockUp = DockRotation * Vector3.up;
            Vector3 bottom = transform.position - dockUp * (w * DockBottomRatio);
            _body.PoseStraight(bottom, h, w, DockRotation);
            if (_dockLabel != null && _dockLabel.gameObject.activeSelf)
            {
                _dockLabel.transform.rotation = DockRotation;
                Vector3 dockFwd = DockRotation * Vector3.forward;
                _dockLabel.transform.position = transform.position + dockUp * (w * 0.15f) - dockFwd * 0.08f;
            }
        }

        void LateUpdate()
        {
            if (_box.IsLaunched && _body != null && !_flying) PoseDocked(); // bám theo khi xếp hàng chờ / MoveTo
            else if (_flying && _dockLabel != null && _dockLabel.gameObject.activeSelf)
            {
                // ống đang bay: nhãn số bi đứng yên tại máy xay (không trôi theo thân ống)
                float dw = DockWidthResolved;
                _dockLabel.transform.position = _flightTarget + (DockRotation * Vector3.up) * (dw * 0.15f) - (DockRotation * Vector3.forward) * 0.08f;
            }
        }

        void OnDestroy()
        {
            if (_body != null) Destroy(_body.gameObject);
        }
    }
}
