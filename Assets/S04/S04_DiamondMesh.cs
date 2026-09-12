using UnityEngine;

// 여섯 정점을 위쪽 네 면과 아래쪽 네 면으로 연결한다.
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S04_DiamondMesh : MonoBehaviour
{
    private readonly Vector3[] vertices =
    {
        new Vector3(0f, 0f, 0f),     // 0: S3 정육면체 0번
        new Vector3(1f, 0f, 0f),     // 1: S3 정육면체 1번
        new Vector3(1f, 0f, 1f),     // 2: S3 정육면체 5번
        new Vector3(0f, 0f, 1f),     // 3: S3 정육면체 4번
        new Vector3(0.5f, 1f, 0.5f), // 4: 위 꼭짓점
        new Vector3(0.5f, -1f, 0.5f) // 5: 아래 꼭짓점
    };
    private Mesh generatedMesh;

    void OnEnable()
    {
        generatedMesh = new Mesh { name = "S04 Diamond", hideFlags = HideFlags.DontSave };
        generatedMesh.vertices = vertices;
        // 각 삼각형의 앞면이 입체 바깥쪽을 향하도록 연결한다.
        generatedMesh.triangles = new int[]
        {
            0, 4, 1, // 위쪽: z=0 방향
            1, 4, 2, // 위쪽: x=1 방향
            2, 4, 3, // 위쪽: z=1 방향
            3, 4, 0, // 위쪽: x=0 방향
            0, 1, 5, // 아래쪽: z=0 방향
            1, 2, 5, // 아래쪽: x=1 방향
            2, 3, 5, // 아래쪽: z=1 방향
            3, 0, 5, // 아래쪽: x=0 방향
        };
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = generatedMesh;
    }

    // 선택했을 때 Scene 뷰에서 여섯 정점의 위치를 확인한다.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        foreach (Vector3 vertex in vertices)
            Gizmos.DrawSphere(transform.TransformPoint(vertex), 0.045f);
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
