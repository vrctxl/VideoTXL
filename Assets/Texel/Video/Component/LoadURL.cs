
using Texel;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class LoadURL : UdonSharpBehaviour
{
    public SyncPlayer syncPlayer;
    public VRCUrl url;

    void Start()
    {
        
    }

    public override void Interact()
    {
        _Load();
    }

    public void _Load()
    {
        syncPlayer._ChangeUrl(url);
    }
}
