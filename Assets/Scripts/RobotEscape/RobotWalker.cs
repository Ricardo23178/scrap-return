using UnityEngine;
using UnityEngine.Rendering;

namespace RobotEscape
{
    public sealed class RobotWalker : MonoBehaviour
    {
        public Vector2 minWalk = new Vector2(-7.4f, -3.4f);
        public Vector2 maxWalk = new Vector2(7.4f, -0.8f);
        public float speed = 3.2f;
        public Transform body;
        public SpriteRenderer illustration;
        public Sprite[] poses;
        public Vector2 Destination { get; private set; }
        public bool IsMoving { get { return Vector2.Distance(transform.position, Destination) > 0.025f; } }
        private Vector3 bodyOrigin;
        private SortingGroup sortingGroup;
        private float stepTime;

        private void Awake()
        {
            Destination = transform.position;
            sortingGroup = GetComponent<SortingGroup>();
            if (body != null) bodyOrigin = body.localPosition;
        }

        public void MoveTo(Vector2 point)
        {
            Destination = new Vector2(Mathf.Clamp(point.x, minWalk.x, maxWalk.x), Mathf.Clamp(point.y, minWalk.y, maxWalk.y));
        }

        public void Stop() { Destination = transform.position; }

        private void Update()
        {
            Vector2 direction = Destination - (Vector2)transform.position;
            Vector2 next = Vector2.MoveTowards(transform.position, Destination, speed * Time.deltaTime);
            transform.position = new Vector3(next.x, next.y, 0);
            float scale = Mathf.Lerp(1.12f, 0.8f, Mathf.InverseLerp(minWalk.y, maxWalk.y, next.y));
            transform.localScale = Vector3.one * scale;
            if (sortingGroup != null) sortingGroup.sortingOrder = 100 + Mathf.RoundToInt(-next.y * 20);
            if (body != null)
                body.localPosition = bodyOrigin + Vector3.up * (IsMoving ? Mathf.Sin(Time.time * 15) * 0.035f : 0);
            if (illustration != null && poses != null && poses.Length == 4)
            {
                if (Mathf.Abs(direction.x) > 0.03f) illustration.flipX = direction.x < 0;
                stepTime += Time.deltaTime * 6;
                illustration.sprite = IsMoving ? poses[1 + Mathf.FloorToInt(stepTime) % 3] : poses[0];
            }
        }
    }
}
