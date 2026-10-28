using UnityEngine;
using UnityEngine.EventSystems;

namespace RobotEscape
{
    public sealed class SlideBlockHandle : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public ControlRoomController controller;
        public int blockIndex;
        private bool dragging;
        private Vector2 pointerStart;
        private int positionStart;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (controller != null && eventData.button == PointerEventData.InputButton.Left)
                controller.SelectBlock(blockIndex);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragging = false;
            if (controller == null || !controller.BoardInputEnabled || controller.boardArea == null ||
                controller.GetBlockPosition(blockIndex) < 0 || eventData.button != PointerEventData.InputButton.Left) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(controller.boardArea, eventData.position, eventData.pressEventCamera, out pointerStart)) return;
            positionStart = controller.GetBlockPosition(blockIndex);
            controller.SelectBlock(blockIndex);
            dragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging || controller == null || !controller.BoardInputEnabled || controller.boardArea == null)
            {
                dragging = false;
                return;
            }
            Vector2 pointer;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(controller.boardArea, eventData.position, eventData.pressEventCamera, out pointer)) return;
            bool horizontal = controller.IsBlockHorizontal(blockIndex);
            Vector2 cell = controller.BoardCellSize;
            float offset = horizontal ? (pointer.x - pointerStart.x) / cell.x : -(pointer.y - pointerStart.y) / cell.y;
            int target = Mathf.Clamp(positionStart + Mathf.RoundToInt(offset), 0, 2);
            int current = controller.GetBlockPosition(blockIndex);
            while (current != target)
            {
                if (!controller.TryMoveBlock(blockIndex, target > current ? 1 : -1)) break;
                current = controller.GetBlockPosition(blockIndex);
            }
        }

        public void OnEndDrag(PointerEventData eventData) { dragging = false; }
        private void OnDisable() { dragging = false; }
    }
}
