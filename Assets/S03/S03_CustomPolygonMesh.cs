using UnityEngine;

// 다섯 정점을 세 삼각형으로 연결하여 집 모양 오각형을 만든다.
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
            new Vector3(-1f, 0.4f, 0f), // 1: 왼쪽 벽 위
            new Vector3(0f, 1.3f, 0f),  // 2: 지붕 꼭대기
            new Vector3(1f, 0.4f, 0f),  // 3: 오른쪽 벽 위
            new Vector3(1f, -1f, 0f),   // 4: 오른쪽 아래
        };

        // Z축 음수 쪽에 있는 카메라를 향하도록 정점을 연결한다.
        int[] triangles = new int[]
        {
            0, 1, 3, // 벽의 왼쪽 삼각형
            0, 3, 4, // 벽의 오른쪽 삼각형
            1, 2, 3, // 지붕 삼각형
        };

        generatedMesh = new Mesh();
        generatedMesh.name = "S03 House Pentagon";
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
