using System.Collections;
using BounceBlast.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BounceBlast.Gameplay
{
    /// <summary>Scene Loading (đứng đầu Build Settings)</summary>
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
            yield return null;

            AsyncOperation op = SceneManager.LoadSceneAsync(target);
            op.allowSceneActivation = false;
            for (float t = 0f; ; t += Time.unscaledDeltaTime)
            {
                float real = Mathf.Clamp01(op.progress / 0.9f);
                float shown = Mathf.Min(real, t / minSeconds);
                if (view != null) view.SetProgress(shown);
                if (shown >= 1f) break;
                yield return null;
            }

            GameObject screen = view != null ? view.transform.root.gameObject : null;
            DontDestroyOnLoad(gameObject);
            if (screen != null && screen != gameObject) DontDestroyOnLoad(screen);

            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;

            if (target == SceneFlow.Gameplay)
                for (float w = 0f; !SceneFlow.LevelReady && w < readyTimeout; w += Time.unscaledDeltaTime) yield return null;
            yield return null;
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
