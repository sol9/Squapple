using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace Squapple.Presentation
{
    public sealed class AdMobInterstitial : IInterstitialAds
    {
        public const string AndroidAdUnitId = "ca-app-pub-8213531068089130/4983077327";
        private const string AndroidTestAdUnitId = "ca-app-pub-3940256099942544/1033173712";
        private const string IosTestAdUnitId = "ca-app-pub-3940256099942544/4411468910";
        private readonly string _adUnitId;
        private InterstitialAd _loaded;
        private InterstitialAd _showing;
        private Action _beforePrivacyForm;
        private bool _disposed;
        private bool _started;
        private bool _initialized;
        private bool _initializing;
        private bool _loading;
        private bool _privacyBusy;
        private bool _consentChecked;
        private int _generation;
        private double _loadedAt;
        private double _retryAfter;

        public AdMobInterstitial(bool useTestAds)
        {
            var test = useTestAds || Application.isEditor || Debug.isDebugBuild;
            if (Application.platform == RuntimePlatform.IPhonePlayer)
                _adUnitId = test ? IosTestAdUnitId : null; // Production iOS IDs are not configured yet.
            else if (Application.platform == RuntimePlatform.Android || Application.isEditor)
                _adUnitId = test ? AndroidTestAdUnitId : AndroidAdUnitId;
        }

        public bool IsBusy => _privacyBusy || _showing != null;
        public bool PrivacyOptionsRequired => !_disposed && _consentChecked &&
            ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
        public bool IsReady => !_disposed && !IsBusy && _loaded != null &&
            Time.realtimeSinceStartupAsDouble - _loadedAt < 3600 &&
            ConsentInformation.CanRequestAds() && _loaded.CanShowAd();

        public void Initialize(Action beforePrivacyForm)
        {
            if (_disposed || _started || _adUnitId == null)
                return;
            _started = true;
            _beforePrivacyForm = beforePrivacyForm;
            // Consent callbacks need a Unity dispatcher before the ad SDK is initialized.
            MobileAdsEventExecutor.Initialize();
            ConsentInformation.Update(new ConsentRequestParameters(), error => OnMainThread(() =>
            {
                if (error != null)
                {
                    _consentChecked = true;
                    Debug.LogWarning("Ad consent update failed: " + error.Message);
                    InitializeAdsIfAllowed();
                    return;
                }
                if (ConsentInformation.ConsentStatus == ConsentStatus.Required)
                {
                    _privacyBusy = true;
                    _beforePrivacyForm?.Invoke();
                }
                ConsentForm.LoadAndShowConsentFormIfRequired(formError => OnMainThread(() =>
                {
                    _consentChecked = true;
                    _privacyBusy = false;
                    if (formError != null)
                        Debug.LogWarning("Ad consent form failed: " + formError.Message);
                    InitializeAdsIfAllowed();
                }));
            }));
        }

        private void InitializeAdsIfAllowed()
        {
            if (!ConsentInformation.CanRequestAds() || _adUnitId == null)
                return;
            if (_initialized)
            {
                Prepare();
                return;
            }
            if (_initializing)
                return;
            _initializing = true;
            MobileAds.Initialize(status => OnMainThread(() =>
            {
                _initializing = false;
                _initialized = status != null;
                Prepare();
            }));
        }

        public void Prepare()
        {
            if (_disposed || !_initialized || IsBusy || _loading || IsReady ||
                !ConsentInformation.CanRequestAds() || Time.realtimeSinceStartupAsDouble < _retryAfter)
                return;
            _loaded?.Destroy();
            _loaded = null;
            _loading = true;
            var generation = _generation;
            InterstitialAd.Load(_adUnitId, new AdRequest(), (ad, error) =>
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (_disposed || generation != _generation)
                    {
                        ad?.Destroy();
                        return;
                    }
                    _loading = false;
                    if (error != null || ad == null)
                    {
                        ad?.Destroy();
                        _retryAfter = Time.realtimeSinceStartupAsDouble + 30;
                        Debug.LogWarning("Interstitial unavailable: " + error?.GetMessage());
                        return;
                    }
                    _loaded = ad;
                    _loadedAt = Time.realtimeSinceStartupAsDouble;
                }));
        }

        public bool TryShow(Action opened, Action closed)
        {
            if (!IsReady)
                return false;
            var ad = _loaded;
            _loaded = null;
            _showing = ad;
            var finished = false;
            var didOpen = false;
            void Complete()
            {
                if (finished)
                    return;
                finished = true;
                _showing = null;
                ad.Destroy();
                _retryAfter = Time.realtimeSinceStartupAsDouble + 30;
                closed();
            }
            ad.OnAdFullScreenContentOpened += () => OnMainThread(() =>
            {
                if (finished || didOpen)
                    return;
                didOpen = true;
                opened();
            });
            ad.OnAdFullScreenContentClosed += () => OnMainThread(Complete);
            ad.OnAdFullScreenContentFailed += error => OnMainThread(() =>
            {
                Debug.LogWarning("Interstitial failed to show: " + error.GetMessage());
                Complete();
            });
            try
            {
                ad.Show();
            }
            catch (Exception error)
            {
                Debug.LogWarning("Interstitial failed to show: " + error.Message);
                Complete();
            }
            return true;
        }

        public void ShowPrivacyOptions(Action<bool> completed)
        {
            if (_disposed || IsBusy || !PrivacyOptionsRequired)
            {
                completed(false);
                return;
            }
            _privacyBusy = true;
            _beforePrivacyForm?.Invoke();
            _generation++;
            _loading = false;
            _loaded?.Destroy();
            _loaded = null;
            ConsentForm.ShowPrivacyOptionsForm(error => OnMainThread(() =>
            {
                _privacyBusy = false;
                if (error != null)
                    Debug.LogWarning("Privacy options failed: " + error.Message);
                InitializeAdsIfAllowed();
                completed(error == null);
            }));
        }

        private void OnMainThread(Action action) => MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {
            if (!_disposed)
                action();
        });

        public void Dispose()
        {
            _disposed = true;
            _generation++;
            _loaded?.Destroy();
            _loaded = null;
            _showing?.Destroy();
            _showing = null;
            _beforePrivacyForm = null;
        }
    }
}
