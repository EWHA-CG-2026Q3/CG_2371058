using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S04_PyramidMesh : MonoBehaviour
{
    private Mesh generatedMesh;
    void OnEnable()
    {
        // 밑면 네 모서리(0~3)와 중심 위 꼭짓점(4).
        Vector3[] vertices = {
            new Vector3(0,0,0), new Vector3(1,0,0),
            new Vector3(1,0,1), new Vector3(0,0,1),
            new Vector3(.5f,1,.5f)
        };
        // 모든 면의 앞면이 입체 바깥쪽을 향하도록 순서를 지정한다.
        int[] triangles = {
            0,1,2, 0,2,3, // 사각형 밑면: -Y 방향, 삼각형 2개
            0,4,1, // 앞쪽 옆면
            1,4,2, // 오른쪽 옆면
            2,4,3, // 뒤쪽 옆면
            3,4,0  // 왼쪽 옆면
        };
        generatedMesh = new Mesh { name="S04 Attendance Pyramid", hideFlags=HideFlags.DontSave };
        generatedMesh.vertices=vertices;
        generatedMesh.triangles=triangles;
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh=generatedMesh;
    }
    void OnDisable()
    {
        if(generatedMesh==null) return;
        GetComponent<MeshFilter>().sharedMesh=null;
        if(Application.isPlaying) Destroy(generatedMesh);
        else DestroyImmediate(generatedMesh);
        generatedMesh=null;
    }
}
