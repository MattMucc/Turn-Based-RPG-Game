using UnityEngine;
namespace Game.GridPrototype
{
    public enum QteLane { Left, Right, Bottom }
    public static class QteLayout
    {
        public static Vector2 Anchor(QteLane lane) => lane == QteLane.Left ? new Vector2(0, 0.45f)
            : lane == QteLane.Right ? new Vector2(1, 0.45f) : new Vector2(0.5f, 0);
        public static Vector2 Offset(QteLane lane) => lane == QteLane.Left ? new Vector2(90, 0)
            : lane == QteLane.Right ? new Vector2(-90, 0) : new Vector2(0, 154);
    }
}
