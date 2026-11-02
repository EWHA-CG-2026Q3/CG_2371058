using UnityEngine;

// S04에서 만든 다이아몬드의 각 삼각형에 UV를 배정한다.
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S17_DiamondMeshUV : MonoBehaviour
{
    private static readonly Vector3[] Corners =
    {
        new Vector3(0f, 0f, 0f),
        new Vector3(1f, 0f, 0f),
        new Vector3(1f, 0f, 1f),
        new Vector3(0f, 0f, 1f),
        new Vector3(0.5f, 1f, 0.5f),
        new Vector3(0.5f, -1f, 0.5f)
    };

    private static readonly int[] Faces =
    {
        0, 4, 1, 1, 4, 2, 2, 4, 3, 3, 4, 0,
        0, 1, 5, 1, 2, 5, 2, 3, 5, 3, 0, 5
    };

    private Mesh generatedMesh;

    private void OnEnable()
    {
        var vertices = new Vector3[Faces.Length];
        var uv = new Vector2[Faces.Length];
        var triangles = new int[Faces.Length];

        for (int face = 0; face < Faces.Length / 3; face++)
        {
            int start = face * 3;
            for (int corner = 0; corner < 3; corner++)
            {
                vertices[start + corner] = Corners[Faces[start + corner]];
                triangles[start + corner] = start + corner;
            }

            // 면마다 정점을 분리하면 삼각형마다 격자의 같은 칸을 매핑할 수 있다.
            if (face < 4)
            {
                uv[start] = new Vector2(0f, 0f);
                uv[start + 1] = new Vector2(0.5f, 1f);
                uv[start + 2] = new Vector2(1f, 0f);
            }
            else
            {
                uv[start] = new Vector2(0f, 1f);
                uv[start + 1] = new Vector2(1f, 1f);
                uv[start + 2] = new Vector2(0.5f, 0f);
            }
        }

        generatedMesh = new Mesh { name = "S17 Diamond with UV", hideFlags = HideFlags.DontSave };
        generatedMesh.vertices = vertices;
        generatedMesh.uv = uv;
        generatedMesh.triangles = triangles;
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = generatedMesh;
    }

    private void OnDisable()
    {
        if (generatedMesh == null) return;
        GetComponent<MeshFilter>().sharedMesh = null;
        if (Application.isPlaying) Destroy(generatedMesh);
        else DestroyImmediate(generatedMesh);
        generatedMesh = null;
    }
}
