
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [AddComponentMenu("Texel/Debug Log")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DebugLog : UdonSharpBehaviour
    {
        public Text debugText;
        public int lineCount = 28;

        string[] debugLines;
        int debugIndex = 0;

        public void _Write(string component, string message)
        {
            if (debugLines == null || debugLines.Length == 0)
            {
                debugLines = new string[lineCount];
                for (int i = 0; i < debugLines.Length; i++)
                    debugLines[i] = "";
            }

            debugLines[debugIndex] = $"[{component}] {message}";

            string buffer = "";
            for (int i = debugIndex + 1; i < debugLines.Length; i++)
                buffer = buffer + debugLines[i] + "\n";
            for (int i = 0; i < debugIndex; i++)
                buffer = buffer + debugLines[i] + "\n";
            buffer = buffer + debugLines[debugIndex];

            debugIndex += 1;
            if (debugIndex >= debugLines.Length)
                debugIndex = 0;

            debugText.text = buffer;
        }
    }
}