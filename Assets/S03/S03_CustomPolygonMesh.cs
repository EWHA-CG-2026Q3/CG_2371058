using UnityEngine;

// 1단계: 세 정점과 하나의 삼각형으로 기본 메시를 만든다.
// 편집 모드에서도 Scene 뷰와 Game 뷰에서 형태를 확인한다.
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S03_CustomPolygonMesh : MonoBehaviour
{
    private Mesh generatedMesh;

    void OnEnable()
    {
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-1f, -1f, 0f), // 0: 왼쪽 아래
            new Vector3(0f, 1f, 0f),   // 1: 위
            new Vector3(1f, -1f, 0f),  // 2: 오른쪽 아래
        };

        // Z축 음수 쪽에 있는 카메라를 향하도록 정점을 연결한다.
        int[] triangles = new int[] { 0, 1, 2 };

        generatedMesh = new Mesh();
        generatedMesh.name = "S03 Triangle";
        generatedMesh.hideFlags = HideFlags.DontSave;
        generatedMesh.vertices = vertices;
        generatedMesh.triangles = triangles;
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = generatedMesh;
    }

    void OnDisable()
    {
        if (generatedMesh == null) return;
        GetComponent<MeshFilter>().sharedMesh = null;
        if (Application.isPlaying) Destroy(generatedMesh);
        else DestroyImmediate(generatedMesh);
        generatedMesh = null;
    }
}
