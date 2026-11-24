using UnityEngine;
using UnityEngine.Advertisements;

public class ads : MonoBehaviour, IUnityAdsListener
{

    string gameId = "3653578";
    string myPlacementId = "video";
    bool testMode = false;

    public static bool showAdBool = true;

    // Initialize the Ads listener and service:
    void Start()
    {
        // Ads disabled
    }


    // Implement IUnityAdsListener interface methods:
    public void OnUnityAdsDidFinish(string placementId, ShowResult showResult)
    {
    }

    public void OnUnityAdsReady(string placementId)
    {
        // Ads disabled
    }

    public void OnUnityAdsDidError(string message)
    {
        // Log the error.
    }

    public void OnUnityAdsDidStart(string placementId)
    {
        // Optional actions to take when the end-users triggers an ad.
    }
}