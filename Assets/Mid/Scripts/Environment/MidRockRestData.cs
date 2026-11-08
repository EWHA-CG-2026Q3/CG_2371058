using UnityEngine;

namespace MidProject.Environment
{
    /// <summary>누적 변형을 피하기 위한 불변 원본. 호출자는 복사본에 행렬을 적용한다.</summary>
    public sealed class MidRockRestData : ScriptableObject
    {
        [SerializeField] Vector3[] originalVertices;
        [SerializeField] int[] originalTriangles;
        public Vector3[] CopyVertices() => (Vector3[])originalVertices.Clone();
        public int[] CopyTriangles() => (int[])originalTriangles.Clone();
        public void Capture(Mesh mesh)
        {
            originalVertices = mesh.vertices;
            originalTriangles = mesh.triangles;
        }
    }
}
