using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using VRC.SDKBase;

public static class URLUtil
{
    public static bool EmptyUrl(VRCUrl url)
    {
        if (url == null)
            return true;

        return EmptyUrl(url.Get());
    }

    public static bool EmptyUrl(string str)
    {
        if (str == null)
            return true;

        return str.Length == 0;
    }

    public static bool WellFormedUrl(VRCUrl url)
    {
        if (url == null)
            return false;

        return WellFormedUrl(url.Get());
    }

    public static bool WellFormedUrl(string url)
    {
        if (EmptyUrl(url))
            return false;

        Regex pattern = new Regex(@"^([a-zA-Z]+\:\/\/|)((([a-zA-Z0-9-_]{1,}\.){1,})([a-zA-Z]{1}[a-zA-Z0-9-]{1,}))(:[0-9]{1,}|)(\/[a-zA-Z0-9_~#?\+\&\.\/-=%-]{1,}|)+");
        return pattern.IsMatch(url);
    }

    public static string GetYoutubeID(VRCUrl url)
    {
        if (url == null)
            return null;

        return GetYoutubeID(url.Get());
    }

    public static string GetYoutubeID(string url)
    {
        string pattern = @"(?:https?:\/\/)?(?:www\.)?youtu\.?be(?:\.com)?\/?.*(?:watch|embed)?(?:.*v=|v\/|\/)([\w\-_]+)\&?";
        Match match = Regex.Match(url, pattern);
        if (match.Success)
            return match.Groups[1].Value;

        return null;
    }

    public static bool IsYoutubeUrl(VRCUrl url)
    {
        if (url == null)
            return false;

        return IsYoutubeUrl(url.Get());
    }

    public static bool IsYoutubeUrl(string url)
    {
        return GetYoutubeID(url) != null;
    }

    public static bool IsTwitchUrl(VRCUrl url)
    {
        if (url == null)
            return false;

        return IsTwitchUrl(url.Get());
    }

    public static bool IsTwitchUrl(string url)
    {
        string pattern = @"(?:https?:\/\/)?(?:www\.)?twitch\.tv\/([\w\-_]+)";
        Match match = Regex.Match(url, pattern);
        return match.Success;
    }
}
