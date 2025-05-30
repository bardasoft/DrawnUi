﻿using DrawnUi.Draw;
using System.ComponentModel;

namespace DrawnUi.Controls;

/// <summary>
/// Base ISkiaCell implementation
/// </summary>
public class SkiaDrawnCell : SkiaLayout, ISkiaCell
{
    protected virtual void SetContent(object ctx)
    {

    }

    public virtual void OnScrolled()
    {

    }

    public virtual TouchActionEventHandler LongPressingHandler => (sender, args) =>
    {
        args.PreventDefault = true;
    };

    private bool _isAttaching;

    public INotifyPropertyChanged Context { get; protected set; }

    public override void OnDisposing()
    {
        base.OnDisposing();

        FreeContext();
    }

    protected virtual void FreeContext()
    {
        Context = null;
    }

    protected virtual void AttachContext(object ctx)
    {
        if (ctx != null)
        {
            Context = ctx as INotifyPropertyChanged;
        }
    }

    private object LockContext = new();


    public override void ApplyBindingContext()
    {
        lock (LockContext)
        {
            base.ApplyBindingContext();

            var ctx = BindingContext;

            if (ctx != Context && !_isAttaching)
            {
                _isAttaching = true;

                FreeContext();

                if (Context == null)
                {
                    SetContent(ctx);
                    AttachContext(ctx);
                }
                _isAttaching = false;
            }
        }
    }






}
