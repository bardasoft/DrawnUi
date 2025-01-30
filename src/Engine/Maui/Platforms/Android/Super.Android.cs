﻿using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Views;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using Context = Android.Content.Context;
using Platform = Microsoft.Maui.ApplicationModel.Platform;

namespace DrawnUi.Maui.Draw;

public partial class Super
{

    public static Android.App.Activity MainActivity { get; set; }

    private static FrameCallback _frameCallback;

    static bool _loopStarting = false;
    static bool _loopStarted = false;

    public static event EventHandler OnFrame;
    static Looper Looper { get; set; }

    public static void Init(Android.App.Activity activity)
    {
        Initialized = true;

        MainActivity = activity;

        Super.Screen.Density = activity.Resources.DisplayMetrics.Density;

        Super.Screen.WidthDip = activity.Resources.DisplayMetrics.WidthPixels / Super.Screen.Density;
        Super.Screen.HeightDip = activity.Resources.DisplayMetrics.HeightPixels / Super.Screen.Density;

        if (Super.NavBarHeight < 0)
            Super.NavBarHeight = 45; //manual

        //var flags = Activity.Window.Attributes.Flags;

        var isFullscreen = (int)activity.Window.DecorView.SystemUiVisibility & (int)SystemUiFlags.LayoutStable;

        //if (((flags & WindowManagerFlags.TranslucentStatus) == WindowManagerFlags.TranslucentStatus) || isFullscreen>0 || (Activity.Window.StatusBarColor == Android.Graphics.Color.Transparent))
        if (!(isFullscreen > 0))
        {
            Super.StatusBarHeight = GetStatusBarHeight(activity) / Super.Screen.Density;
        }
        else
        {
            Super.StatusBarHeight = 0;
        }

        //VisualDiagnostics.VisualTreeChanged += OnVisualTreeChanged;

        bool isRendering = false;
        object lockFrane = new();




        InitShared();

        //Looper = new(() =>
        //{
        //    OnFrame?.Invoke(null, null);
        //});

        //Looper.StartOnMainThread(120);

        Tasks.StartDelayed(TimeSpan.FromMilliseconds(250), async () =>
        {
            _frameCallback = new FrameCallback((nanos) =>
            {
                if (isRendering)
                    return;
                isRendering = true;
                OnFrame?.Invoke(null, null);
                Choreographer.Instance.PostFrameCallback(_frameCallback);
                isRendering = false;
            });

            while (!_loopStarted)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    lock (lockFrane)
                    {
                        if (_loopStarting)
                            return;

                        _loopStarting = true;

                        if (MainThread.IsMainThread) // Choreographer is available
                        {
                            if (!_loopStarted)
                            {
                                _loopStarted = true;
                                Choreographer.Instance.PostFrameCallback(_frameCallback);
                            }
                        }

                        _loopStarting = false;
                    }
                });

                if (_loopStarted)
                    break;

                await Task.Delay(100);
            }

        });

    }




    /// <summary>
    /// ToDo resolve obsolete for android api 30 and later
    /// </summary>
    /// <param name="activity"></param>
    public static void SetFullScreen(Android.App.Activity activity)
    {
        if (_insetsListener == null)
        {
            _insetsListener = new();

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
            {
                // https://stackoverflow.com/a/33355089/7149454
                var uiOptions = (int)activity.Window.DecorView.SystemUiVisibility;
                uiOptions |= (int)SystemUiFlags.LayoutStable;
                uiOptions |= (int)SystemUiFlags.LayoutFullscreen;
                activity.Window.DecorView.SystemUiVisibility = (StatusBarVisibility)uiOptions;
                activity.Window.SetStatusBarColor(Android.Graphics.Color.Transparent);
                var contentView = activity.FindViewById(Android.Resource.Id.Content);
                if (contentView != null)
                    contentView.SetOnApplyWindowInsetsListener(_insetsListener);
            }
        }
    }

    static InsetsListener _insetsListener;



    public class InsetsListener : Java.Lang.Object, Android.Views.View.IOnApplyWindowInsetsListener
    {
        private WindowInsets _returnInsets;

        public WindowInsets OnApplyWindowInsets(Android.Views.View v, WindowInsets insets)
        {
            //we are saving system insets BEFORE the fullscreen flag was applied
            //and system insets became zero
            if (_returnInsets == null)
            {
                Super.Screen.TopInset = insets.SystemWindowInsetTop / Super.Screen.Density;

                bool invalidate = false;

                if (Super.StatusBarHeight != Super.Screen.TopInset)
                    invalidate = true;

                Super.StatusBarHeight = Super.Screen.TopInset;

                //if (invalidate)
                {
                    //App.Instance.Shell.InvalidateNavBar();
                    InsetsChanged?.Invoke(this, null);
                }

                _returnInsets = insets.ReplaceSystemWindowInsets(
                    insets.SystemWindowInsetLeft,
                    0,
                    insets.SystemWindowInsetRight,
                    insets.SystemWindowInsetBottom
                );
            }
            return _returnInsets;
        }
    }

    public static int GetStatusBarHeight(Context context)
    {

        int statusBarHeight = 0, totalHeight = 0, contentHeight = 0;
        int resourceId = context.Resources.GetIdentifier("status_bar_height", "dimen", "android");
        if (resourceId > 0)
        {
            statusBarHeight = context.Resources.GetDimensionPixelSize(resourceId);
            totalHeight = context.Resources.DisplayMetrics.HeightPixels;
            contentHeight = totalHeight - statusBarHeight;
        }

        return statusBarHeight;
    }

    //public static void ClearImagesCache()
    //{
    //    var glide = Glide.Get(Platform.CurrentActivity);
    //    Task.Run(async () =>
    //    {
    //        glide.ClearDiskCache();
    //    }).ConfigureAwait(false);

    //    MainThread.BeginInvokeOnMainThread(() =>
    //    {
    //        glide.ClearMemory();
    //    });
    //}


    public static void SetNavigationBarColor(
        Microsoft.Maui.Graphics.Color colorBar,
        Microsoft.Maui.Graphics.Color colorSeparator,
        bool darkStatusBarTint)
    {
        if (Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Lollipop)
            return;

        var activity = Platform.CurrentActivity;
        var window = activity.Window;

        window.ClearFlags(WindowManagerFlags.TranslucentNavigation);
        window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);

        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            if (darkStatusBarTint)
            {
                if (Build.VERSION.SdkInt > (BuildVersionCodes)27)
                {
                    if (colorSeparator == Colors.Transparent)
                        window.NavigationBarDividerColor =
                            Microsoft.Maui.Graphics.Color.FromArgb("#FF222222").ToAndroid();
                    else
                        window.NavigationBarDividerColor = colorSeparator.ToAndroid();
                }

                window.SetNavigationBarColor(colorBar.ToAndroid());

                if (Build.VERSION.SdkInt > (BuildVersionCodes)26)
                {
                    // Fetch the current flags.
                    var lFlags = activity.Window.DecorView.SystemUiVisibility;

                    var mask = ~(StatusBarVisibility)SystemUiFlags.LightNavigationBar;

                    // Update the SystemUiVisibility dependening on whether we want a Light or Dark theme.
                    activity.Window.DecorView.SystemUiVisibility = lFlags & mask;
                }
            }
            else
            {
                //todo share everywhere !!!
                if (Build.VERSION.SdkInt > (BuildVersionCodes)27)
                {
                    if (colorSeparator == Colors.Transparent)
                        window.NavigationBarDividerColor =
                            Microsoft.Maui.Graphics.Color.FromArgb("#FFeeeeee").ToAndroid();
                    else
                        window.NavigationBarDividerColor = colorSeparator.ToAndroid();
                }

                window.SetNavigationBarColor(colorBar.ToAndroid());

                if (Build.VERSION.SdkInt > (BuildVersionCodes)26)
                {

                    // Fetch the current flags.
                    var lFlags = activity.Window.DecorView.SystemUiVisibility;
                    // Update the SystemUiVisibility dependening on whether we want a Light or Dark theme.
                    activity.Window.DecorView.SystemUiVisibility =
                        lFlags | (StatusBarVisibility)SystemUiFlags.LightNavigationBar;
                }

            }
        }
    }

    public static void SetWhiteTextStatusBar()
    {
        if (Build.VERSION.SdkInt > Android.OS.BuildVersionCodes.M)
        {

            var activity = Platform.CurrentActivity;
            var window = activity.Window;

            // Fetch the current flags.
            var lFlags = window.DecorView.SystemUiVisibility;

            var mask = ~(StatusBarVisibility)SystemUiFlags.LightStatusBar;

            window.DecorView.SystemUiVisibility = lFlags & mask;
        }
    }

    public static void SetBlackTextStatusBar()
    {
        if (Build.VERSION.SdkInt > Android.OS.BuildVersionCodes.M)
        {
            var activity = Platform.CurrentActivity;
            var window = activity.Window;

            // Fetch the current flags.
            var lFlags = window.DecorView.SystemUiVisibility;
            // Update the SystemUiVisibility dependening on whether we want a Light or Dark theme.
            window.DecorView.SystemUiVisibility = lFlags | (StatusBarVisibility)SystemUiFlags.LightStatusBar;
        }
    }

    public class FrameCallback : Java.Lang.Object, Choreographer.IFrameCallback
    {
        public FrameCallback(Action<long> callback)
        {
            _callback = callback;
        }

        Action<long> _callback;

        public void DoFrame(long frameTimeNanos)
        {
            _callback?.Invoke(frameTimeNanos);
        }

    }

    /// <summary>
    /// Opens web link in native browser
    /// </summary>
    /// <param name="link"></param>
    public static void OpenLink(string link)
    {
        try
        {
            var intent2 = new Intent(Intent.ActionView,
                Android.Net.Uri.Parse(link));
            intent2.AddFlags(ActivityFlags.NewTask);
            Android.App.Application.Context.StartActivity(intent2);
        }
        catch (Exception e)
        {
            Super.Log(e);
        }

    }

    /// <summary>
    /// Lists assets inside the Resources/Raw subfolder
    /// </summary>
    /// <param name="subfolder"></param>
    /// <returns></returns>
    public static IEnumerable<string> ListResources(string subfolder)
    {
        AssetManager assets = Platform.AppContext.Assets;
        string[] files = assets.List(subfolder);
        return files;
    }
}
