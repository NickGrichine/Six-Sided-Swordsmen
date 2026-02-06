using UnityEngine;

// Hex coordinate Math
public static class HexMath
{
    // Axial (q, r) -> cube (x, y, z) {x + y + z = 0}

    public static Vector3 AxialToCube(Vector2Int axial)
    {
        float x = axial.x;
        float z = axial.y;
        float y = - x - z;
        return new Vector3(x, y, z);
    }

    public static Vector2Int CubeToAxial(Vector3 cube)
    {
        int q = Mathf.RoundToInt(cube.x);
        int r = Mathf.RoundToInt(cube.z);
        return new Vector2Int(q, r);
    }

    public static Vector3 HexLerp(Vector3 a, Vector3 b, float t)
    {
        return new Vector3(
            Mathf.Lerp(a.x, b.x, t),
            Mathf.Lerp(a.y, b.y, t),
            Mathf.Lerp(a.z, b.z, t)
        );
    }

    public static Vector3 HexRound(Vector3 cube)
    {
        int rx = Mathf.RoundToInt(cube.x);
        int ry = Mathf.RoundToInt(cube.y);
        int rz = Mathf.RoundToInt(cube.z);

        float xDiff = Mathf.Abs(rx - cube.x);
        float yDiff = Mathf.Abs(ry - cube.y);
        float zDiff = Mathf.Abs(rz - cube.z);

        if (xDiff > yDiff && xDiff > zDiff)
            rx = -ry - rz;
        else if (yDiff > zDiff)
            ry = -rx - rz;
        else
            rz = -rx - ry;

        return new Vector3(rx, ry, rz);
    }

    public static int Distance(Vector2Int aAxial, Vector2Int bAxial)
    {
        int dq = aAxial.x - bAxial.x;
        int dr = aAxial.y - bAxial.y;
        int ds = - dq - dr;
        return (Mathf.Abs(dq) + Mathf.Abs(dr) + Mathf.Abs(ds)) / 2;
    }

    // 6 axial neighbor
    public static readonly Vector2Int[] AxialDirections =
    {
        new Vector2Int(1, 0),
        new Vector2Int(1, -1), 
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0),
        new Vector2Int(-1, 1),
        new Vector2Int(0, 1)
    };

}