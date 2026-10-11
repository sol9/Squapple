using System;

namespace Squapple.Presentation
{
    public interface IInterstitialAds : IDisposable
    {
        bool IsReady { get; }
        bool IsBusy { get; }
        bool PrivacyOptionsRequired { get; }
        void Initialize(Action beforePrivacyForm);
        void Prepare();
        bool TryShow(Action opened, Action closed);
        void ShowPrivacyOptions(Action<bool> completed);
    }
}
