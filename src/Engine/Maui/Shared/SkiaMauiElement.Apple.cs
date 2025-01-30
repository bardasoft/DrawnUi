﻿using CoreGraphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
using UIKit;


namespace DrawnUi.Maui.Draw;

public partial class SkiaMauiElement
{

    public bool ShowSnapshot => false;

    protected virtual void LayoutNativeView(VisualElement element)
    {
        if (element.Handler?.PlatformView is UIView nativeView)
        {
            var visibility = VisualTransformNative.IsVisible && IsNativeVisible ? Visibility.Visible : Visibility.Hidden;
            nativeView.UpdateVisibility(visibility);

            nativeView.ClipsToBounds = true;

            nativeView.Transform = CGAffineTransform.MakeIdentity();
            nativeView.Frame = new CGRect(
                VisualTransformNative.Rect.Left + this.Padding.Left,
                VisualTransformNative.Rect.Top + this.Padding.Top,
                VisualTransformNative.Rect.Width - (this.Padding.Left + this.Padding.Right),
                VisualTransformNative.Rect.Height - (this.Padding.Top + this.Padding.Bottom)
            );

            nativeView.Transform = CGAffineTransform.MakeTranslation(VisualTransformNative.Translation.X, VisualTransformNative.Translation.Y);
            nativeView.Transform = CGAffineTransform.Rotate(nativeView.Transform, VisualTransformNative.Rotation); // Assuming rotation in radians
            nativeView.Transform = CGAffineTransform.Scale(nativeView.Transform, VisualTransformNative.Scale.X, VisualTransformNative.Scale.Y);
            nativeView.Alpha = VisualTransformNative.Opacity;

            //Debug.WriteLine($"Layout Maui : {VisualTransformNative.Opacity} {VisualTransformNative.Translation} {VisualTransformNative.IsVisible}");
        }
    }

    public virtual void SetNativeVisibility(bool state)
    {
        IsNativeVisible = state;
        LayoutNativeView(Element);
    }

    protected void RemoveMauiElement(Element element)
    {
        if (element == null)
            return;

        var layout = Superview.Handler?.PlatformView as UIView;
        if (layout != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (element.Handler?.PlatformView is UIView nativeView)
                {
                    nativeView.RemoveFromSuperview();
                    if (element is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    //todo destroy child handler?
                }
                else
                if (NativeView != null)
                {
                    NativeView.RemoveFromSuperview();
                    if (NativeView is IDisposable disposable)
                    {
                        NativeView.Dispose();
                    }
                    NativeView = null;
                    //todo destroy child handler?
                }
            });
        }
    }


    protected virtual void SetupMauiElement(VisualElement element)
    {
        if (element == null)
            return;

        IViewHandler handler = Superview.Handler;
        if (handler != null)
        {

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (element.Handler == null)
                {
                    //create handler
                    var childHandler = element.ToHandler(handler.MauiContext);
                }
                //add native view to canvas
                var view = element.Handler?.PlatformView as UIView;
                var layout = Superview.Handler?.PlatformView as UIView;
                if (layout != null)
                {
                    this.NativeView = view;
                    layout.AddSubview(view);
                }

                LayoutNativeView(Element);
            });

        }

    }

    public UIView NativeView { get; set; }
}
