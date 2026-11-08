using System;
using UnityEditor;
using UnityEngine;
using MidProject.Characters;

namespace MidProject.EditorTools
{
    // 눈 중심을 고정하고 하이라이트까지 같은 비율로 조절한 뒤 머리 곡면에 다시 투영한다.
    public static class MidCharacterEyeSizing
    {
        public const float EyeScale = .96f;

        public static void Apply(Transform root)
        {
            var head = root.Find("Head");
            for (int side = -1; side <= 1; side += 2)
            {
                string prefix = side < 0 ? "Left" : "Right";
                var center = new Vector2(side * .166f, .710f - MidChiikawaGeometry.HeadBottom);
                Update(head, prefix + "Eye", center, new Vector2(.0685f, .074f) * EyeScale, .0021f);
                Update(head, prefix + "Eye/" + prefix + "Eye_HighlightLarge",
                    center + new Vector2(-.008f, .025f) * EyeScale,
                    new Vector2(.0275f, .0235f) * EyeScale, .0036f);
                Update(head, prefix + "Eye/" + prefix + "Eye_HighlightSmall",
                    center + new Vector2(.001f, -.030f) * EyeScale,
                    new Vector2(.0255f, .012f) * EyeScale, .0036f);
            }
        }

        static void Update(Transform head, string path, Vector2 center, Vector2 radii, float offset)
        {
            var part = head.Find(path);
            var mesh = part.GetComponent<MeshFilter>().sharedMesh;
            string expected = "Assets/Mid/Meshes/" + head.root.name + "/";
            if (!AssetDatabase.GetAssetPath(mesh).StartsWith(expected))
                throw new InvalidOperationException("캐릭터별 독립 눈 메시가 필요합니다: " + path);
            var generated = MidChiikawaGeometry.FaceEllipse(mesh.name, center, radii, offset, out var anchor);
            mesh.Clear();
            mesh.vertices = generated.vertices; mesh.triangles = generated.triangles;
            mesh.uv = generated.uv; mesh.normals = generated.normals; mesh.bounds = generated.bounds;
            UnityEngine.Object.DestroyImmediate(generated);
            EditorUtility.SetDirty(mesh);
            part.position = head.TransformPoint(anchor);
        }

        [MenuItem("Mid/Characters/Update Eyes Only %#e")]
        public static void UpdateBoth()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before updating eyes.");
            foreach (string path in new[] { MidChiikawaBuilder.PrefabPath, MidHachiwareBuilder.PrefabPath })
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try { Apply(root.transform); PrefabUtility.SaveAsPrefabAsset(root, path); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("두 캐릭터 눈 크기 적용 완료: " + EyeScale);
        }
    }
}
