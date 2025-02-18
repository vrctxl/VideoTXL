using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;

namespace Texel
{
    public abstract class VideoUrlListSource : VideoUrlSource
    {
        public const int EVENT_LIST_CHANGE = VideoUrlSource.EVENT_COUNT + 0;
        public const int EVENT_TRACK_CHANGE = VideoUrlSource.EVENT_COUNT + 1;
        protected new const int EVENT_COUNT = VideoUrlSource.EVENT_COUNT + 2;

        protected override int EventCount => EVENT_COUNT;

        public override VideoUrlListSource ListSource
        {
            get { return this; }
        }

        public virtual string ListName
        {
            get { return ""; }
        }

        public virtual short CurrentIndex
        {
            get { return -1; }
            protected set { }
        }

        public virtual short Count
        {
            get { return 0; }
        }

        public virtual VRCUrl _GetTrackURL(int index)
        {
            return null;
        }

        public virtual VRCUrl _GetTrackQuestURL(int index)
        {
            return null;
        }

        public virtual string _GetTrackName(int index)
        {
            return null;
        }

        public virtual bool _Enqueue(int index)
        {
            return false;
        }

        protected void _EventTrackChange()
        {
            if (sourceManager)
                sourceManager._OnSourceTrackChange(sourceIndex);

            _UpdateHandlers(EVENT_TRACK_CHANGE);
        }
    }
}
