using UnityEngine;

namespace MidProject.Characters
{
    /// <summary>외형 비교 화면에서 두 캐릭터를 사용자의 드래그로만 회전시킨다.</summary>
    public sealed class MidCharacterPairReview : MonoBehaviour
    {
        public Transform[] characters;
        float yaw;
        bool dragging;
        void OnGUI()
        {
            var e=Event.current;
            if(e.type==EventType.MouseDown&&e.button==0){dragging=true;e.Use();}
            else if(e.type==EventType.MouseDrag&&e.button==0&&dragging)
            {yaw-=e.delta.x*360f/Mathf.Max(1,Screen.width);Apply();e.Use();}
            else if(e.type==EventType.MouseUp&&e.button==0)dragging=false;
            else if(e.type==EventType.KeyDown&&e.keyCode==KeyCode.R){yaw=0;Apply();e.Use();}
        }
        void Apply(){foreach(var t in characters)if(t)t.rotation=Quaternion.Euler(0,yaw,0);}
        void OnApplicationFocus(bool focused){if(!focused)dragging=false;}
    }
}
