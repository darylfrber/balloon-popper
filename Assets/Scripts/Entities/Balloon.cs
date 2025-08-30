using UnityEngine;

namespace BalloonBuster.Core
{
    [RequireComponent(typeof(Collider2D))]
    public class Balloon : MonoBehaviour
    {
        private BalloonTypeSO type;
        private float riseSpeed;
        private int scoreValue;

        public void Configure(BalloonTypeSO t)
        {
            type = t;
            riseSpeed = t.baseRiseSpeed * UnityEngine.Random.Range(0.9f, 1.1f);
            scoreValue = t.scoreValue;
            transform.localScale = Vector3.one * t.size;
            // color / sprite could be set here
        }

        private void Update()
        {
            transform.Translate(Vector3.up * riseSpeed * Time.deltaTime);
        }

        public void Pop()
        {
            GameManager.Instance.AddScore(scoreValue);
            Destroy(gameObject);
        }

        private void OnMouseDown()
        {
            // simple click pop for prototype
            Pop();
        }
    }
}
