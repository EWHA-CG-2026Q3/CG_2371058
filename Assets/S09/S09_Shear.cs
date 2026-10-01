using UnityEngine;

// 높이(y)에 비례해 x축 방향으로 다이아몬드를 기울이는 4×4 전단 변환
[ExecuteAlways]
[RequireComponent(typeof(DiamondMesh))]
public class S09_Shear : MonoBehaviour
{
    [SerializeField] float k = 1.8f; // (학번 끝자리 8 + 1) / 5
    [SerializeField] bool buildComparison;
    [SerializeField] float comparisonOffset = 5f;
    [SerializeField] Material originalMaterial;
    [SerializeField] Material shearedMaterial;

    DiamondMesh diamondMesh;

    void OnEnable()
    {
        diamondMesh = GetComponent<DiamondMesh>();
        ApplyMaterial(gameObject, shearedMaterial);
        LogTopVertex();
        EnsureComparisonObjects();
    }

    void Update()
    {
        if (diamondMesh == null || diamondMesh.BaseVertices == null) return;

        float[,] shear = ShearMatrixRaw(k);
        Vector3[] baseVertices = diamondMesh.BaseVertices;
        Vector3[] vertices = new Vector3[baseVertices.Length];

        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector4 homogeneous = ToHomogeneous(baseVertices[i]);
            vertices[i] = FromHomogeneous(MultiplyMatrixVectorRaw(shear, homogeneous));
        }

        diamondMesh.SetVertices(vertices);
        EnsureComparisonObjects();
    }

    void OnValidate()
    {
        LogTopVertex();
    }

    // e1, e3, 원점은 그대로이고 e2=(0,1,0)는 (k,1,0)으로 이동한다.
    public float[,] ShearMatrixRaw(float k)
    {
        return new float[,] {
            { 1f,  k, 0f, 0f },
            { 0f, 1f, 0f, 0f },
            { 0f, 0f, 1f, 0f },
            { 0f, 0f, 0f, 1f }
        };
    }

    void LogTopVertex()
    {
        Vector3 top = new Vector3(0.5f, 1f, 0.5f);
        Vector3 result = FromHomogeneous(MultiplyMatrixVectorRaw(ShearMatrixRaw(k), ToHomogeneous(top)));
        Debug.Log($"[S09 Shear] k={k:0.0}, 꼭대기 정점 {top} → {result}", this);
    }

    void EnsureComparisonObjects()
    {
        if (!buildComparison) return;

        transform.position = new Vector3(-comparisonOffset * 0.5f, 0f, 0f);
        ApplyMaterial(gameObject, shearedMaterial);

        EnsureOriginal("OriginalDiamond_k_1_8", transform.position);
        EnsureNegativeShear();
        EnsureOriginal("OriginalDiamond_k_minus_1_8", new Vector3(comparisonOffset * 0.5f, 0f, 0f));
    }

    void EnsureNegativeShear()
    {
        const string objectName = "ShearedDiamond_k_minus_1_8";
        GameObject negative = GameObject.Find(objectName);
        if (negative == null)
        {
            negative = new GameObject(objectName);
            negative.SetActive(false);
            negative.AddComponent<DiamondMesh>();
            S09_Shear negativeShear = negative.AddComponent<S09_Shear>();
            negativeShear.k = -Mathf.Abs(k);
            negativeShear.buildComparison = false;
            negativeShear.originalMaterial = originalMaterial;
            negativeShear.shearedMaterial = shearedMaterial;
            negative.transform.position = new Vector3(comparisonOffset * 0.5f, 0f, 0f);
            ApplyMaterial(negative, shearedMaterial);
            negative.SetActive(true);
        }
        else
        {
            negative.transform.position = new Vector3(comparisonOffset * 0.5f, 0f, 0f);
            S09_Shear negativeShear = negative.GetComponent<S09_Shear>();
            if (negativeShear != null) negativeShear.k = -Mathf.Abs(k);
            ApplyMaterial(negative, shearedMaterial);
        }
    }

    void EnsureOriginal(string objectName, Vector3 position)
    {
        GameObject original = GameObject.Find(objectName);
        if (original == null)
        {
            original = new GameObject(objectName);
            original.SetActive(false);
            original.AddComponent<DiamondMesh>();
            original.transform.position = position;
            ApplyMaterial(original, originalMaterial);
            original.SetActive(true);
        }
        else
        {
            original.transform.position = position;
            ApplyMaterial(original, originalMaterial);
        }
    }

    static void ApplyMaterial(GameObject target, Material material)
    {
        if (material == null) return;
        MeshRenderer renderer = target.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.sharedMaterial = material;
    }

    Vector4 ToHomogeneous(Vector3 vertex)
    {
        return new Vector4(vertex.x, vertex.y, vertex.z, 1f);
    }

    Vector3 FromHomogeneous(Vector4 homogeneous)
    {
        return new Vector3(homogeneous.x, homogeneous.y, homogeneous.z);
    }

    Vector4 MultiplyMatrixVectorRaw(float[,] matrix, Vector4 vector)
    {
        float[] input = { vector.x, vector.y, vector.z, vector.w };
        float[] result = new float[4];

        for (int row = 0; row < 4; row++)
            for (int column = 0; column < 4; column++)
                result[row] += matrix[row, column] * input[column];

        return new Vector4(result[0], result[1], result[2], result[3]);
    }
}