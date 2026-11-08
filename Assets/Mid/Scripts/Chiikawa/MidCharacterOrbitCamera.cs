using UnityEngine;

namespace MidProject.Characters
{
    /// <summary>외형 검토용 카메라. 캐릭터의 Transform과 메시에는 손대지 않는다.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class MidCharacterOrbitCamera : MonoBehaviour
    {
        public Vector3 target = new Vector3(0, .60f, 0);
        public float distance = 3f;
        public float defaultSize = .70f;
        float yaw, pitch;
        bool dragging;
        Camera view;

        void OnEnable()
        {
            view = GetComponent<Camera>();
            ResetView();
        }

        // Game 뷰의 GUI 이벤트로 짧은 드래그도 빠뜨리지 않고 처리한다.
        void OnGUI()
        {
            Event input = Event.current;
            if (input.type == EventType.MouseDown && input.button == 0)
            {
                dragging = true;
                input.Use();
            }
            else if (input.type == EventType.MouseDrag && input.button == 0 && dragging)
            {
                yaw -= input.delta.x * 360f / Mathf.Max(Screen.width, 1);
                pitch = Mathf.Clamp(pitch + input.delta.y * 180f / Mathf.Max(Screen.height, 1), -85f, 85f);
                input.Use();
            }
            else if (input.type == EventType.MouseUp && input.button == 0) dragging = false;
            else if (input.type == EventType.ScrollWheel)
            {
                view.orthographicSize = Mathf.Clamp(view.orthographicSize * Mathf.Exp(input.delta.y * .04f), .40f, 1.20f);
                input.Use();
            }
            else if (input.type == EventType.KeyDown && input.keyCode == KeyCode.R)
            { ResetView(); input.Use(); }
            ApplyView();
        }

        void OnApplicationFocus(bool focused) { if (!focused) dragging = false; }

        public void ResetView()
        {
            yaw = pitch = 0;
            if (!view) view = GetComponent<Camera>();
            view.orthographicSize = defaultSize;
            ApplyView();
        }

        void ApplyView()
        {
            yaw = Mathf.Repeat(yaw, 360f);
            transform.position = target + Quaternion.Euler(pitch, yaw, 0) * new Vector3(0, 0, -distance);
            transform.LookAt(target);
        }
    }
}
