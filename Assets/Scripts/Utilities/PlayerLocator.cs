using UnityEngine;

public static class PlayerLocator
{
    public static GameObject FindPlayerGameObject()
    {
        // 1) try Player tag 
        try
        {
            GameObject tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged != null) return tagged;
        }
        catch (UnityException)
        {
            // Tag doesnt exist
        }

        // Fallback
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer < 0) return null;


        var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (var t in transforms)
        {
            if (t != null && t.gameObject.layer == playerLayer)
                return t.gameObject;
        }
        return null;
    }

    public static T FindPlayerComponent<T>() where T : Component
    {
        var playerGo = FindPlayerGameObject();
        if (playerGo == null) return null;

        var c = playerGo.GetComponent<T>();
        if (c != null) return c;

        c = playerGo.GetComponentInChildren<T>();
        if (c != null) return c;

        return playerGo.GetComponentInParent<T>();
    }
}
