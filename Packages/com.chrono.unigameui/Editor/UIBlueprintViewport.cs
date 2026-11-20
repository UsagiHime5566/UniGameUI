using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniGameUI.Editor
{
    /// <summary>
    /// 藍圖畫布的視角：平移與縮放，以及兩種座標之間的換算。
    /// 「畫布座標」是節點儲存的座標，不受視角影響；「視窗座標」是畫布區域裡的像素，左上角是 (0, 0)。
    /// 這裡只有計算，沒有任何介面，所以可以直接測試。
    /// </summary>
    [Serializable]
    internal struct UIBlueprintViewport
    {
        public const float MinZoom = 0.25f;
        public const float MaxZoom = 2f;

        // 畫布原點在視窗裡的位置。
        public Vector2 pan;
        public float zoom;

        public UIBlueprintViewport(Vector2 pan, float zoom)
        {
            this.pan = pan;
            this.zoom = Mathf.Clamp(zoom, MinZoom, MaxZoom);
        }

        public static UIBlueprintViewport Default => new UIBlueprintViewport(Vector2.zero, 1f);

        /// <summary>縮放比例。還沒設定過（0）時當成 100%。</summary>
        public float Zoom => zoom <= 0f ? 1f : zoom;

        public Vector2 CanvasToWindow(Vector2 canvasPoint)
        {
            return canvasPoint * Zoom + pan;
        }

        public Vector2 WindowToCanvas(Vector2 windowPoint)
        {
            return (windowPoint - pan) / Zoom;
        }

        public Rect CanvasToWindow(Rect canvasRect)
        {
            return new Rect(CanvasToWindow(canvasRect.position), canvasRect.size * Zoom);
        }

        /// <summary>滑鼠在視窗裡移動的距離，換算成畫布上的距離。</summary>
        public Vector2 WindowToCanvasDelta(Vector2 windowDelta)
        {
            return windowDelta / Zoom;
        }

        public UIBlueprintViewport Panned(Vector2 windowDelta)
        {
            return new UIBlueprintViewport(pan + windowDelta, Zoom);
        }

        /// <summary>
        /// 以視窗裡的一點為中心縮放。縮放之後，那一點底下的畫布座標不變。比例會夾在允許的範圍內。
        /// </summary>
        public UIBlueprintViewport ZoomedAround(Vector2 windowPoint, float newZoom)
        {
            var anchor = WindowToCanvas(windowPoint);
            var clamped = Mathf.Clamp(newZoom, MinZoom, MaxZoom);
            return new UIBlueprintViewport(windowPoint - anchor * clamped, clamped);
        }

        /// <summary>滾輪轉動量對應的縮放倍率：往上是放大，一格大約 10%。</summary>
        public static float WheelFactor(float wheelDelta)
        {
            return Mathf.Pow(1.1f, -Mathf.Clamp(wheelDelta, -9f, 9f) / 3f);
        }

        /// <summary>
        /// 讓畫布上的一塊範圍完整出現在視窗裡並置中。只縮小、不放大超過 100%；
        /// 範圍大到縮到最小也放不下時，以最小的比例置中。
        /// </summary>
        public static UIBlueprintViewport Framing(Rect canvasBounds, Vector2 viewSize, float padding)
        {
            var available = new Vector2(
                Mathf.Max(1f, viewSize.x - padding * 2f), Mathf.Max(1f, viewSize.y - padding * 2f));
            var fit = Mathf.Min(
                available.x / Mathf.Max(1f, canvasBounds.width), available.y / Mathf.Max(1f, canvasBounds.height));
            var zoom = Mathf.Clamp(fit, MinZoom, 1f);
            return new UIBlueprintViewport(viewSize * 0.5f - canvasBounds.center * zoom, zoom);
        }
    }

    /// <summary>
    /// 藍圖視窗在這個 Editor 工作階段內要記住的東西：每個 Scene 的視角、右欄的寬度、區塊的收合狀態。
    /// 存在 SessionState，重新整理視窗、進出 Play Mode、重新載入 Domain 之後都還在，但不會寫進 Scene 或專案。
    /// </summary>
    internal static class UIBlueprintSession
    {
        private const string ViewPrefix = "UniGameUI.Blueprint.View.";

        /// <summary>用來記住視角的鍵值：存過檔的 Scene 用 GUID，還沒存檔的共用一個。</summary>
        public static string SceneKey(Scene scene)
        {
            if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
                return "untitled";
            var guid = AssetDatabase.AssetPathToGUID(scene.path);
            return string.IsNullOrEmpty(guid) ? scene.path : guid;
        }

        public static bool TryLoadView(string sceneKey, out UIBlueprintViewport view)
        {
            var json = SessionState.GetString(ViewPrefix + sceneKey, "");
            if (json.Length == 0)
            {
                view = UIBlueprintViewport.Default;
                return false;
            }
            var loaded = JsonUtility.FromJson<UIBlueprintViewport>(json);
            view = new UIBlueprintViewport(loaded.pan, loaded.Zoom);
            return true;
        }

        public static void SaveView(string sceneKey, UIBlueprintViewport view)
        {
            SessionState.SetString(ViewPrefix + sceneKey, JsonUtility.ToJson(view));
        }

        public static void ForgetView(string sceneKey)
        {
            SessionState.EraseString(ViewPrefix + sceneKey);
        }
    }
}
