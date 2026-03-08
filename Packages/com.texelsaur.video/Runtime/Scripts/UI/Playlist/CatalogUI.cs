
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CatalogUI : UdonSharpBehaviour
    {
        public GameObject catalogEntryTemplate;
        public bool expandOnStart = true;

        public ScrollRect scrollRect;
        public GameObject layoutGroup;
        public GameObject catalogExpanded;
        public GameObject catalogCollapsed;

        Playlist playlist;
        PlaylistUI playlistUI;

        bool initialized = false;
        bool expanded = false;
        CatalogUIEntry[] entries;
        RectTransform[] entriesRT;

        void Start()
        {
            _Init();
        }

        void _Init()
        {
            if (initialized)
                return;

            entries = new CatalogUIEntry[0];
            entriesRT = new RectTransform[0];

            expanded = expandOnStart;
            _UpdateCatalogVis();

            initialized = true;
        }

        public void _InitFromPlaylist(Playlist playlist, PlaylistUI playlistUI)
        {
            if (!initialized)
                _Init();

            if (entries == null)
                entries = new CatalogUIEntry[0];

            this.playlist = playlist;
            this.playlistUI = playlistUI;

            _UpdateCatalogVis();
            _RebuildList();

            /*if (gameObject.activeInHierarchy)
            {
                SendCustomEventDelayedFrames("_InitUI", 1);
            }*/
        }

        public void _InitUI()
        {
            _RebuildList();
        }

        private void OnEnable()
        {
            if (!initialized)
                _Init();

            if (playlist)
                _UpdateSelected();
        }

        void _UpdateCatalogVis()
        {
            bool hasCatalog = playlist && playlistUI && playlistUI.showCatalog && playlist.Catalog && playlist.Catalog.PlaylistCount > 1;
            if (catalogCollapsed)
                catalogCollapsed.SetActive(hasCatalog && !expanded);
            if (catalogExpanded)
                catalogExpanded.SetActive(hasCatalog && expanded);
        }

        public void _SelectIndex (int index)
        {
            if (playlistUI)
                playlistUI.CatalogPreviewIndex = index;
        }

        public void _ToggleExpand ()
        {
            expanded = !expanded;
            _UpdateCatalogVis();
        }

        public void _OnPreviewChange()
        {
            _UpdateSelected();
        }

        void _RebuildList()
        {
            layoutGroup.SetActive(false);
            _ClearList();

            if (!playlist)
                return;

            _BuildList();
            layoutGroup.SetActive(true);
        }

        void _ClearList()
        {
            int entryCount = layoutGroup.transform.childCount;
            for (int i = 0; i < entryCount; i++)
            {
                GameObject child = layoutGroup.transform.GetChild(i).gameObject;
                child.SetActive(false);
                //Destroy(child);
            }

            //entries = new CatalogUIEntry[0];
        }

        void _UpdateSelected()
        {
            int index = playlistUI.CatalogPreviewIndex;
            if (index < 0)
                index = playlist.CatalogueIndex;

            for (int i = 0; i < entries.Length; i++)
            {
                CatalogUIEntry entry = entries[i];
                if (!entry)
                    continue;

                bool entrySelected = index == i;
                if (entry.Selected != entrySelected)
                    entry.Selected = entrySelected;
            }
        }

        void _BuildList()
        {
            if (!playlist)
                return;

            PlaylistCatalog catalog = playlist.Catalog;
            if (!catalog)
                return;

            int count = catalog.PlaylistCount;

            entries = (CatalogUIEntry[])UtilityTxl.ArrayMinSize(entries, count, typeof(UdonBehaviour));
            entriesRT = (RectTransform[])UtilityTxl.ArrayMinSize(entriesRT, count, typeof(RectTransform));

            for (int i = 0; i < count; i++)
            {
                CatalogUIEntry script = entries[i];
                if (!script)
                {
                    GameObject newEntry = Instantiate(catalogEntryTemplate);
                    script = newEntry.GetComponent<CatalogUIEntry>();
                    script.catalogtUI = this;
                    script.index = i;

                    entries[i] = script;

                    newEntry.transform.SetParent(layoutGroup.transform);
                    RectTransform rt = (RectTransform)newEntry.GetComponent(typeof(RectTransform));
                    rt.localScale = Vector3.one;
                    rt.localRotation = Quaternion.identity;
                    rt.localPosition = Vector3.zero;

                    entriesRT[i] = rt;
                }

                GameObject entry = script.gameObject;
                entry.SetActive(true);

                string title = "";
                PlaylistData pdata = catalog.playlists[i];
                if (pdata)
                    title = pdata.playlistName;

                script.Title = title;
                script.Selected = false;

                if (playlistUI.CatalogPreviewIndex >= 0)
                    script.Selected = playlistUI.CatalogPreviewIndex == i;
                else if (playlist.CatalogueIndex >= 0)
                    script.Selected = playlist.CatalogueIndex == i;
            }
        }
    }
}
