
using System.Text;
using System.Text.RegularExpressions;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class UrlInfoResolver : EventBase
    {
        [Tooltip("Log debug statements to a world object")]
        [SerializeField] internal DebugLog debugLog;
        [SerializeField] internal bool vrcLogging = false;
        [SerializeField] internal bool eventLogging = false;
        [SerializeField] internal bool lowLevelLogging = false;

        DataDictionary typeCache;
        DataDictionary infoCache;
        DataDictionary errorCache;

        public const int EVENT_URL_INFO = 0;
        public const int EVENT_URL_ERROR = 1;
        const int EVENT_COUNT = 2;

        public const int TYPE_UNKNOWN = 0;
        public const int TYPE_YOUTUBE = 1;
        const int TYPE_COUNT = 2;

        void Start()
        {
            _EnsureInit();
        }

        protected override int EventCount => EVENT_COUNT;

        protected override void _Init()
        {
            base._Init();

            typeCache = new DataDictionary();
            infoCache = new DataDictionary();
            errorCache = new DataDictionary();
        }

        public bool _HasInfo(VRCUrl url)
        {
            return _HasInfo(url.Get());
        }

        public bool _HasInfo(string url)
        {
            return infoCache.ContainsKey(url);
        }

        public DataDictionary _GetInfo(VRCUrl url)
        {
            return _GetInfo(url.Get());
        }

        public DataDictionary _GetInfo(string url)
        {
            if (infoCache.TryGetValue(url, out DataToken value))
            {
                if (value.TokenType == TokenType.DataDictionary)
                    return value.DataDictionary;
            }

            return null;
        }

        public string _GetError(VRCUrl url)
        {
            return _GetError(url.Get());
        }

        public string _GetError(string url)
        {
            if (errorCache.TryGetValue(url, out DataToken value))
            {
                if (value.TokenType == TokenType.String)
                    return value.String;
            }

            return null;
        }

        public int _GetUrlType(VRCUrl url)
        {
            return _GetUrlType(url.Get());
        }

        public int _GetUrlType(string url)
        {
            if (typeCache.TryGetValue(url, out DataToken value))
            {
                if (value.TokenType == TokenType.Int)
                    return value.Int;
            }

            int type = TYPE_UNKNOWN;
            if (URLUtil.IsYoutubeUrl(url))
                type = TYPE_YOUTUBE;

            typeCache.SetValue(url, type);
            return type;
        }

        public string _GetTitle(DataDictionary info)
        {
            return _GetInfoString(info, "t");
        }

        public string _GetAuthor(DataDictionary info)
        {
            return _GetInfoString(info, "a");
        }

        public string _GetFormatted(VRCUrl url)
        {
            DataDictionary info = _GetInfo(url);
            return _GetFormatted(url.Get(), info);
        }

        public string _GetFormatted(string url, DataDictionary info)
        {
            if (info == null)
                return "";

            string formatted = _GetInfoString(info, "f");
            if (formatted != null)
                return formatted;

            string source = _GetInfoString(info, "s");
            string id = _GetInfoString(info, "i");
            string title = _GetInfoString(info, "t");
            string author = _GetInfoString(info, "a");

            formatted = _Format(url, source, id, title, author);
            if (formatted != null)
                info.SetValue("f", formatted);

            return formatted;
        }

        string _GetInfoString(DataDictionary info, DataToken key)
        {
            if (info.TryGetValue(key, out DataToken value))
            {
                if (value.TokenType == TokenType.String)
                    return value.String;
            }

            return null;
        }

        public bool _ResolveInfo(VRCUrl url)
        {
            if (_HasInfo(url))
                return false;

            int type = _GetUrlType(url);
            if (type == TYPE_UNKNOWN)
                return false;

            VRCStringDownloader.LoadUrl(url, (UdonBehaviour)(Component)this);
            return true;
        }

        public bool _AddInfo(VRCUrl url, string title = null, string author = null)
        {
            if (_HasInfo(url))
                return false;

            DataDictionary info = new DataDictionary();

            int type = _GetUrlType(url);
            string typecode = _GetTypeCode(type);
            if (typecode != "")
                info.SetValue("s", typecode);

            if (title != null && title != "")
                info.SetValue("t", title);

            if (author != null && author != "")
                info.SetValue("a", author);

            if (type == TYPE_YOUTUBE)
            {
                string id = URLUtil.GetYoutubeID(url);
                if (id != null && id != "")
                    info.SetValue("i", id);
            }

            infoCache.SetValue(url.Get(), info);
            _UpdateHandlers(EVENT_URL_INFO, url);

            return true;
        }

        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            _DebugLog($"String load bytes={result.ResultBytes.Length}");
            errorCache.Remove(result.Url.Get());

            DataDictionary info = _ParseInfo(result.Url, result.Result);
            if (info != null && info.Count > 0)
                infoCache.SetValue(result.Url.Get(), info);

            _UpdateHandlers(EVENT_URL_INFO, result.Url);
        }

        public override void OnStringLoadError(IVRCStringDownload result)
        {
            _DebugError(result.Error);
            errorCache.SetValue(result.Url.Get(), result.Error);

            _UpdateHandlers(EVENT_URL_ERROR, result.Url);
        }

        protected DataDictionary _ParseInfo(VRCUrl url, string rawdata)
        {
            int type = _GetUrlType(url);
            if (type == TYPE_YOUTUBE)
                return _ParseYoutube(url, rawdata);
            else
                return new DataDictionary();

        }

        protected DataDictionary _ParseYoutube(VRCUrl url, string rawdata)
        {
            string typecode = _GetTypeCode(TYPE_YOUTUBE);

            DataDictionary info = new DataDictionary();
            info.SetValue("s", typecode);

            string id = URLUtil.GetYoutubeID(url);
            if (id != null)
                info.SetValue("i", id);

            Regex titlePattern = new Regex(@"<title>(.*)</title>");
            Match titleMatch = titlePattern.Match(rawdata);
            string title = titleMatch.Success ? titleMatch.Groups[1].Value : null;
            if (title != null)
            {
                title = _ReplaceEntities(title);
                Regex stripPattern = new Regex(@"\s+-\s+YouTube$");
                title = stripPattern.Replace(title, "");
                info.SetValue("t", title);
            }

            Regex authorPattern = new Regex(@"([\'""])ownerChannelName\1\s*:\s*\1((\\\1|.)*?)\1");
            Match authorMatch = authorPattern.Match(rawdata);
            string author = authorMatch.Success ? authorMatch.Groups[2].Value : null;
            if (author != null)
            {
                author = _ReplaceEntities(author);
                info.SetValue("a", author);
            }

            string formatted = _Format(url.Get(), typecode, id, title, author);
            info.SetValue("f", formatted);

            _DebugLog($"Parsed {typecode} i={id}, t={title}, a={author}");

            return info;
        }

        protected virtual string _ReplaceEntities(string text)
        {
            text = text.Replace("&#34;", "\"");
            text = text.Replace("&#38;", "&");
            text = text.Replace("&#39;", "'");
            text = text.Replace("&amp;", "&");

            return text;
        }

        protected string _Format(string url, string s, string i, string t, string a)
        {
            string main = t != null ? t : i;
            if (a != null)
                main = main == null ? a : $"{main} - {a}";

            if (main == null && url != null)
                main = url;

            if (s != null)
                main = $"[{s}] {main}";

            return main;
        }

        protected virtual string _GetTypeCode(int type)
        {
            if (type == TYPE_YOUTUBE)
                return "YT";
            return "";
        }

        void _DebugLog(string message)
        {
            if (vrcLogging)
                Debug.Log("[VideoTXL:InfoResolver] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("InfoResolver", message);
        }

        void _DebugError(string message, bool force = false)
        {
            if (vrcLogging || force)
                Debug.LogError("[VideoTXL:InfoResolver] " + message);
            if (Utilities.IsValid(debugLog))
                debugLog._Write("InfoResolver", message);
        }

        void _DebugLowLevel(string message)
        {
            if (lowLevelLogging)
                _DebugLog(message);
        }
    }
}
