using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Rendering;
#endif

namespace SqueakyLane.Presentation
{
    // Chỉ scene AssetLab dùng component này. Không tự chạy trong Play hoặc trong scene chơi.
    [ExecuteAlways]
    public sealed class AssetLabView : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField] private AssetLabConfig config = null;
        [SerializeField] private Light previewLight = null;
        private GameObject generated;
        private AssetLabConfig subscribed;
        private volatile bool pending;
        private void OnEnable()
        {
            EditorApplication.update += EditorTick;
            pending = true;
        }
        private void OnValidate() { pending = true; }
        private void MarkPending() { pending = true; }
        private void EditorTick()
        {
            if (this == null) { EditorApplication.update -= EditorTick; return; }
            if (Application.isPlaying || !isActiveAndEnabled || gameObject.scene != UnityEngine.SceneManagement.SceneManager.GetActiveScene()) { return; }
            if (subscribed != config)
            {
                if (subscribed != null) { subscribed.Changed -= MarkPending; }
                subscribed = config;
                if (subscribed != null) { subscribed.Changed += MarkPending; }
                pending = true;
            }
            if (!pending) { return; }
            pending = false;
            Rebuild();
            SceneView.RepaintAll();
        }
        public void RefreshPreview()
        {
            pending = true;
            EditorTick();
        }
        private void Rebuild()
        {
            Cleanup();
            if (config == null || config.ShadowPrefab == null || config.GroundMaterial == null) { return; }
            generated = new GameObject("Preview (tự sinh; không lưu prefab/scene)");
            generated.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            generated.transform.SetParent(transform, false);
            var floor = new GameObject("Nền kiểm tra", typeof(MeshFilter), typeof(MeshRenderer));
            MarkTransient(floor);
            floor.transform.SetParent(generated.transform, false);
            floor.transform.localScale = new Vector3(config.GroundSize.x, 1, config.GroundSize.y);
            floor.GetComponent<MeshFilter>().sharedMesh = config.ShadowPrefab.GetComponent<MeshFilter>().sharedMesh;
            var renderer = floor.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = config.GroundMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", config.GroundColor);
            renderer.SetPropertyBlock(block);
            if (previewLight != null)
            {
                previewLight.intensity = config.LightIntensity;
                previewLight.transform.rotation = Quaternion.Euler(config.LightAngles);
                previewLight.shadows = LightShadows.None;
            }
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = config.Ambient;
            var source = config.SelectedPrefab;
            if (source == null) { return; }
            var model = Instantiate(source, generated.transform, false);
            model.name = source.name;
            var surface = model.GetComponent<LotSurfaceView>();
            if (surface != null && config.Mechanics != null)
            { surface.Configure(new SqueakyLane.Core.Model.LevelDefBuilder(4, 4).Build(), config.Mechanics.Settings); }
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(config.ModelAngles) * source.transform.localRotation;
            model.transform.localScale = source.transform.localScale * config.ModelScale;
            var bounds = BoundsInParent(model);
            model.transform.localPosition += new Vector3(-bounds.center.x, -bounds.min.y + config.ModelLift, -bounds.center.z);
            MarkTransient(model);
            foreach (var collider in model.GetComponentsInChildren<Collider>(true)) { collider.enabled = false; }
            foreach (var item in model.GetComponentsInChildren<Renderer>(true)) { item.shadowCastingMode = ShadowCastingMode.Off; item.receiveShadows = false; }
            if (!config.ShowShadow) { return; }
            var shadow = Instantiate(config.ShadowPrefab, generated.transform, false);
            shadow.name = "Bóng thử (root asset, ngoài modelRoot)";
            shadow.transform.localPosition = new Vector3(config.ShadowOffset.x, config.ShadowLift, config.ShadowOffset.y);
            shadow.transform.localRotation = Quaternion.identity;
            shadow.transform.localScale = new Vector3(config.ShadowSize.x > 0 ? config.ShadowSize.x : bounds.size.x,
                1, config.ShadowSize.y > 0 ? config.ShadowSize.y : Mathf.Max(bounds.size.z, .2f));
            shadow.transform.SetParent(model.transform, true);
            MarkTransient(shadow);
            block.Clear();
            block.SetColor("_Color", new Color(0, 0, 0, config.ShadowAlpha));
            shadow.GetComponent<MeshRenderer>().SetPropertyBlock(block);
        }
        private static void MarkTransient(GameObject root)
        {
            foreach (var node in root.GetComponentsInChildren<Transform>(true))
            { node.gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild; }
        }
        private static Bounds BoundsInParent(GameObject root)
        {
            var bounds = new Bounds(root.transform.position, Vector3.zero);
            bool found = false;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) { continue; }
                var local = filter.sharedMesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    var p = local.center + Vector3.Scale(local.extents,
                        new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                    p = root.transform.parent.InverseTransformPoint(filter.transform.TransformPoint(p));
                    if (found) { bounds.Encapsulate(p); }
                    else { bounds = new Bounds(p, Vector3.zero); found = true; }
                }
            }
            return bounds;
        }
        private void Cleanup()
        {
            if (generated != null) { DestroyImmediate(generated); generated = null; }
        }
        private void OnDisable()
        {
            EditorApplication.update -= EditorTick;
            if (subscribed != null) { subscribed.Changed -= MarkPending; }
            subscribed = null;
            Cleanup();
        }
#endif
    }
}
