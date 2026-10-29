using System.Collections.Generic;
using UnityEngine;

namespace GroveSurvivor
{
    // All geometry is native Unity mesh geometry. No browser, web view or external assets.
    public sealed class ForestArt
    {
        readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        readonly List<Mesh> meshes = new List<Mesh>();
        public static Color Gold = new Color(.9f, .74f, .43f);
        public Material Material(Color color)
        {
            if (!materials.TryGetValue(color, out var material))
            {
                var template = Resources.Load<Material>("GroveBase");
                material = template != null ? new Material(template) : new Material(Shader.Find("Standard"));
                material.color = color;
                material.SetFloat("_Glossiness", .05f);
                materials.Add(color, material);
            }
            return material;
        }
        public GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Material(color);
            Object.Destroy(go.GetComponent<Collider>());
            return go;
        }
        public Transform Character(Transform parent, bool hero, bool skeleton)
        {
            var root = new GameObject(hero ? "Swordsman" : skeleton ? "Skeleton Archer" : "Zombie").transform;
            root.SetParent(parent, false);
            Color body = hero ? new Color(.38f, .53f, .54f) : skeleton ? new Color(.65f, .65f, .52f) : new Color(.27f, .37f, .24f);
            Color skin = hero ? new Color(.75f, .63f, .48f) : skeleton ? new Color(.81f, .79f, .64f) : new Color(.45f, .57f, .35f);
            Shape("Torso", PrimitiveType.Cube, root, new Vector3(0, 1.05f, 0), new Vector3(.65f, .75f, .4f), body);
            Shape("Head", PrimitiveType.Sphere, root, new Vector3(0, 1.75f, 0), new Vector3(.52f, .55f, .5f), skin);
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Shape("Leg", PrimitiveType.Cube, root, new Vector3(sign * .19f, .36f, 0), new Vector3(.23f, .65f, .27f), body * .65f);
                Shape("Arm", PrimitiveType.Cube, root, new Vector3(sign * .47f, 1.03f, .04f), new Vector3(.2f, .65f, .22f), skin);
                Shape("Eye", PrimitiveType.Cube, root, new Vector3(sign * .11f, 1.82f, .232f), new Vector3(.07f, .065f, .025f), Color.black);
            }
            if (hero)
            {
                Shape("Helmet", PrimitiveType.Cube, root, new Vector3(0, 1.94f, 0), new Vector3(.56f, .2f, .52f), new Color(.66f, .73f, .7f));
                Shape("Cape", PrimitiveType.Cube, root, new Vector3(0, 1.05f, -.27f), new Vector3(.75f, 1.1f, .12f), new Color(.48f, .21f, .15f));
                Shape("Sword", PrimitiveType.Cube, root, new Vector3(.65f, 1.15f, .6f), new Vector3(.12f, .1f, 1.6f), new Color(.8f, .87f, .85f));
                Shape("Guard", PrimitiveType.Cube, root, new Vector3(.65f, 1.15f, -.05f), new Vector3(.5f, .16f, .13f), Gold);
                Shape("Shield", PrimitiveType.Cylinder, root, new Vector3(-.62f, 1.1f, .1f), new Vector3(.65f, .1f, .65f), new Color(.34f, .44f, .38f)).transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            if (skeleton)
            {
                for (int i = 0; i < 3; i++) Shape("Rib", PrimitiveType.Cube, root, new Vector3(0, .9f + i * .17f, .23f), new Vector3(.55f, .055f, .05f), skin);
                Shape("Bow", PrimitiveType.Cube, root, new Vector3(.67f, 1.05f, .4f), new Vector3(.07f, 1.05f, .1f), Gold);
            }
            return root;
        }
        public Transform CreateForest(Transform parent)
        {
            var root = new GameObject("Forest Environment").transform;
            root.SetParent(parent);
            Shape("Forest Floor", PrimitiveType.Plane, root, Vector3.zero, Vector3.one * 26, new Color(.13f, .22f, .15f));
            var randomState = Random.state;
            Random.InitState(17031);
            for (int i = 0; i < 330; i++)
            {
                Vector3 pos = new Vector3(Random.Range(-88f, 88f), 0, Random.Range(-88f, 88f));
                if (pos.magnitude < 6) continue;
                float size = Random.Range(.7f, 1.5f);
                if (i % 4 == 0)
                {
                    Shape("Mossy Rock", PrimitiveType.Sphere, root, pos + Vector3.up * .3f, new Vector3(1.5f, .9f, 1.2f) * size, new Color(.3f, .37f, .29f));
                    continue;
                }
                Shape("Trunk", PrimitiveType.Cylinder, root, pos + Vector3.up * 1.3f * size, new Vector3(.45f, 1.3f, .45f) * size, new Color(.23f, .22f, .16f));
                for (int k = 0; k < 3; k++)
                {
                    var crown = Shape("Pine Crown", PrimitiveType.Sphere, root, pos + Vector3.up * (2.3f + k * .85f) * size,
                        new Vector3(2.9f - k * .6f, 1.8f, 2.9f - k * .6f) * size, new Color(.1f + k * .018f, .25f + k * .02f, .17f + k * .012f));
                    // Keep tree crowns above the player's silhouette.
                    crown.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                }
            }
            Random.state = randomState;
            return root;
        }
        public Mesh SlashMesh(float radius)
        {
            const int steps = 24;
            var verts = new Vector3[(steps + 1) * 2];
            var tris = new int[steps * 6];
            for (int i = 0; i <= steps; i++)
            {
                float angle = Mathf.Lerp(-75, 75, i / (float)steps) * Mathf.Deg2Rad;
                var v = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                verts[i * 2] = v * (radius - .35f); verts[i * 2 + 1] = v * radius;
                if (i == steps) continue;
                int n = i * 6, a = i * 2;
                tris[n] = a; tris[n + 1] = a + 1; tris[n + 2] = a + 2;
                tris[n + 3] = a + 1; tris[n + 4] = a + 3; tris[n + 5] = a + 2;
            }
            var mesh = new Mesh { vertices = verts, triangles = tris };
            mesh.RecalculateNormals(); meshes.Add(mesh); return mesh;
        }
        public void Dispose()
        {
            foreach (var material in materials.Values) Object.Destroy(material);
            foreach (var mesh in meshes) Object.Destroy(mesh);
        }
    }
}
