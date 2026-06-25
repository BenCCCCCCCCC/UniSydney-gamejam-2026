using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FairyTale.Core
{
    /// <summary>
    /// 场景转场管理器（全局单例，跨场景存活）。
    /// TransitionTo      : 淡黑转场。
    /// PanToNextScene    : 摄像机平移转场，并把 StoryActor 标签对象携带到新场景。
    /// </summary>
    public class SceneTransitionManager : MonoBehaviour
    {
        public static SceneTransitionManager Instance { get; private set; }

        [Tooltip("淡黑转场的淡入/淡出时长（秒）")]
        public float fadeDuration = 0.35f;

        private CanvasGroup _fade;
        private bool _busy;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildFadeCanvas();
        }

        // ─── 公开接口 ──────────────────────────────────────────────────────────

        /// <summary>淡黑转场到指定场景</summary>
        public void TransitionTo(string sceneName)
        {
            if (_busy) return;
            StartCoroutine(TransitionRoutine(sceneName));
        }

        /// <summary>
        /// 摄像机平移转场：背景视觉连续，同时把 StoryActor 标签对象携带到新场景。
        /// panDuration：平移动画时长（秒）。
        /// </summary>
        public void PanToNextScene(string nextSceneName, float panDuration = 0.8f)
        {
            if (_busy) return;
            StartCoroutine(PanTransitionRoutine(nextSceneName, panDuration));
        }

        // ─── 内部协程 ──────────────────────────────────────────────────────────

        private IEnumerator TransitionRoutine(string sceneName)
        {
            _busy = true;
            _fade.blocksRaycasts = true;
            yield return Fade(0f, 1f);
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (!op.isDone) yield return null;
            yield return null;
            yield return Fade(1f, 0f);
            _fade.blocksRaycasts = false;
            _busy = false;
        }

        private IEnumerator PanTransitionRoutine(string nextSceneName, float duration)
        {
            _busy = true;
            _fade.blocksRaycasts = true;

            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("PanToNextScene: 找不到 Main Camera，回退淡黑转场。");
                yield return TransitionRoutine(nextSceneName);
                yield break;
            }

            // ── 步骤 0：把 StoryActor 脱离旧场景层级，设为 DontDestroyOnLoad ──
            // 这样无论旧场景何时被卸载，actor 都能存活，不会产生残留触发。
            GameObject[] actors = GameObject.FindGameObjectsWithTag("StoryActor");
            foreach (var a in actors)
            {
                a.transform.SetParent(null);   // 必须是根对象才能 DontDestroyOnLoad
                DontDestroyOnLoad(a);
            }

            string fromSceneName = SceneManager.GetActiveScene().name;
            Vector3 camStart = cam.transform.position;
            float screenWidthWorld = cam.orthographicSize * 2f * cam.aspect;
            Vector3 camEnd = camStart + Vector3.right * screenWidthWorld;

            // ── 步骤 1：Additive 加载新场景（两场景同时在内存里，摄像机平移期间都可见）──
            var op = SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Additive);
            while (!op.isDone) yield return null;

            Scene nextScene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
            if (!nextScene.IsValid())
            {
                Debug.LogError($"PanToNextScene: 找不到有效场景 '{nextSceneName}'，中止。");
                _fade.blocksRaycasts = false;
                _busy = false;
                yield break;
            }

            // ── 步骤 2：新场景偏移到右侧一屏；禁用其摄像机和 AudioListener；删除重复 actor ──
            foreach (var go in nextScene.GetRootGameObjects())
            {
                go.transform.position += Vector3.right * screenWidthWorld;

                var nc = go.GetComponentInChildren<Camera>(true);
                if (nc != null)
                {
                    nc.enabled = false;
                    var al = nc.GetComponent<AudioListener>();
                    if (al != null) al.enabled = false;
                }

                // 若我们携带了 actor，删掉新场景里同类占位（防止出现两个猎人）
                if (actors.Length > 0)
                {
                    foreach (var dup in go.GetComponentsInChildren<StoryActorAutoMove>(true))
                        Destroy(dup.gameObject);
                }
            }

            yield return null; // 等新场景 Start() 执行完毕

            // ── 步骤 3：平滑平移主摄像机 ──
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                cam.transform.position = Vector3.Lerp(camStart, camEnd,
                    Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            cam.transform.position = camEnd;

            // ── 步骤 4：摄像机移入新场景（否则旧场景卸载时会一并销毁它）──
            SceneManager.MoveGameObjectToScene(cam.gameObject, nextScene);

            // ── 步骤 5：新场景所有根对象（含摄像机）复位到世界原点（同一帧，无视觉跳帧）──
            foreach (var go in nextScene.GetRootGameObjects())
                go.transform.position -= Vector3.right * screenWidthWorld;

            // ── 步骤 6：激活新场景，卸载旧场景 ──
            // DontDestroyOnLoad 的 actor 不受 Unload 影响，旧场景其他逻辑全部销毁。
            SceneManager.SetActiveScene(nextScene);
            var unload = SceneManager.UnloadSceneAsync(fromSceneName);
            if (unload != null)
                while (!unload.isDone) yield return null;
            else
                Debug.LogWarning($"PanToNextScene: 无法卸载场景 '{fromSceneName}'。");

            // ── 步骤 7：把携带的 actor 移入新场景，更新路径引用 ──
            Transform newStart = FindNamedTransform(nextScene, "ActorStart");
            Transform newEnd   = FindNamedTransform(nextScene, "ActorEnd");

            foreach (var a in actors)
            {
                if (a == null) continue;
                SceneManager.MoveGameObjectToScene(a, nextScene);

                var mover = a.GetComponent<StoryActorAutoMove>();
                if (mover == null) continue;
                if (newStart != null) mover.startPoint = newStart;
                if (newEnd   != null) mover.endPoint   = newEnd;
            }

            _fade.blocksRaycasts = false;

            // ── 步骤 8：短暂停顿后让 actor 从新场景左侧走入 ──
            yield return new WaitForSeconds(0.3f);
            foreach (var a in actors)
            {
                if (a == null) continue;
                var mover = a.GetComponent<StoryActorAutoMove>();
                if (mover != null) mover.StartPlay();
            }

            _busy = false;  // 放在最后，防止 StartPlay 触发的逻辑重入
        }

        // ─── 工具方法 ──────────────────────────────────────────────────────────

        private static Transform FindNamedTransform(Scene scene, string namePart)
        {
            foreach (var go in scene.GetRootGameObjects())
                foreach (Transform tf in go.GetComponentsInChildren<Transform>(true))
                    if (tf.name.Contains(namePart)) return tf;
            return null;
        }

        private IEnumerator Fade(float from, float to)
        {
            float t = 0f;
            _fade.alpha = from;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                _fade.alpha = Mathf.Lerp(from, to, t / fadeDuration);
                yield return null;
            }
            _fade.alpha = to;
        }

        private void BuildFadeCanvas()
        {
            var canvasGO = new GameObject("TransitionCanvas");
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            canvasGO.AddComponent<GraphicRaycaster>();
            _fade = canvasGO.AddComponent<CanvasGroup>();
            _fade.alpha = 0f;
            _fade.blocksRaycasts = false;
            var imgGO = new GameObject("Black");
            imgGO.transform.SetParent(canvasGO.transform, false);
            var img = imgGO.AddComponent<Image>();
            img.color = Color.black;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
