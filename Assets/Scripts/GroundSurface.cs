using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Flat low-poly yard. The walkable surface stays on y = 0 in Ground local space,
/// matching the existing plane collider. Dirt and grass share that surface.
/// </summary>
public static class GroundSurface
{
    public const int Cells = 8;
    const float PlaneHalf = 5f;
    const float Skirt = 0.22f;

    public struct KeepOut
    {
        public Vector2 center;
        public float radius;
    }

    public struct Settings
    {
        public int seed;
        public Vector2 clearingCenter;
        public Vector2 clearingSize;
        public float pathWidth;
        public float pathAngle;
        public Vector3 origin;
        public Vector3 scale;
    }

    public static Settings Read(GroundLook look, Transform ground)
    {
        Settings settings = Default(ground);
        if (look == null)
            return settings;

        settings.seed = look.seed;
        settings.clearingCenter = look.clearingCenter;
        settings.clearingSize = new Vector2(Mathf.Max(2f, look.clearingSize.x), Mathf.Max(2f, look.clearingSize.y));
        settings.pathWidth = Mathf.Max(1f, look.pathWidth);
        settings.pathAngle = look.pathAngle;
        return settings;
    }

    public static Settings Default(Transform ground)
    {
        return new Settings
        {
            seed = 17,
            clearingCenter = new Vector2(0.5f, 2.5f),
            clearingSize = new Vector2(15f, 13f),
            pathWidth = 5f,
            pathAngle = 4f,
            origin = ground != null ? ground.position : Vector3.zero,
            scale = ground != null ? ground.lossyScale : Vector3.one
        };
    }

