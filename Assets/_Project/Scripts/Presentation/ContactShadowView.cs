using UnityEngine;

namespace SqueakyLane.Presentation
{
    // Con trực tiếp của root xe/vật. Không Update, không collider, không đụng material instance.
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class ContactShadowView : MonoBehaviour
    {
        private UiTheme theme;
        private Transform owner;
        private Vector2 footprint;
        private Vector2 center;
        private float planeY;

        public static ContactShadowView Attach(Transform root, UiTheme value, Vector2 size, float boardPlaneY, Vector2 localCenter = default)
        {
            if (value == null || value.ContactShadowPrefab == null) { return null; }
            var shadow = Instantiate(value.ContactShadowPrefab, root, false).GetComponent<ContactShadowView>();
            shadow.name = "013 Contact Shadow";
            shadow.owner = root;
            shadow.theme = value;
            shadow.footprint = size;
            shadow.center = localCenter;
            shadow.planeY = boardPlaneY;
            shadow.Project();
            return shadow;
        }

        // CarView.Show gọi sau khi đặt pose: bóng giữ mặt phẳng bãi kể cả lúc thân chúi/nhún/xe xoay.
        public void Project()
        {
            if (owner == null || owner.parent == null || theme == null || Mathf.Abs(owner.localScale.x) < .0001f) { return; }
            var boardPoint = owner.parent.InverseTransformPoint(owner.TransformPoint(new Vector3(center.x, 0, center.y)));
            boardPoint.x += theme.ContactShadowOffset.x;
            boardPoint.z += theme.ContactShadowOffset.y;
            boardPoint.y = planeY + theme.ContactShadowLift;
            transform.localPosition = owner.InverseTransformPoint(owner.parent.TransformPoint(boardPoint));
            transform.localRotation = Quaternion.identity;
            transform.localScale = new Vector3(footprint.x, 1, footprint.y);
        }
    }
}
