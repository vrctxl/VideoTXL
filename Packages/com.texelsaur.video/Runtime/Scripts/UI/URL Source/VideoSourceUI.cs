
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VideoSourceUI : UdonSharpBehaviour
    {
        [SerializeField] internal SourceManager sourceManager;
        [SerializeField] internal GameObject contentRoot;
        [SerializeField] internal GameObject buttonRoot;
        [SerializeField] internal GameObject templateRoot;
        [SerializeField] internal GameObject buttonTemplate;

        SourceManager boundSourceManager;
        GameObject[] contentPanels;
        VideoSourceUIButton[] contentButtons;

        VideoSourceUIBase[] sourceTemplates;

        void Start()
        {
            if (sourceManager)
                _BindSourceManager(sourceManager);
        }

        public SourceManager SourceManager
        {
            get { return boundSourceManager; }
        }

        public void _BindSourceManager(SourceManager sourceManager)
        {
            if (!sourceManager || boundSourceManager == sourceManager)
                return;

            if (templateRoot && (sourceTemplates == null || sourceTemplates.Length == 0))
                sourceTemplates = templateRoot.GetComponentsInChildren<VideoSourceUIBase>(true);
            else
                sourceTemplates = new VideoSourceUIBase[0];

            boundSourceManager = sourceManager;

            contentPanels = new GameObject[boundSourceManager.Count];
            contentButtons = new VideoSourceUIButton[boundSourceManager.Count];

            int firstTemplateSource = -1;
            
            for (int i = 0; i < boundSourceManager.Count; i++)
            {
                VideoUrlSource source = boundSourceManager._GetSource(i);
                if (!source)
                    continue;

                VideoSourceUIBase template = null;
                foreach (var tmp in sourceTemplates)
                {
                    if (tmp._CompatibleSource(source))
                    {
                        template = tmp;
                        break;
                    }
                }

                if (!template)
                    continue;

                if (firstTemplateSource == -1)
                    firstTemplateSource = i;

                if (buttonRoot && buttonTemplate) {
                    GameObject button = Instantiate(buttonTemplate);

                    VideoSourceUIButton script = button.GetComponent<VideoSourceUIButton>();
                    script._Init(this, i, source.SourceName);
                    contentButtons[i] = script;

                    button.transform.SetParent(buttonRoot.transform);

                    RectTransform rt = button.GetComponent<RectTransform>();
                    rt.localScale = Vector3.one;
                    rt.localRotation = Quaternion.identity;
                    rt.localPosition = Vector3.zero;
                }

                if (contentRoot)
                {
                    GameObject content = Instantiate(template.gameObject);
                    contentPanels[i] = content;

                    VideoSourceUIBase script = content.GetComponentInChildren<VideoSourceUIBase>();
                    script._SetSource(source);

                    content.transform.SetParent(contentRoot.transform);

                    RectTransform rt = content.GetComponent<RectTransform>();
                    rt.localScale = Vector3.one;
                    rt.localRotation = Quaternion.identity;
                    rt.localPosition = Vector3.zero;
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
            }

            Canvas.ForceUpdateCanvases();

            _Select(firstTemplateSource);
        }

        public void _Select(int index)
        {
            for (int i = 0; i < contentPanels.Length; i++)
            {
                if (contentPanels[i])
                    contentPanels[i].SetActive(i == index);
                if (contentButtons[i])
                    contentButtons[i]._SetActive(i == index);
            }
        }

        public void _SelectActive()
        {
            if (!boundSourceManager || !boundSourceManager.VideoPlayer)
                return;

            VideoUrlSource active = boundSourceManager.VideoPlayer.currentUrlSource;
            if (!active)
                return;

            int activeIndex = boundSourceManager._GetSourceIndex(active);
            if (contentPanels[activeIndex] && contentButtons[activeIndex])
                _Select(activeIndex);
        }
    }
}
