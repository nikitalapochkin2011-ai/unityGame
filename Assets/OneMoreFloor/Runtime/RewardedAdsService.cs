using System;
using System.Collections.Concurrent;
using UnityEngine;
#if OMF_ADMOB && !UNITY_EDITOR
using GoogleMobileAds.Api;
#endif

namespace OneMoreFloor
{
    [Serializable]
    public sealed class RewardedAdsConfiguration
    {
        public bool EditorSimulation=true;
        public bool UseTestAds=true;
        public string AndroidRewardedAdUnitId="";
        public string IosRewardedAdUnitId="";
    }
    public enum RewardedPlacement { DailyWheel, ContinueRun }

    // The production backend is enabled only after the official Google Mobile Ads
    // Unity plugin is installed and OMF_ADMOB is added to Scripting Define Symbols.
    public sealed class RewardedAdsService : MonoBehaviour
    {
        readonly ConcurrentQueue<Action> callbacks=new ConcurrentQueue<Action>();
        RewardedAdsConfiguration configuration;
        volatile bool destroyed;
        bool showing;
        public bool Showing { get { return showing; } }
#if OMF_ADMOB && !UNITY_EDITOR
        RewardedAd loaded;
        bool initialized, loading;
        float nextLoad;
        string UnitId
        {
            get
            {
#if UNITY_ANDROID
                return configuration.UseTestAds ? "ca-app-pub-3940256099942544/5224354917" : configuration.AndroidRewardedAdUnitId;
#elif UNITY_IOS
                return configuration.UseTestAds ? "ca-app-pub-3940256099942544/1712485313" : configuration.IosRewardedAdUnitId;
#else
                return "";
#endif
            }
        }
#endif
        public bool Ready
        {
            get
            {
                if(destroyed || showing || configuration==null) return false;
#if UNITY_EDITOR
                return configuration.EditorSimulation;
#elif OMF_ADMOB
                return loaded!=null && loaded.CanShowAd();
#else
                return false;
#endif
            }
        }
        public void Configure(RewardedAdsConfiguration value)
        {
            configuration=value ?? new RewardedAdsConfiguration();
#if OMF_ADMOB && !UNITY_EDITOR
            if(string.IsNullOrWhiteSpace(UnitId)) return;
            try { MobileAds.Initialize(_=>Post(()=> { initialized=true; Load(); })); }
            catch(Exception error) { Debug.LogWarning("Rewarded ads initialization failed: "+error.Message); }
#endif
        }
        public bool Show(RewardedPlacement placement,Action<bool> completed)
        {
            if(!Ready) return false;
            showing=true;
            var current=new RewardedRequest(reward=>
            {
                showing=false;
#if OMF_ADMOB && !UNITY_EDITOR
                loaded?.Destroy(); loaded=null; nextLoad=Time.unscaledTime+1;
#endif
                completed?.Invoke(reward);
            });
#if UNITY_EDITOR
            // An explicit Editor-only simulation, never a production ad or hidden grant.
            bool earned=UnityEditor.EditorUtility.DisplayDialog(Loc.T("ads.testTitle"),Loc.T("ads.testDetail"),Loc.T("ads.testEarn"),Loc.T("language.cancel"));
            Post(()=> { if(earned) current.Earn(); current.Close(); });
#elif OMF_ADMOB
            var ad=loaded;
            ad.OnAdFullScreenContentClosed+=()=>Post(()=>current.Close());
            ad.OnAdFullScreenContentFailed+=error=>Post(()=>current.Fail());
            try { ad.Show(reward=>Post(()=>current.Earn())); }
            catch(Exception error) { Debug.LogWarning("Rewarded ad show failed: "+error.Message); current.Fail(); }
#endif
            return true;
        }
        void Post(Action action) { if(!destroyed) callbacks.Enqueue(action); }
        void Update()
        {
            while(callbacks.TryDequeue(out var action)) { if(!destroyed) action(); }
#if OMF_ADMOB && !UNITY_EDITOR
            if(initialized && !showing && loaded==null && !loading && Time.unscaledTime>=nextLoad) Load();
#endif
        }
#if OMF_ADMOB && !UNITY_EDITOR
        void Load()
        {
            if(destroyed || loading || !initialized || string.IsNullOrWhiteSpace(UnitId)) return;
            loading=true;
            try
            {
                RewardedAd.Load(UnitId,new AdRequest(),(ad,error)=>
                {
                    if(destroyed) { ad?.Destroy(); return; }
                    Post(()=>
                    {
                        loading=false;
                        if(error!=null || ad==null) { ad?.Destroy(); nextLoad=Time.unscaledTime+20; return; }
                        loaded?.Destroy(); loaded=ad;
                    });
                });
            }
            catch(Exception error)
            { loading=false; nextLoad=Time.unscaledTime+20; Debug.LogWarning("Rewarded ad load failed: "+error.Message); }
        }
#endif
        void OnDestroy()
        {
            destroyed=true;
#if OMF_ADMOB && !UNITY_EDITOR
            loaded?.Destroy(); loaded=null;
#endif
        }
    }
}
