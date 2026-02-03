
using UdonSharp;
using UnityEngine;

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
            boundSourceManager._Register(SourceManager.EVENT_SOURCE_ADDED, this, nameof(_InternalOnSourceAddRemove));
            boundSourceManager._Register(SourceManager.EVENT_SOURCE_REMOVED, this, nameof(_InternalOnSourceAddRemove));

            _Rebuild();
        }

        public void _InternalOnSourceAddRemove()
        {
            _Rebuild();
        }

        public void _Rebuild()
        {
            contentPanels = new GameObject[boundSourceManager.Count];
            contentButtons = new VideoSourceUIButton[boundSourceManager.Count];

            int firstTemplateSource = -1;

            if (buttonRoot)
            {
                while (buttonRoot.transform.childCount > 0)
                {
                    var child = buttonRoot.transform.GetChild(0);
                    child.parent = null;
                    Destroy(child.gameObject);
                }
            }

            if (contentRoot)
            {
                while (contentRoot.transform.childCount > 0)
                {
                    var child = contentRoot.transform.GetChild(0);
                    child.parent = null;
                    Destroy(child.gameObject);
                }
            }

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

                if (buttonRoot && buttonTemplate)
                {
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
