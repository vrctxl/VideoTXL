﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Texel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ScreenPropertyMap : UdonSharpBehaviour
    {
        [Tooltip("The name of the shader property holding the main screen texture")]
        public string screenTexture;

        [Header("Optional Properties")]
        [Tooltip("The name of the shader property that indicates the source is AVPro-based.  AVPro sources should have gamma applied for all platforms and flip the image on the Y axis on PC/VR.")]
        public string avProCheck;
        [Tooltip("The name of the shader property that indicates the screen should be flipped on the Y axis")]
        public string invertY;
        [Tooltip("The name of the shader property that indicates gamma correction should be applied to the image")]
        public string applyGamma;
        [Tooltip("The name of the shader property that sets the screen fit enum value (0=fit, 1=fit-h, 2=fit-w, 3=stretch)")]
        public string screenFit;
        [Tooltip("The name of the shader property that indicates the intended aspect ratio of the screen texture, overriding ratio calculated from the actual texture dimensions.")]
        public string aspectRatio;

        [Header("Optional CRT Properties")]
        [Tooltip("The name of the shader property that indicates the intended aspect ratio of the target screen mesh.")]
        public string targetAspectRatio;
        [Tooltip("The name of the shader property that indicates the material should handle double buffering.  Only applicable to CRT materials.")]
        public string doubleBuffered;

        private void Start()
        {

        }
    }
}
