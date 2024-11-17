using System;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace TCS.GameComputer {
    public class GameComputerHud : MonoBehaviour {
        [Header("UI Components")]
        [SerializeField] UIDocument m_uiDocument;
        [SerializeField] AudioSource[] m_clickSound;
        
        [SerializeField] float m_textSize = 14;

        int m_currentAudioIndex;

        [Header("Commands")]
        [SerializeField] List<TerminalCommand> m_commands = new();
        GameTerminal m_gameTerminal;
        
        [SerializeField] Camera m_screenFocusCamera;

        void Awake() {
            if (!m_uiDocument) { Debug.LogError("Missing UIDocument component."); return; }
            
            if (!m_screenFocusCamera) { Debug.LogError("Missing Camera component for screen focus."); return; }
            Logger.LogToDo("Add a way to focus on the screen when the console is opened.");

            m_gameTerminal = new GameTerminal(m_uiDocument, m_textSize);

            foreach (var cmd in m_commands) {
                m_gameTerminal.AddCommand(cmd);
            }

            m_gameTerminal.InputStringChanged += PlayClickSound;
        }

        async void Start() {
            try {
                m_gameTerminal.InitElements();

                await m_gameTerminal.AddOutput("> Welcome to the In-Game Computer Console!");
                await m_gameTerminal.AddOutput("> Type 'help' to see available commands.");
            }
            catch (Exception e) {
                Debug.LogError($"An error occurred during initialization: {e.Message}");
            }
        }

        void OnDisable() => m_gameTerminal.InputStringChanged -= PlayClickSound;

        void OnDestroy() => m_gameTerminal.Dispose();

        void PlayClickSound() {
            if (m_clickSound.Length <= 0) return;

            m_clickSound[m_currentAudioIndex].Play();
            m_currentAudioIndex = (m_currentAudioIndex + 1) % m_clickSound.Length;
        }
    }
}