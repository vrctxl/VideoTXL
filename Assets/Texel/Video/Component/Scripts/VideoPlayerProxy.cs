
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDKBase;
using VRC.Udon;

public class VideoPlayerProxy : UdonSharpBehaviour
{
    [NonSerialized]
    public short playerSource;
    [NonSerialized]
    public short playerSourceOverride;
    [NonSerialized]
    public int playerState;
    [NonSerialized]
    public bool paused;
    [NonSerialized]
    public bool syncing;
    [NonSerialized]
    public VideoError lastErrorCode;
    [NonSerialized]
    public bool seekableSource;
    [NonSerialized]
    public float trackDuration;
    [NonSerialized]
    public float trackPosition;
    [NonSerialized]
    public float trackTarget;
    [NonSerialized]
    public bool locked;
    [NonSerialized]
    public bool repeatPlaylist;
    [NonSerialized]
    public VRCUrl currentUrl;
    [NonSerialized]
    public VRCUrl lastUrl;

    bool init = false;
    GameObject[] playerStateHandlers;
    GameObject[] trackingHandlers;
    GameObject[] lockHandlers;
    GameObject[] infoHandlers;
    GameObject[] playlistHandlers;

    public void _Init()
    {
        if (init)
            return;

        playerStateHandlers = new GameObject[0];
        trackingHandlers = new GameObject[0];
        lockHandlers = new GameObject[0];
        infoHandlers = new GameObject[0];
        playlistHandlers = new GameObject[0];
        init = true;
    }

    public void _RegisterEventHandler(GameObject handler, string eventName)
    {
        if (!Utilities.IsValid(handler))
            return;

        if (!init)
            _Init();

        switch (eventName)
        {
            case "_VideoStateUpdate":
                playerStateHandlers = _RegsiterEventHandlerIntoList(playerStateHandlers, handler);
                break;
            case "_VideoTrackingUpdate":
                trackingHandlers = _RegsiterEventHandlerIntoList(trackingHandlers, handler);
                break;
            case "_VideoInfoUpdate":
                infoHandlers = _RegsiterEventHandlerIntoList(infoHandlers, handler);
                break;
            case "_VideoLockUpdate":
                lockHandlers = _RegsiterEventHandlerIntoList(lockHandlers, handler);
                break;
            case "_VideoPlaylistUpdate":
                playlistHandlers = _RegsiterEventHandlerIntoList(playlistHandlers, handler);
                break;
            default:
                return;
        }

        Debug.Log($"[VideoTXL:VideoPlayerProxy] registering new event handler for {eventName}");
    }

    GameObject[] _RegsiterEventHandlerIntoList(GameObject[] handlerList, GameObject handler)
    {
        if (!Utilities.IsValid(handlerList))
            handlerList = new GameObject[0];

        foreach (GameObject h in handlerList)
        {
            if (h == handler)
                return handlerList;
        }

        GameObject[] newHandlers = new GameObject[handlerList.Length + 1];
        for (int i = 0; i < handlerList.Length; i++)
            newHandlers[i] = handlerList[i];

        newHandlers[handlerList.Length] = handler;
        handlerList = newHandlers;

        return handlerList;
    }

    public void _EmitStateUpdate()
    {
        _EmitEvent(playerStateHandlers, "_VideoStateUpdate");
    }

    public void _EmitTrackingUpdate()
    {
        _EmitEvent(trackingHandlers, "_VideoTrackingUpdate");
    }

    public void _EmitLockUpdate()
    {
        _EmitEvent(lockHandlers, "_VideoLockUpdate");
    }

    public void _EmitInfoUpdate()
    {
        _EmitEvent(infoHandlers, "_VideoInfoUpdate");
    }

    public void _EmitPlaylistUpdate()
    {
        _EmitEvent(playlistHandlers, "_VideoPlaylistUpdate");
    }

    void _EmitEvent(GameObject[] handlerList, string eventName)
    {
        if (!Utilities.IsValid(handlerList))
            return;

        foreach (GameObject handler in handlerList)
        {
            if (!Utilities.IsValid(handler))
                continue;

            UdonBehaviour script = (UdonBehaviour)handler.GetComponent(typeof(UdonBehaviour));
            if (Utilities.IsValid(script))
                script.SendCustomEvent(eventName);
        }
    }
}
