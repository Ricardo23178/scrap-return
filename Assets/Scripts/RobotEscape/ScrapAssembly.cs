using UnityEngine;
using UnityEngine.UI;

namespace RobotEscape
{
    public sealed class ScrapAssembly : MonoBehaviour
    {
        public RobotEscapeRoom room;
        public RoomInteractable leg, arm, self, armCrate;
        public Button legButton, armButton;
        public GameObject legIcon, armIcon, damagedBody, repairedLegs;
        public bool LegRevealed { get; private set; }
        public bool HasLeg { get; private set; }
        public bool HasArm { get; private set; }
        public bool LegAttached { get; private set; }
        public bool ArmAttached { get; private set; }
        public bool ArmRevealed { get; private set; }
        public int SelectedPart { get; private set; }

        private void Awake()
        {
            if (legButton != null) legButton.onClick.AddListener(SelectLeg);
            if (armButton != null) armButton.onClick.AddListener(SelectArm);
        }

        public bool CanReachWithoutLeg(RoomInteractable target)
        {
            return (target == room.trash || target == leg || target == self) &&
                Vector2.Distance(room.robot.transform.position, target.StandPosition) <= 1.75f;
        }

        public bool HandleInteraction(RoomInteractable target)
        {
            if (target == self)
            {
                if (SelectedPart == 1 && HasLeg)
                {
                    HasLeg = false;
                    LegAttached = true;
                }
                else if (SelectedPart == 2 && HasArm && LegAttached)
                {
                    HasArm = false;
                    ArmAttached = true;
                    room.CompleteAssembly();
                }
                SelectedPart = 0;
                Refresh();
                return true;
            }
            if (target == leg)
            {
                if (LegRevealed && !LegAttached && !HasLeg)
                {
                    HasLeg = true;
                    leg.gameObject.SetActive(false);
                    Refresh();
                }
                return true;
            }
            if (target == armCrate)
            {
                if (LegAttached && !ArmRevealed)
                {
                    ArmRevealed = true;
                    arm.gameObject.SetActive(true);
                    armCrate.transform.rotation = Quaternion.Euler(0, 0, -6);
                }
                return true;
            }
            if (target == arm)
            {
                if (LegAttached && ArmRevealed && !HasArm && !ArmAttached)
                {
                    HasArm = true;
                    arm.gameObject.SetActive(false);
                    Refresh();
                }
                return true;
            }
            if (target == room.terminal || target == room.battery) return true;
            if (target.kind == ObjectKind.WorkshopValve && !ArmAttached)
            {
                if (room.mechanism != null) room.mechanism.TryTurnValve();
                return true;
            }
            return false;
        }

        public void RevealLeg()
        {
            LegRevealed = true;
            leg.gameObject.SetActive(true);
        }

        public void SelectLeg()
        {
            if (!HasLeg || room.InputBlocked) return;
            SelectedPart = SelectedPart == 1 ? 0 : 1;
            Refresh();
        }

        public void SelectArm()
        {
            if (!HasArm || room.InputBlocked) return;
            SelectedPart = SelectedPart == 2 ? 0 : 2;
            Refresh();
        }

        public void ResetAssembly()
        {
            LegRevealed = HasLeg = HasArm = LegAttached = ArmAttached = ArmRevealed = false;
            SelectedPart = 0;
            leg.gameObject.SetActive(false);
            arm.gameObject.SetActive(false);
            armCrate.transform.rotation = Quaternion.identity;
            Refresh();
        }

        private void Refresh()
        {
            if (self != null && self.GetComponent<Collider2D>() != null)
                self.GetComponent<Collider2D>().enabled = SelectedPart != 0;
            if (legButton != null) legButton.interactable = HasLeg;
            if (armButton != null) armButton.interactable = HasArm;
            if (legIcon != null) legIcon.SetActive(HasLeg);
            if (armIcon != null) armIcon.SetActive(HasArm);
            if (legButton != null) legButton.GetComponent<Image>().color = SelectedPart == 1 ? new Color(0.61f, 0.42f, 0.2f) : new Color(0.14f, 0.2f, 0.2f);
            if (armButton != null) armButton.GetComponent<Image>().color = SelectedPart == 2 ? new Color(0.61f, 0.42f, 0.2f) : new Color(0.14f, 0.2f, 0.2f);
            if (damagedBody != null) damagedBody.SetActive(!ArmAttached);
            if (repairedLegs != null) repairedLegs.SetActive(LegAttached && !ArmAttached);
            room.robot.illustration.enabled = ArmAttached;
        }
    }
}
