using System;
using UnityEngine.Events;
namespace TCS.GameComputer {
    [Serializable] public class TerminalCommand {
        public string m_commandString;
        public UnityEvent m_commandEvent;
    }
}