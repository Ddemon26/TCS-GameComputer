#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

namespace TCS.GameComputer {
    [RequireComponent(typeof(Camera))]
    public class CameraTempSwap : MonoBehaviour {
        [Header("Temp Camera")]
        public Camera m_tempCamera;
        
        [Header("Temp Camera Time")]
        public float m_tempCameraTime = 1.0f;

        const int PRIORITY = 9999;
        float m_tempCameraTimer;
        bool m_tempCameraActive;

        void Awake() {
            if (!m_tempCamera) {
                m_tempCamera = GetComponent<Camera>();
                if (!m_tempCamera) {
                    Debug.LogError("Temp Camera not set");
                    return;
                }
            }
            
            m_tempCameraTimer = 0.0f;
            m_tempCamera.depth = PRIORITY;
            m_tempCameraActive = false;
            m_tempCamera.enabled = false;
        }

        void Update() {
            if (!m_tempCameraActive) return;
            
            m_tempCameraTimer -= Time.deltaTime;
            
            if (!(m_tempCameraTimer <= 0.0f)) return;
            
            m_tempCamera.enabled = false;
            m_tempCameraActive = false;
        }

#if ODIN_INSPECTOR
        [Button]
#endif
        public void SwapToTempCamera() {
            if (m_tempCameraActive) {
                return;
            }

            m_tempCamera.enabled = true;
            m_tempCameraActive = true;
            m_tempCameraTimer = m_tempCameraTime;
        }
    }
}