using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
namespace TCS.GameComputer {
    public class GameTerminal : IDisposable {
        readonly Dictionary<string, UnityEvent> m_commandDictionary = new();
        int m_currentAudioIndex;

        readonly VisualElement m_root;
        TextField m_inputField;
        ScrollView m_scrollView;

        readonly float m_textSize;

        public Action InputStringChanged;

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

            // Register a callback for when the user presses Enter/Return
            m_inputField.RegisterCallback<KeyDownEvent>
            (
                evt => {
                    if (evt.keyCode is not (KeyCode.Return or KeyCode.KeypadEnter)) return;

                    //evt.PreventDefault();
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
                },
                TrickleDown.TrickleDown // Capture event before default handlers
            );

            // Register a callback for when the input field changes
            m_inputField.RegisterCallback<ChangeEvent<string>>
            (
                _ => {
                    InputStringChanged?.Invoke();
                }
            );

            // Prevent the input field from losing focus
            m_inputField.RegisterCallback<BlurEvent>
            (
                evt => {
                    evt.StopPropagation();
                    m_inputField.schedule.Execute
                    (
                        () => m_inputField.Focus()
                    ).ExecuteLater(1);
                }
            );
        }

        async void OnInputSubmitted(string input) {
            try {
                await ProcessInput(input);
                m_inputField.Focus();
            }
            catch (Exception e) {
                Debug.LogError($"An error occurred while processing input: {e.Message}");
            }
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
            if (cmdKey == "help") {
                Debug.LogWarning("'help' is a reserved command and cannot be used.");
                return;
            }

            if (m_commandDictionary.ContainsKey(cmdKey)) {
                Debug.LogWarning($"Duplicate command: {command.m_commandString}");
                return;
            }

            m_commandDictionary.Add(cmdKey, command.m_commandEvent);
        }

        public void ToggleInputFieldFocus(bool focus) => m_inputField.focusable = focus;

        public void Dispose() {
            m_root.Clear();
            m_inputField = null;
            m_scrollView = null;
            m_commandDictionary.Clear();
            InputStringChanged = null;
        }
    }
}