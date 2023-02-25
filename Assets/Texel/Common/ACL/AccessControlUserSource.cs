using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Texel;

namespace Texel
{
    public class AccessControlUserSource : EventBase
    {
        public const int EVENT_REVALIDATE = 0;
        public const int EVENT_COUNT = 1;

        protected override int EventCount { get => EVENT_COUNT; }

        void Start()
        {
            _EnsureInit();
        }

        public virtual bool _ContainsName(string name)
        {
            return false;
        }
    }
}
