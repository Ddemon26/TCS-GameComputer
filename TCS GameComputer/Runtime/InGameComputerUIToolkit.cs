using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace TCS {
    [System.Serializable]
    public class Command {
        public string m_commandString;
        public UnityEvent m_commandEvent;
    }

    public class InGameComputerUIToolkit : MonoBehaviour {
        [Header("UI Components")]
        [SerializeField] UIDocument m_uiDocument;
        [SerializeField] AudioSource[] m_clickSound;
        int m_currentAudioIndex;
        
        VisualElement m_root;

        TextField m_inputField;
        ScrollView m_scrollView;

        [Header("Commands")]
        [SerializeField] List<Command> m_commands = new();

        readonly Dictionary<string, UnityEvent> m_commandDictionary = new();

        void Awake() {
            if (!m_uiDocument) {
                Debug.LogError("Missing UIDocument component.");
                return;
            }

            m_root = m_uiDocument.rootVisualElement;
            m_inputField = m_root.Q<TextField>();
            m_scrollView = m_root.Q<ScrollView>();

            if (m_inputField == null || m_scrollView == null) {
                Debug.LogError("Missing required UI elements.");
                return;
            }

            // Set the TextField to single-line mode
            m_inputField.multiline = false;

            foreach (var cmd in m_commands) {
                string cmdKey = cmd.m_commandString.ToLower();
                if (!m_commandDictionary.ContainsKey(cmdKey) && cmdKey != "help") {
                    m_commandDictionary.Add(cmdKey, cmd.m_commandEvent);
                }
                else if (cmdKey == "help") {
                    Debug.LogWarning("'help' is a reserved command and cannot be used in the commands list.");
                }
                else {
                    Debug.LogWarning($"Duplicate command detected: {cmd.m_commandString}");
                }
            }

            m_inputField.RegisterCallback<KeyDownEvent>(
                evt => {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) {
                        evt.PreventDefault();
                        evt.StopPropagation();

                        if (!string.IsNullOrEmpty(m_inputField.value.Trim())) {
                            OnInputSubmitted(m_inputField.value.Trim());
                            m_inputField.value = ""; // Clear input after submission
                        }

                        // Schedule the focus to occur after the UI updates
                        m_inputField.schedule.Execute(() => m_inputField.Focus()).ExecuteLater(1);
                    }
                },
                TrickleDown.TrickleDown // Capture event before default handlers
            );
            
            m_inputField.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                PlayClickSound();
            });

        }

        void Start() {
            AddOutput("> Welcome to the In-Game Computer Console!");
            AddOutput("> Type 'help' to see available commands.");
            m_inputField.Focus();
        }

        void PlayClickSound() {
            if (m_clickSound.Length <= 0) return;
            
            m_clickSound[m_currentAudioIndex].Play();
            m_currentAudioIndex = (m_currentAudioIndex + 1) % m_clickSound.Length;
        }

        void OnInputSubmitted(string input) {
            ProcessInput(input);
            m_inputField.Focus();
        }

        void ProcessInput(string input) {
            string trimmedInput = input.Trim();
            
            if (string.IsNullOrEmpty(trimmedInput)) {
                return;
            }

            AddOutput($"> {trimmedInput}");
            string lowerInput = trimmedInput.ToLower();

            if (lowerInput == "help") {
                HelpCommand();
            }
            else if (m_commandDictionary.TryGetValue(lowerInput, out var value)) {
                value.Invoke();
            }
            // else {
            //     AddOutput("> Error: Unknown command. Type 'help' for a list of commands.");
            // }
        }

        void AddOutput(string message) {
            Label newLabel = new Label(message);
            m_scrollView.Add(newLabel);
            
            // Start a coroutine to scroll to the bottom after the layout has been updated
            StartCoroutine(ScrollToBottom(newLabel));
        }

        IEnumerator ScrollToBottom(Label newLabel) {
            // Wait for the end of the frame to ensure the layout is updated
            yield return new WaitForEndOfFrame();

            m_scrollView.scrollOffset = new Vector2
            (
                m_scrollView.contentContainer.contentRect.width,
                m_scrollView.contentContainer.contentRect.height
            );

            // Scroll to the new label
            m_scrollView.ScrollTo(newLabel);
            
            // Ensure the input field is focused immediately after adding the output
            m_inputField.Focus();
        }

        void HelpCommand() {
            AddOutput("> Available Commands:");
            foreach (string cmd in m_commandDictionary.Keys) {
                AddOutput($"- {cmd}");
            }
        }

        void RemoveOutput() {
            foreach (var child in m_scrollView.Children()) {
                m_scrollView.Remove(child);
            }
        }
    }
}