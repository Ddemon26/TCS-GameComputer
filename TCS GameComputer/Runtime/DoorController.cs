using UnityEngine;

namespace TCS {
    public class DoorController : MonoBehaviour {
        public enum DoorState { Open, Closed }
        public DoorState m_doorState = DoorState.Closed;

        public AudioSource m_audioSource;
        public float m_doorOpenSpeed = 1.0f;
        public float m_doorCloseSpeed = 1.0f;

        public Vector3 m_openPosition;
        Vector3 m_closedPosition;


        void Start() {
            m_closedPosition = transform.position;
        }

        public void OpenDoor() {
            if (m_doorState != DoorState.Closed) return;

            m_doorState = DoorState.Open;
            m_audioSource.Play();
        }

        public void CloseDoor() {
            if (m_doorState != DoorState.Open) return;

            m_doorState = DoorState.Closed;
            m_audioSource.Play();
        }

        void Update() {
            if (m_doorState == DoorState.Open) {
                if (Vector3.Distance(transform.position, m_openPosition) > 0.01f) {
                    transform.position = Vector3.Lerp
                    (
                        transform.position,
                        m_openPosition,
                        Time.deltaTime * m_doorOpenSpeed
                    );
                }
            }
            else {
                if (Vector3.Distance(transform.position, m_closedPosition) > 0.01f) {
                    transform.position = Vector3.Lerp
                    (
                        transform.position,
                        m_closedPosition,
                        Time.deltaTime * m_doorCloseSpeed
                    );
                }
            }
        }
    }
}