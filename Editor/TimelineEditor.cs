using UnityEngine;
using UnityEditor;

namespace UniBeat.RhythmEngine.Editor
{

    [CustomEditor(typeof(DrawableTimeline))]
    [InitializeOnLoad]

    /// <summary>
    /// Adds custom labels to the NoteGrid in Editor
    /// </summary>

    public class TimelineEditor : UnityEditor.Editor
    {
        [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
        static void DrawGameObjectName(DrawableTimeline timeline, GizmoType gizmoType)
        {
            if (!timeline.DataIsLoaded)
                return;

            GUIStyle style = new GUIStyle();
            Vector3 v3FrontTopLeft;
            var x = timeline.transform.position.x;
            var y = timeline.transform.position.y;
            var z = timeline.transform.position.z;
            style.normal.textColor = Color.yellow;
            v3FrontTopLeft = new Vector3(x, y, z);  // Front top left corner
            Handles.Label(v3FrontTopLeft, "Music Grid Bounds", style);
            var collider = timeline.Collider;

            if (collider == null)
                return;

            //MMDebug.DrawHandlesBounds(noteGrid.GridBounds,Color.yellow);
            UniBeat.RhythmEngine.Editor.Utils.DrawGizmoRectangle(new Vector2(x + collider.offset.x, y), collider.size, Color.magenta);
        }
    }
}
