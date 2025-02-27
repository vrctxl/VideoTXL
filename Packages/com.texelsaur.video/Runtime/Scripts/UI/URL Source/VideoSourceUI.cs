﻿
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
        GameObject[] contentButtons;

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
            contentButtons = new GameObject[boundSourceManager.Count];
            
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

                if (buttonRoot && buttonTemplate) {
                    GameObject button = Instantiate(buttonTemplate);
                    contentButtons[i] = button;

                    VideoSourceUIButton script = button.GetComponent<VideoSourceUIButton>();
                    script._Init(this, i, source.SourceName);

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

            _Select(0);
        }

        public void _Select(int index)
        {
            for (int i = 0; i < contentPanels.Length; i++)
            {
                if (contentPanels[i])
                    contentPanels[i].SetActive(i == index);
            }
        }
    }
}
