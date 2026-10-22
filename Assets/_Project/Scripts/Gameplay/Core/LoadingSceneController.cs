using System.Collections;
using BounceBlast.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Scene Loading (đứng đầu Build Settings): hiện màn loading gốc rồi nạp nền scene đích.
    /// Mở app → nạp MenuGame; <see cref="SceneFlow.GoToGameplay"/> đi qua đây → nạp Gameplay.
    /// Thanh tiến độ = min(tiến độ nạp thật, thời gian tối thiểu) để không chớp qua quá nhanh.
    /// Màn loading được giữ lại qua lúc chuyển scene (DontDestroyOnLoad) và chỉ mờ đi khi scene đích đã sẵn sàng
    /// (Gameplay: level đầu đã dựng xong – <see cref="SceneFlow.LevelReady"/>) → không lộ khung bàn trống / cú khựng dựng level.
    /// </summary>
    public class LoadingSceneController : MonoBehaviour
    {
        [SerializeField] LoadingScreenView view;
        [Tooltip("Giây tối thiểu của màn loading khi mới mở app")] [SerializeField] float bootSeconds = 2f;
        [Tooltip("Giây tối thiểu của màn loading khi vào màn chơi")] [SerializeField] float levelSeconds = 1f;
        [Tooltip("Chờ Gameplay dựng level tối đa bao giây (phòng khi không có ai báo)")] [SerializeField] float readyTimeout = 5f;
        [Tooltip("Giây mờ dần màn loading sau khi scene đích sẵn sàng")] [SerializeField] float fadeSeconds = 0.25f;

        IEnumerator Start()
        {
            string target = SceneFlow.TakePending();
            bool boot = target == null;
            if (boot) target = SceneFlow.Menu;
            float minSeconds = Mathf.Max(0.01f, boot ? bootSeconds : levelSeconds);

            if (view != null) view.SetProgress(0f);
            yield return null;   // vẽ màn loading trước khi bắt đầu nạp

            AsyncOperation op = SceneManager.LoadSceneAsync(target);
            op.allowSceneActivation = false;
            for (float t = 0f; ; t += Time.unscaledDeltaTime)
            {
                float real = Mathf.Clamp01(op.progress / 0.9f);   // Unity dừng ở 0.9 khi chờ kích hoạt
                float shown = Mathf.Min(real, t / minSeconds);
                if (view != null) view.SetProgress(shown);
                if (shown >= 1f) break;
                yield return null;
            }

            // giữ màn loading sống qua lúc đổi scene
            GameObject screen = view != null ? view.transform.root.gameObject : null;
            DontDestroyOnLoad(gameObject);
            if (screen != null && screen != gameObject) DontDestroyOnLoad(screen);

            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;

            if (target == SceneFlow.Gameplay)
                for (float w = 0f; !SceneFlow.LevelReady && w < readyTimeout; w += Time.unscaledDeltaTime) yield return null;
            yield return null;   // scene đích vẽ xong ít nhất một khung phía sau màn loading
            yield return null;

            CanvasGroup group = screen != null ? screen.GetComponent<CanvasGroup>() : null;
            if (group != null)
            {
                group.blocksRaycasts = false;
                for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
                {
                    group.alpha = 1f - t / fadeSeconds;
                    yield return null;
                }
            }
            if (screen != null && screen != gameObject) Destroy(screen);
            Destroy(gameObject);
        }
    }
}
