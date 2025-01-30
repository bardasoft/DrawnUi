﻿using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Platform;
using Microsoft.Maui.Platform;
using System.Runtime.CompilerServices;
using Visibility = Microsoft.UI.Xaml.Visibility;

namespace DrawnUi.Maui.Views
{

    public partial class DrawnView
    {

        /// <summary>
        /// To optimize rendering and not update controls that are inside storyboard that is offscreen or hidden
        /// Apple - UI thread only !!!
        /// If you set 
        /// </summary>
        /// <param name="element"></param>
        public void CheckElementVisibility(VisualElement element)
        {
            NeedCheckParentVisibility = false;
            IsHiddenInViewTree = !GetIsVisibleWithParent(this);
        }

        protected virtual void OnSizeChanged()
        {

            if (Handler != null) //this is basically for clipping SkiaMauiElement
            {
                var layout = Handler.PlatformView as ContentPanel;
                layout.Clip = new Microsoft.UI.Xaml.Media.RectangleGeometry
                {
                    Rect = new Windows.Foundation.Rect(0, 0, Width, Height)
                };
            }

            Update();
        }

        public virtual void SetupRenderingLoop()
        {
#if !LEGACY
            Super.OnFrame -= OnSuperFrame;
            Super.OnFrame += OnSuperFrame;
#endif
        }

        protected virtual void PlatformHardwareAccelerationChanged()
        {

        }




#if LEGACY

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckCanDraw()
        {
            if (UpdateLocked && StopDrawingWhenUpdateIsLocked)
                return false;

            return CanvasView != null
                   && !IsRendering
                   && IsDirty
                   && IsVisible;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void UpdatePlatform()
        {
            IsDirty = true;
            if (!OrderedDraw && CheckCanDraw())
            {
                OrderedDraw = true;
                InvalidateCanvas();
            }
        }



#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void UpdatePlatform()
        {
            IsDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckCanDraw()
        {
            return
               CanvasView != null && this.Handler != null && this.Handler.PlatformView != null
               && !OrderedDraw
               && !CanvasView.IsDrawing
               && IsDirty
               && !(UpdateLocked && StopDrawingWhenUpdateIsLocked)
               && IsVisible && Super.EnableRendering;
        }

#endif

        private long test;
        private void OnSuperFrame(object sender, EventArgs e)
        {
            if (CheckCanDraw())
            {
                OrderedDraw = true;
                if (NeedCheckParentVisibility)
                {
                    CheckElementVisibility(this);
                }
                if (CanDraw)
                {
                    //Debug.WriteLine($"UPDATE {Tag}");
                    CanvasView?.Update();
                }
                else
                {
                    OrderedDraw = false;
                }
            }
        }

        protected virtual void DisposePlatform()
        {
            Super.OnFrame -= OnSuperFrame;
        }

    }
}
