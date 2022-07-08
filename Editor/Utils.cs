using UnityEngine;

namespace UniBeat.RhythmEngine.Editor
{
    public static class Utils
    {
        /// <summary>
        /// Draws a gizmo rectangle
        /// </summary>
        /// <param name="center">Center.</param>
        /// <param name="size">Size.</param>
        /// <param name="color">Color.</param>
        public static void DrawGizmoRectangle(Vector2 center, Vector2 size, Color color)
        {
            Gizmos.color = color;

            Vector3 v3TopLeft = new Vector3(center.x - size.x / 2, center.y + size.y / 2, 0);
            Vector3 v3TopRight = new Vector3(center.x + size.x / 2, center.y + size.y / 2, 0);
            ;
            Vector3 v3BottomRight = new Vector3(center.x + size.x / 2, center.y - size.y / 2, 0);
            ;
            Vector3 v3BottomLeft = new Vector3(center.x - size.x / 2, center.y - size.y / 2, 0);
            ;

            Gizmos.DrawLine(v3TopLeft, v3TopRight);
            Gizmos.DrawLine(v3TopRight, v3BottomRight);
            Gizmos.DrawLine(v3BottomRight, v3BottomLeft);
            Gizmos.DrawLine(v3BottomLeft, v3TopLeft);
        }
    }
}
