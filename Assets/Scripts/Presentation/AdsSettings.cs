using UnityEngine;

namespace Squapple.Presentation
{
    [CreateAssetMenu(menuName = "Squapple/Ads Settings")]
    public sealed class AdsSettings : ScriptableObject
    {
        [SerializeField] private bool enableAds;
        [SerializeField] private bool useTestAds = true;
        [SerializeField] private bool initializeAdsInEditor;

        public bool EnableAds => enableAds;
        public bool UseTestAds => useTestAds;
        public bool InitializeAdsInEditor => initializeAdsInEditor;
    }
}
