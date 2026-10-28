using UnityEngine;

namespace RobotEscape
{
    public enum ObjectKind { Trash, Battery, Console, Door, Scenery, Photo, Notice, DutyLog, Radio, Npc, ExitLever, UpperExit, FrequencyCard, PressureCabinet, ControlConsole, ControlTrace, ControlArchive, ControlExitLever, ControlExit, WorkshopValve, LooseLeg, LooseArm, RobotBody, DisguiseHat, DisguiseCoat, GateGuard, ScrapCrate }

    public sealed class RoomInteractable : MonoBehaviour
    {
        public ObjectKind kind;
        public Transform standPoint;
        public Vector2 StandPosition { get { return standPoint != null ? (Vector2)standPoint.position : (Vector2)transform.position; } }
    }
}
