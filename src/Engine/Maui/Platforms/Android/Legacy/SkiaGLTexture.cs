﻿using Android.Content;
using Android.Util;
using System.ComponentModel;
using SKPaintGLSurfaceEventArgs = SkiaSharp.Views.Android.SKPaintGLSurfaceEventArgs;

namespace DrawnUi.Maui
{
    public partial class SkiaGLTexture : SkiaGLTextureView
    {
        private SkiaGLTextureRenderer renderer;

        public SkiaGLTexture(Context context)
            : base(context)
        {
            Initialize();
        }

        public SkiaGLTexture(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Initialize();
        }

        private void Initialize()
        {
            SetEGLContextClientVersion(2);
            SetEGLConfigChooser(8, 8, 8, 8, 0, 8);

            renderer = new InternalRenderer(this);
            SetRenderer(renderer);
        }

        public SKSize CanvasSize => renderer.CanvasSize;

        public GRContext GRContext => renderer.GRContext;

        public event EventHandler<SKPaintGLSurfaceEventArgs> PaintSurface;

        protected virtual void OnPaintSurface(SKPaintGLSurfaceEventArgs e)
        {
            PaintSurface?.Invoke(this, e);
        }


        private class InternalRenderer : SkiaGLTextureRenderer
        {
            private readonly SkiaGLTexture textureView;

            public InternalRenderer(SkiaGLTexture textureView)
            {
                this.textureView = textureView;
            }

            protected override void OnPaintSurface(SKPaintGLSurfaceEventArgs e)
            {
                textureView.OnPaintSurface(e);
            }

        }
    }
}