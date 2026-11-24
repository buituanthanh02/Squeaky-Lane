using UnityEngine;
using SqueakyLane.Game;

namespace SqueakyLane.Presentation
{
    // Chỉ hiển thị trạng thái được BoardView truyền vào; không đọc Core hay tự tính lượt.
    public sealed class MechanicView : MonoBehaviour
    {
        [SerializeField] private MechanicKind kind;
        [SerializeField, Tooltip("Đèn và vạch đỏ/xanh được bật cùng nhau.")] private GameObject redState, greenState;
        [SerializeField, Tooltip("Tay chắn và nhãn A/B quay; thân trụ giữ yên.")] private Transform movingPart;
        public MechanicKind Kind => kind;
        public Transform MovingPart => movingPart != null ? movingPart : transform;
        public bool HasLightStates => redState != null && greenState != null;
        public void Configure(MechanicKind type, Transform moving = null, GameObject red = null, GameObject green = null)
        {
            kind = type;
            movingPart = moving;
            redState = red;
            greenState = green;
        }
        public void SetRed(bool red)
        {
            if (redState != null) { redState.SetActive(red); }
            if (greenState != null) { greenState.SetActive(!red); }
        }
        public static bool Usable(GameObject prefab, MechanicKind type)
        {
            if (prefab == null) { return false; }
            var view = prefab.GetComponent<MechanicView>();
            return view != null && view.kind == type &&
                (type != MechanicKind.TrafficLight || view.HasLightStates) &&
                (type != MechanicKind.Pylon || view.movingPart != null) &&
                (type != MechanicKind.Surface || prefab.GetComponent<LotSurfaceView>() != null) &&
                prefab.transform.localPosition == Vector3.zero && prefab.transform.localRotation == Quaternion.identity &&
                prefab.transform.localScale == Vector3.one && prefab.GetComponentInChildren<Collider>(true) == null;
        }
    }
}