    public static void FillSurface(Mesh mesh, Settings settings)
    {
        int n = Cells;
        int side = n + 1;
        var verts = new List<Vector3>(side * side + side * 8);
        for (int j = 0; j < side; j++)
        {
            for (int i = 0; i < side; i++)
            {
                float x = Mathf.Lerp(-PlaneHalf, PlaneHalf, i / (float)n);
                float z = Mathf.Lerp(-PlaneHalf, PlaneHalf, j / (float)n);
                bool edge = i == 0 || j == 0 || i == n || j == n;
                if (!edge)
                {
                    float cell = PlaneHalf * 2f / n;
                    x += (Hash01(i, j, settings.seed) - 0.5f) * cell * 0.42f;
                    z += (Hash01(i + 40, j + 9, settings.seed + 3) - 0.5f) * cell * 0.42f;
                }

                verts.Add(new Vector3(x, 0f, z));
            }
        }

        var grass = new List<int>();
        var grassLight = new List<int>();
        var grassDark = new List<int>();
        var dirt = new List<int>();
        var dirtLight = new List<int>();

        for (int j = 0; j < n; j++)
        {
            for (int i = 0; i < n; i++)
            {
                int v00 = j * side + i;
                int v10 = v00 + 1;
                int v01 = v00 + side;
                int v11 = v01 + 1;
                bool flip = Hash01(i + 3, j + 11, settings.seed + 19) > 0.5f;
                if (flip)
                {
                    AddTri(settings, verts, v00, v11, v10, grass, grassLight, grassDark, dirt, dirtLight);
                    AddTri(settings, verts, v00, v01, v11, grass, grassLight, grassDark, dirt, dirtLight);
                }
                else
                {
                    AddTri(settings, verts, v00, v01, v10, grass, grassLight, grassDark, dirt, dirtLight);
                    AddTri(settings, verts, v10, v01, v11, grass, grassLight, grassDark, dirt, dirtLight);
                }
            }
        }

        AddSkirt(verts, dirt, n);

        mesh.Clear();
        mesh.name = "GroundSurface";
        mesh.SetVertices(verts);
        mesh.subMeshCount = 5;
        mesh.SetTriangles(grass, 0);
        mesh.SetTriangles(grassLight, 1);
        mesh.SetTriangles(grassDark, 2);
        mesh.SetTriangles(dirt, 3);
        mesh.SetTriangles(dirtLight, 4);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    public static void FillTufts(Mesh mesh, Settings settings, KeepOut[] keepOuts)
    {
        FillProps(mesh, settings, keepOuts, true);
    }

    public static void FillRocks(Mesh mesh, Settings settings, KeepOut[] keepOuts)
    {
        FillProps(mesh, settings, keepOuts, false);
    }

    static void FillProps(Mesh mesh, Settings settings, KeepOut[] keepOuts, bool tufts)
    {
        var verts = new List<Vector3>(256);
        var dark = new List<int>();
        var light = new List<int>();
        int borderTarget = tufts ? 10 : 6;
        int cornerTarget = tufts ? 4 : 4;
        int border = 0;
        int corner = 0;
        var rng = new System.Random(settings.seed + (tufts ? 101 : 307));

        for (int attempt = 0; attempt < 500 && (border < borderTarget || corner < cornerTarget); attempt++)
        {
            Vector2 world = RandomPoint(rng, settings);
            bool blocked = Blocked(world, keepOuts) || IsDirt(world, settings);
            if (blocked)
                continue;

            bool onBorder = EdgeDistance(world, settings) <= 2.3f;
            bool onCorner = MapEdgeDistance(world, settings) <= 2.4f;
            if (onBorder && border < borderTarget)
                border++;
            else if (onCorner && corner < cornerTarget)
                corner++;
            else
                continue;

            Vector3 at = ToDecoration(world, settings);
            float yaw = (float)rng.NextDouble() * 360f;
            if (tufts)
            {
                int blades = 3 + rng.Next(3);
                bool pale = rng.Next(2) == 0;
                AddTuft(verts, pale ? light : dark, at, yaw, blades, rng);
            }
            else
            {
                float scale = onCorner ? 0.22f + (float)rng.NextDouble() * 0.2f : 0.12f + (float)rng.NextDouble() * 0.12f;
                AddRock(verts, dark, at, yaw, scale, rng);
            }
        }

        mesh.Clear();
        mesh.name = tufts ? "GroundTufts" : "GroundRocks";
        mesh.SetVertices(verts);
        if (tufts)
        {
            mesh.subMeshCount = 2;
            mesh.SetTriangles(dark, 0);
            mesh.SetTriangles(light, 1);
        }
        else
        {
            mesh.subMeshCount = 1;
            mesh.SetTriangles(dark, 0);
        }

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    public static bool IsDirt(Vector2 world, Settings settings)
    {
        Vector2 delta = world - settings.clearingCenter;
        float radians = settings.pathAngle * Mathf.Deg2Rad;
        Vector2 along = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
        Vector2 side = new Vector2(along.y, -along.x);
        float lateral = Mathf.Abs(Vector2.Dot(delta, side));
        float wobble = 1f + Noise(world.x, world.y, settings.seed) * 0.22f;
        if (lateral < settings.pathWidth * 0.5f * wobble)
            return true;

        float hx = Mathf.Max(0.5f, settings.clearingSize.x * 0.5f);
        float hz = Mathf.Max(0.5f, settings.clearingSize.y * 0.5f);
        float nx = delta.x / hx;
        float nz = delta.y / hz;
        float edge = 1f + Noise(world.x + 12f, world.y - 4f, settings.seed + 5) * 0.18f;
        return nx * nx + nz * nz < edge;
    }

    static void AddTri(Settings settings, List<Vector3> verts, int a, int b, int c, List<int> grass, List<int> grassLight, List<int> grassDark, List<int> dirt, List<int> dirtLight)
    {
        Vector3 mid = (verts[a] + verts[b] + verts[c]) / 3f;
        Vector2 world = WorldOf(mid, settings);
        int bucket = HashInt(Mathf.FloorToInt(world.x / 3.6f), Mathf.FloorToInt(world.y / 3.6f), settings.seed);
        List<int> target;
        if (IsDirt(world, settings))
            target = bucket % 2 == 0 ? dirt : dirtLight;
        else if (bucket % 3 == 1)
            target = grassLight;
        else if (bucket % 3 == 2)
            target = grassDark;
        else
            target = grass;

        target.Add(a);
        target.Add(b);
        target.Add(c);
    }

    static void AddSkirt(List<Vector3> verts, List<int> dirt, int n)
    {
        int side = n + 1;
        int top = verts.Count;
        for (int i = 0; i < side; i++)
        {
            verts.Add(verts[i] + Vector3.down * Skirt);
            verts.Add(verts[n * side + i] + Vector3.down * Skirt);
            verts.Add(verts[i * side] + Vector3.down * Skirt);
            verts.Add(verts[i * side + n] + Vector3.down * Skirt);
        }

        for (int i = 0; i < n; i++)
        {
            int botZ0 = top + i * 4;
            int botZ1 = top + (i + 1) * 4;
            Tri(dirt, i, i + 1, botZ1);
            Tri(dirt, i, botZ1, botZ0);

            int far0 = n * side + i;
            int far1 = far0 + 1;
            int botFar0 = top + i * 4 + 1;
            int botFar1 = top + (i + 1) * 4 + 1;
            Tri(dirt, far0, botFar0, botFar1);
            Tri(dirt, far0, botFar1, far1);

            int left0 = i * side;
            int left1 = (i + 1) * side;
            int botLeft0 = top + i * 4 + 2;
            int botLeft1 = top + (i + 1) * 4 + 2;
            Tri(dirt, left0, botLeft0, left1);
            Tri(dirt, botLeft0, botLeft1, left1);

            int right0 = i * side + n;
            int right1 = (i + 1) * side + n;
            int botRight0 = top + i * 4 + 3;
            int botRight1 = top + (i + 1) * 4 + 3;
            Tri(dirt, right0, right1, botRight0);
            Tri(dirt, botRight0, right1, botRight1);
        }
    }

    static void Tri(List<int> tris, int a, int b, int c)
    {
        tris.Add(a);
        tris.Add(b);
        tris.Add(c);
    }

    static void AddTuft(List<Vector3> verts, List<int> tris, Vector3 at, float yaw, int blades, System.Random rng)
    {
        for (int i = 0; i < blades; i++)
        {
            float angle = yaw + i * (68f + (float)rng.NextDouble() * 18f);
            float lean = 12f + (float)rng.NextDouble() * 16f;
            float height = 0.18f + (float)rng.NextDouble() * 0.16f;
            float width = 0.035f + (float)rng.NextDouble() * 0.02f;
            Quaternion rotation = Quaternion.Euler(lean, angle, 0f);
            Vector3 right = rotation * Vector3.right * width;
            Vector3 up = rotation * Vector3.up * height;
            int start = verts.Count;
            verts.Add(at - right);
            verts.Add(at + right);
            verts.Add(at + up);
            verts.Add(at - right);
            verts.Add(at + right);
            verts.Add(at + up);
            tris.Add(start);
            tris.Add(start + 1);
            tris.Add(start + 2);
            tris.Add(start + 3);
            tris.Add(start + 5);
            tris.Add(start + 4);
        }
    }

    static void AddRock(List<Vector3> verts, List<int> tris, Vector3 at, float yaw, float scale, System.Random rng)
    {
        Quaternion rotation = Quaternion.Euler((float)rng.NextDouble() * 12f - 4f, yaw, (float)rng.NextDouble() * 12f - 4f);
        Vector3 stretch = new Vector3(0.8f + (float)rng.NextDouble() * 0.5f, 0.55f + (float)rng.NextDouble() * 0.3f, 0.75f + (float)rng.NextDouble() * 0.45f);
        Vector3[] ring =
        {
            new Vector3(0.55f, 0.02f, 0.08f),
            new Vector3(0.12f, 0.05f, 0.5f),
            new Vector3(-0.48f, 0f, 0.16f),
            new Vector3(-0.18f, 0.04f, -0.46f),
            new Vector3(0.28f, -0.02f, -0.32f)
        };
        Vector3 top = new Vector3(0.04f, 0.42f, 0.02f);
        Vector3 bottom = new Vector3(0f, -0.28f, 0f);
        int start = verts.Count;
        for (int i = 0; i < ring.Length; i++)
            verts.Add(at + rotation * Vector3.Scale(ring[i], stretch) * scale);
        verts.Add(at + rotation * Vector3.Scale(top, stretch) * scale);
        verts.Add(at + rotation * Vector3.Scale(bottom, stretch) * scale + Vector3.down * 0.03f);

        int topIndex = start + ring.Length;
        int bottomIndex = topIndex + 1;
        Vector3 center = at;
        for (int i = 0; i < ring.Length; i++)
        {
            int a = start + i;
            int b = start + (i + 1) % ring.Length;
            AddFacingOut(verts, tris, a, topIndex, b, center);
            AddFacingOut(verts, tris, a, b, bottomIndex, center);
        }
    }

    static void AddFacingOut(List<Vector3> verts, List<int> tris, int a, int b, int c, Vector3 center)
    {
        Vector3 normal = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]);
        Vector3 mid = (verts[a] + verts[b] + verts[c]) / 3f;
        if (Vector3.Dot(normal, mid - center) < 0f)
        {
            int swap = b;
            b = c;
            c = swap;
        }

        tris.Add(a);
        tris.Add(b);
        tris.Add(c);
    }

