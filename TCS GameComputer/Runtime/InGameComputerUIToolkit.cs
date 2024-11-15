using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TCS {
    [Serializable]
    public class TerminalCommand {
        public string m_commandString;
        public UnityEvent m_commandEvent;
    }

    public class GameTerminal : IDisposable {
        readonly Dictionary<string, UnityEvent> m_commandDictionary = new();
        int m_currentAudioIndex;

        readonly VisualElement m_root;
        TextField m_inputField;
        ScrollView m_scrollView;

        readonly float m_textSize;

        string m_userName = "Player";
        const string CURRENT_DIRECTORY = "~";
        const string PROMPT_SYMBOL = "$ ";
        const string PROMPT_ECHO = "> ";
        const string PROMPT_ERROR = "! ";
        const string PROMPT_ROOT = "# ";

        public Action InputStringChanged;

        public string UserName {
            get => m_userName;
            set => m_userName = string.IsNullOrEmpty(value) ? "Player" : value;
        }

        public GameTerminal(UIDocument uiDocument, float textSize = 14) {
            m_root = uiDocument.rootVisualElement;
            m_textSize = textSize;
        }

        public void InitElements() {
            m_inputField = m_root.Q<TextField>();
            m_scrollView = m_root.Q<ScrollView>();

            if (m_inputField == null || m_scrollView == null) {
                Debug.LogError("Missing required UI elements.");
                return;
            }

            // Set the TextField to single-line mode
            m_inputField.multiline = false;

            m_inputField.RegisterCallback<KeyDownEvent>
            (
                evt => {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) {
                        evt.PreventDefault();
                        evt.StopPropagation();

                        if (!string.IsNullOrEmpty(m_inputField.value.Trim())) {
                            OnInputSubmitted(m_inputField.value.Trim());
                            m_inputField.value = ""; // Clear input after submission
                        }

                        // Schedule the focus to occur after the UI updates
                        m_inputField.schedule.Execute
                        (
                            () => m_inputField.Focus()
                        ).ExecuteLater(1);
                    }
                },
                TrickleDown.TrickleDown // Capture event before default handlers
            );

            m_inputField.RegisterCallback<ChangeEvent<string>>
            (
                _ => {
                    InputStringChanged?.Invoke();
                }
            );
            
            m_inputField.RegisterCallback<BlurEvent>
            (
                evt => {
                    evt.StopPropagation(); // Prevent the blur event from propagating
                    m_inputField.schedule.Execute
                    (
                        () => m_inputField.Focus()
                    ).ExecuteLater(1); // Refocus the input field
                }
            );
        }

        async void OnInputSubmitted(string input) {
            await ProcessInput(input);
            m_inputField.Focus();
        }

        async Task ProcessInput(string input) {
            string trimmedInput = input.Trim();

            if (string.IsNullOrEmpty(trimmedInput)) {
                return;
            }

            await AddOutput($"> {trimmedInput}");
            string lowerInput = trimmedInput.ToLower();

            if (lowerInput == "help") {
                await HelpCommand();
            }
            else if (m_commandDictionary.TryGetValue(lowerInput, out var value)) {
                value.Invoke();
            }
            else {
                await AddOutput("> Error: Unknown command. Type 'help' for a list of commands.");
            }
        }

        public async Task AddOutput(string message) {
            var newLabel = new Label(message) {
                style = {
                    fontSize = m_textSize,
                },
            };
            m_scrollView.Add(newLabel);

            // Await the asynchronous method to ensure it completes before continuing
            await ScrollToBottomAsync(newLabel);
        }

        async Task ScrollToBottomAsync(Label newLabel) {
            // Wait for the end of the frame to ensure the layout is updated
            await Task.Yield();

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

        async Task HelpCommand() {
            await AddOutput("> Available Commands:");
            foreach (string cmd in m_commandDictionary.Keys) {
                await AddOutput($"- {cmd}");
            }
        }

        public void AddCommand(TerminalCommand command) {
            string cmdKey = command.m_commandString.ToLower();
            if (!m_commandDictionary.ContainsKey(cmdKey) && cmdKey != "help") {
                m_commandDictionary.Add(cmdKey, command.m_commandEvent);
            }
            else if (cmdKey == "help") {
                Debug.LogWarning("'help' is a reserved command and cannot be used in the commands list.");
            }
            else {
                Debug.LogWarning($"Duplicate command detected: {command.m_commandString}");
            }
        }
        public void Dispose() {
            m_root.Clear();
            m_inputField = null;
            m_scrollView = null;
            m_commandDictionary.Clear();
            InputStringChanged = null;
            m_userName = null;
        }
    }

    public class InGameComputerUIToolkit : MonoBehaviour {
        [Header("UI Components")]
        [SerializeField] UIDocument m_uiDocument;
        [SerializeField] AudioSource[] m_clickSound;
        
        [SerializeField] float m_textSize = 14;

        int m_currentAudioIndex;

        [Header("Commands")]
        [SerializeField] List<TerminalCommand> m_commands = new();
        GameTerminal m_gameTerminal;

        void Awake() {
            if (!m_uiDocument) {
                Debug.LogError("Missing UIDocument component.");
                return;
            }

            m_gameTerminal = new GameTerminal(m_uiDocument, m_textSize);

            foreach (var cmd in m_commands) {
                m_gameTerminal.AddCommand(cmd);
            }

            m_gameTerminal.InputStringChanged += PlayClickSound;
        }

        async void Start() {
            m_gameTerminal.InitElements();

            await m_gameTerminal.AddOutput("> Welcome to the In-Game Computer Console!");
            await m_gameTerminal.AddOutput("> Type 'help' to see available commands.");
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