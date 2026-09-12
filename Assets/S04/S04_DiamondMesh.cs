using UnityEngine;

// S04 1단계: 정육면체 바닥의 네 점과 위아래 꼭짓점으로 정점을 정의한다.
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
        generatedMesh = new Mesh { name = "S04 Diamond Vertices", hideFlags = HideFlags.DontSave };
        generatedMesh.vertices = vertices;
        // 2단계에서 위쪽 4면과 아래쪽 4면의 인덱스를 작성한다.
        generatedMesh.triangles = new int[0];
        generatedMesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = generatedMesh;
    }

    // 면이 없는 1단계에서도 Scene 뷰에서 여섯 점의 위치를 확인한다.
    void OnDrawGizmos()
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