    static Vector2 RandomPoint(System.Random rng, Settings settings)
    {
        float hx = Mathf.Abs(settings.scale.x) * PlaneHalf - 1.1f;
        float hz = Mathf.Abs(settings.scale.z) * PlaneHalf - 1.1f;
        float x = Mathf.Lerp(-hx, hx, (float)rng.NextDouble());
        float z = Mathf.Lerp(-hz, hz, (float)rng.NextDouble());
        return new Vector2(settings.origin.x + x, settings.origin.z + z);
    }

    static float EdgeDistance(Vector2 world, Settings settings)
    {
        if (IsDirt(world, settings))
            return 0f;

        float best = 4f;
        float[] radii = { 0.45f, 0.9f, 1.4f, 1.9f, 2.4f };
        for (int r = 0; r < radii.Length; r++)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 0.25f;
                Vector2 sample = world + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radii[r];
                if (IsDirt(sample, settings))
                    best = Mathf.Min(best, radii[r]);
            }
        }

        return best;
    }

    static float MapEdgeDistance(Vector2 world, Settings settings)
    {
        float hx = Mathf.Abs(settings.scale.x) * PlaneHalf;
        float hz = Mathf.Abs(settings.scale.z) * PlaneHalf;
        float dx = hx - Mathf.Abs(world.x - settings.origin.x);
        float dz = hz - Mathf.Abs(world.y - settings.origin.z);
        return Mathf.Min(dx, dz);
    }

    static bool Blocked(Vector2 world, KeepOut[] keepOuts)
    {
        if (keepOuts == null)
            return false;

        for (int i = 0; i < keepOuts.Length; i++)
        {
            if (Vector2.Distance(world, keepOuts[i].center) < keepOuts[i].radius)
                return true;
        }

        return false;
    }

    static Vector3 ToDecoration(Vector2 world, Settings settings)
    {
        Vector3 plane = new Vector3(
            (world.x - settings.origin.x) / Safe(settings.scale.x),
            0f,
            (world.y - settings.origin.z) / Safe(settings.scale.z));
        return new Vector3(plane.x * settings.scale.x, 0f, plane.z * settings.scale.z);
    }

    static Vector2 WorldOf(Vector3 planeLocal, Settings settings)
    {
        return new Vector2(
            settings.origin.x + planeLocal.x * settings.scale.x,
            settings.origin.z + planeLocal.z * settings.scale.z);
    }

    static float Safe(float scale)
    {
        return Mathf.Abs(scale) < 0.0001f ? 1f : scale;
    }

    static float Noise(float x, float z, int seed)
    {
        float fx = x * 0.37f;
        float fz = z * 0.37f;
        int x0 = Mathf.FloorToInt(fx);
        int z0 = Mathf.FloorToInt(fz);
        float tx = fx - x0;
        float tz = fz - z0;
        tx = tx * tx * (3f - 2f * tx);
        tz = tz * tz * (3f - 2f * tz);
        float a = Hash01(x0, z0, seed);
        float b = Hash01(x0 + 1, z0, seed);
        float c = Hash01(x0, z0 + 1, seed);
        float d = Hash01(x0 + 1, z0 + 1, seed);
        return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tz), tz) * 2f - 1f;
    }

    static float Hash01(int x, int z, int seed)
    {
        int n = x * 374761393 + z * 668265263 + seed * 1442695041;
        n = (n ^ (n >> 13)) * 1274126177;
        n ^= n >> 16;
        return (n & 0x7fffffff) / (float)int.MaxValue;
    }

    static int HashInt(int x, int z, int seed)
    {
        int n = x * 374761393 + z * 668265263 + seed * 1442695041;
        n = (n ^ (n >> 13)) * 1274126177;
        return n & 0x7fffffff;
    }
}
