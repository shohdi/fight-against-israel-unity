using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FightAgainstIsrael
{
    public static class CombatVisuals
    {
        private static Mesh sphere, capsule;

        public static GameObject Create(string name, bool tracer)
        {
            if (sphere == null) sphere = BuildMesh(false);
            if (capsule == null) capsule = BuildMesh(true);
            var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            obj.GetComponent<MeshFilter>().sharedMesh = tracer ? capsule : sphere;
            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return obj;
        }

        private static Mesh BuildMesh(bool elongated)
        {
            const int longitude = 12;
            int rings = elongated ? 18 : 13;
            var vertices = new List<Vector3>(rings * (longitude + 1));
            var triangles = new List<int>();
            for (int ring = 0; ring < rings; ring++)
            {
                float angle = elongated ? (ring < 9 ? ring / 8f * Mathf.PI * 0.5f : Mathf.PI * 0.5f + (ring - 9) / 8f * Mathf.PI * 0.5f) : ring / 12f * Mathf.PI;
                float y = Mathf.Cos(angle) * 0.5f + (elongated ? (ring < 9 ? 0.5f : -0.5f) : 0);
                float radius = Mathf.Sin(angle) * 0.5f;
                for (int segment = 0; segment <= longitude; segment++)
                {
                    float rotation = segment / (float)longitude * Mathf.PI * 2;
                    vertices.Add(new Vector3(Mathf.Cos(rotation) * radius, y, Mathf.Sin(rotation) * radius));
                    if (ring == 0 || segment == longitude) continue;
                    int current = ring * (longitude + 1) + segment;
                    int previous = current - longitude - 1;
                    triangles.Add(previous); triangles.Add(current + 1); triangles.Add(current);
                    triangles.Add(previous); triangles.Add(previous + 1); triangles.Add(current + 1);
                }
            }
            var mesh = new Mesh { name = elongated ? "Original pooled tracer mesh" : "Original pooled impact mesh" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
