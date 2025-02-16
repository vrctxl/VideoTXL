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

        Regex pattern = new Regex(@"^(http(s|)\:\/\/|)((([a-zA-Z0-9-_]{1,}\.){1,})([a-zA-Z]{1}[a-zA-Z0-9-]{1,}))(:[0-9]{1,}|)(\/[a-zA-Z0-9_~#?\+\&\.\/-=%-]{1,}|)+");
        return pattern.IsMatch(url);
    }
}
