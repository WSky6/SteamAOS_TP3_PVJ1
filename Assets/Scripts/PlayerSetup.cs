using System.Collections;
using UnityEngine;

public class PlayerSetup : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (FindFirstObjectByType<PlayerSetup>() != null) return;

        GameObject go = new GameObject("PlayerSetup");
        DontDestroyOnLoad(go);
        go.AddComponent<PlayerSetup>();
    }

    IEnumerator Start()
    {
        WaitForSecondsRealtime wait = new WaitForSecondsRealtime(0.3f);

        while (true)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");

            if (p != null)
            {
                if (p.GetComponent<PlayerWeapon>() == null) p.AddComponent<PlayerWeapon>();
                if (p.GetComponent<PlayerHealth>() == null) p.AddComponent<PlayerHealth>();
                if (p.GetComponent<PlayerDeathZones>() == null) p.AddComponent<PlayerDeathZones>();
            }

            yield return wait;
        }
    }
}