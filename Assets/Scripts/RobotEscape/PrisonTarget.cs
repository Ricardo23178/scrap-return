using UnityEngine;

namespace RobotEscape
{
    public enum PrisonAction { Bed, Vent, ToCell, Ladder, Magnet, Gear, Hole, Clock, Door, Exit, Prisoner, Guard, Scenery }

    public sealed class PrisonTarget : MonoBehaviour
    {
        public PrisonAction action;
        public int area;
        public bool upperLanding;
        public Transform standPoint;
        public Vector2 StandPosition { get { return standPoint != null ? (Vector2)standPoint.position : (Vector2)transform.position; } }
    }
}
