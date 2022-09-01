using UdonSharp;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class TranslationTable : UdonSharpBehaviour
    {
        public string[] languages;
        public string[] keys;
        public string[] values;

        public string _GetValue(int langIndex, int keyIndex)
        {
            int max = langIndex * keyIndex;
            if (max < 0 || max >= values.Length)
                return "";

            int index = keyIndex * languages.Length + langIndex;
            string lookup = values[index];

            if (lookup == "")
            {
                index = keyIndex * languages.Length;
                lookup = values[index];
            }

            return lookup;
        }
    }
}
