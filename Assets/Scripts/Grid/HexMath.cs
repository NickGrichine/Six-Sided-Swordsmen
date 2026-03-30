using UnityEngine;

public static class HexMath
{
    // Flat-top odd-q offset neighbor directions
    private static readonly Vector2Int[] EvenQDirections =
    {
        new Vector2Int(+1, -1),
        new Vector2Int(+1,  0),
        new Vector2Int( 0, -1),
        new Vector2Int( 0, +1),
        new Vector2Int(-1, -1),
        new Vector2Int(-1,  0)
    };

    private static readonly Vector2Int[] OddQDirections =
    {
        new Vector2Int(+1,  0),
        new Vector2Int(+1, +1),
        new Vector2Int( 0, -1),
        new Vector2Int( 0, +1),
        new Vector2Int(-1,  0),
        new Vector2Int(-1, +1)
    };

    public static Vector2Int[] GetNeighborDirections(int q)
    {
        return (q & 1) == 0 ? EvenQDirections : OddQDirections;
    }

    public static Vector3Int OddQToCube(Vector2Int offset)
    {
        int x = offset.x;
        int z = offset.y - (offset.x - (offset.x & 1)) / 2;
        int y = -x - z;
        return new Vector3Int(x, y, z);
    }

    public static Vector2Int CubeToOddQ(Vector3Int cube)
    {
        int q = cube.x;
        int r = cube.z + (cube.x - (cube.x & 1)) / 2;
        return new Vector2Int(q, r);
    }

    public static int Distance(Vector2Int a, Vector2Int b)
    {
        Vector3Int ac = OddQToCube(a);
        Vector3Int bc = OddQToCube(b);

        return Mathf.Max(
            Mathf.Abs(ac.x - bc.x),
            Mathf.Abs(ac.y - bc.y),
            Mathf.Abs(ac.z - bc.z)
        );
    }

    public static bool AreNeighbors(Vector2Int a, Vector2Int b)
    {
        foreach (Vector2Int dir in GetNeighborDirections(a.x))
        {
            if (a + dir == b)
                return true;
        }

        return false;
    }

    public static Vector3 CubeLerp(Vector3 a, Vector3 b, float t)
    {
        return Vector3.Lerp(a, b, t);
    }

    public static Vector3Int CubeRound(Vector3 cube)
    {
        int rx = Mathf.RoundToInt(cube.x);
        int ry = Mathf.RoundToInt(cube.y);
        int rz = Mathf.RoundToInt(cube.z);

        float dx = Mathf.Abs(rx - cube.x);
        float dy = Mathf.Abs(ry - cube.y);
        float dz = Mathf.Abs(rz - cube.z);

        if (dx > dy && dx > dz)
            rx = -ry - rz;
        else if (dy > dz)
            ry = -rx - rz;
        else
            rz = -rx - ry;

        return new Vector3Int(rx, ry, rz);
    }
}