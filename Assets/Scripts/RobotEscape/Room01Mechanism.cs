using UnityEngine;

namespace RobotEscape
{
    public sealed class Room01Mechanism : MonoBehaviour
    {
        public RobotEscapeRoom room;
        public RoomInteractable valve;
        public Transform valveWheel, gaugeNeedle, piston;
        public SpriteRenderer readyLamp;
        public SpriteRenderer[] steam;
        public bool InterlockReleased { get; private set; }
        public bool Cycling { get; private set; }
        public float Pressure { get; private set; }

        private Quaternion wheelStart, needleStart;
        private Vector3 pistonStart;
        private Color lampStart;
        private Vector3[] steamStart;
        private Color[] steamColors;
        private bool[] steamEnabled;
        private Transform robotBody;
        private Quaternion robotBodyRotation;
        private SpriteRenderer robotIllustration;
        private bool robotFlip, posing, pumping, initialized;
        private float elapsed, facing;

        private void Awake()
        {
            if (valveWheel != null) wheelStart = valveWheel.localRotation;
            if (gaugeNeedle != null) needleStart = gaugeNeedle.localRotation;
            if (piston != null) pistonStart = piston.localPosition;
            if (readyLamp != null) lampStart = readyLamp.color;
            int count = steam == null ? 0 : steam.Length;
            steamStart = new Vector3[count];
            steamColors = new Color[count];
            steamEnabled = new bool[count];
            for (int i = 0; i < count; i++)
            {
                if (steam[i] == null) continue;
                steamStart[i] = steam[i].transform.localPosition;
                steamColors[i] = steam[i].color;
                steamEnabled[i] = steam[i].enabled;
            }
            initialized = true;
        }

        private void OnEnable()
        {
            if (InterlockReleased && readyLamp != null) readyLamp.color = new Color(0.32f, 0.85f, 0.53f);
        }

        public void ReleaseInterlock()
        {
            if (room == null || !room.Started || !room.Powered || room.Won || room.DoorOpen ||
                (room.assembly != null ? !room.assembly.LegAttached || !room.assembly.ArmAttached : room.PuzzleProgress < 3) || InterlockReleased) return;
            InterlockReleased = true;
            if (room.robot != null) room.robot.Stop();
            if (isActiveAndEnabled && readyLamp != null) readyLamp.color = new Color(0.32f, 0.85f, 0.53f);
        }

        public void TryTurnValve()
        {
            if (!initialized || !isActiveAndEnabled || room == null || !room.Started || room.Won || room.DoorOpen ||
                room.InputBlocked || room.robot == null || valve == null ||
                !valve.gameObject.activeInHierarchy ||
                Vector2.Distance(room.robot.transform.position, valve.StandPosition) >= 0.16f) return;
            room.robot.Stop();
            elapsed = 0;
            pumping = InterlockReleased;
            Cycling = true;
            robotBody = room.robot.body;
            robotIllustration = room.robot.illustration;
            if (robotBody != null) robotBodyRotation = robotBody.localRotation;
            if (robotIllustration != null) robotFlip = robotIllustration.flipX;
            facing = valve.transform.position.x < room.robot.transform.position.x ? -1f : 1f;
            posing = true;
        }

        private void Update()
        {
            if (!Cycling) return;
            if (room == null || !room.isActiveAndEnabled || !room.Started || room.Won)
            {
                ResetMechanism();
                return;
            }
            room.robot.Stop();
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / (pumping ? 1.6f : 0.3f));
            if (!pumping)
            {
                if (valveWheel != null)
                    valveWheel.localRotation = wheelStart * Quaternion.Euler(0, 0, Mathf.Sin(elapsed * 65) * 3f * (1 - progress));
            }
            else
            {
                Pressure = Mathf.SmoothStep(0, 1, progress);
                if (valveWheel != null) valveWheel.localRotation = wheelStart * Quaternion.Euler(0, 0, -360 * Pressure);
                if (gaugeNeedle != null) gaugeNeedle.localRotation = needleStart * Quaternion.Euler(0, 0, 130 * Pressure);
                if (piston != null) piston.localPosition = pistonStart + Vector3.up * (0.3f * Pressure);
                for (int i = 0; i < steamStart.Length; i++)
                {
                    if (steam[i] == null) continue;
                    Color color = steamColors[i];
                    color.a *= Mathf.Sin(progress * Mathf.PI);
                    steam[i].enabled = true;
                    steam[i].color = color;
                    steam[i].transform.localPosition = steamStart[i] + Vector3.up * (0.65f * progress);
                }
            }
            if (progress < 1) return;
            if (pumping) room.OpenPneumaticDoor();
            else if (valveWheel != null) valveWheel.localRotation = wheelStart;
            Cycling = false;
            RestoreSteam();
            RestoreRobotPose();
        }

        private void LateUpdate()
        {
            if (!Cycling || !posing) return;
            float lean = Mathf.Sin(Mathf.Clamp01(elapsed / (pumping ? 1.6f : 0.3f)) * Mathf.PI);
            if (robotBody != null) robotBody.localRotation = robotBodyRotation * Quaternion.Euler(0, 0, -7f * facing * lean);
            if (robotIllustration != null) robotIllustration.flipX = facing < 0;
        }

        public void ResetMechanism()
        {
            InterlockReleased = false;
            RestoreMechanism();
        }

        private void OnDisable()
        {
            RestoreMechanism();
        }

        private void RestoreMechanism()
        {
            Cycling = pumping = false;
            Pressure = elapsed = 0;
            if (!initialized) return;
            if (valveWheel != null) valveWheel.localRotation = wheelStart;
            if (gaugeNeedle != null) gaugeNeedle.localRotation = needleStart;
            if (piston != null) piston.localPosition = pistonStart;
            if (readyLamp != null) readyLamp.color = lampStart;
            RestoreSteam();
            RestoreRobotPose();
        }

        private void RestoreSteam()
        {
            if (steamStart == null) return;
            for (int i = 0; i < steamStart.Length; i++)
            {
                if (steam[i] == null) continue;
                steam[i].transform.localPosition = steamStart[i];
                steam[i].color = steamColors[i];
                steam[i].enabled = steamEnabled[i];
            }
        }

        private void RestoreRobotPose()
        {
            if (!posing) return;
            if (robotBody != null) robotBody.localRotation = robotBodyRotation;
            if (robotIllustration != null) robotIllustration.flipX = robotFlip;
            posing = false;
        }
    }
}
