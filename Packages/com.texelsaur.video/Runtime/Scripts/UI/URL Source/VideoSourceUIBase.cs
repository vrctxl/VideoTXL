using System.Collections;
using System.Collections.Generic;
using UdonSharp;
using UnityEngine;

namespace Texel
{
    public abstract class VideoSourceUIBase : UdonSharpBehaviour
    {
        public virtual bool _CompatibleSource (VideoUrlSource source)
        {
            return false;
        }

        public virtual void _SetSource (VideoUrlSource source) { }
    }
}
