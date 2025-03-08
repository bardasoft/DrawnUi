﻿using System.Windows.Input;

namespace DrawnUi.Maui.Controls;

public class SkiaMauiEditor : SkiaMauiElement, ISkiaGestureListener
{

    public override ISkiaGestureListener ProcessGestures(SkiaGesturesParameters args, GestureEventProcessingInfo apply)
    {
        if (args.Type == TouchActionResult.Up)
        {
            var point = TranslateInputOffsetToPixels(args.Event.Location, apply.childOffset);
            if (!DrawingRect.Contains(point))
            {
                //we got this gesture because we were focused, but it's now outside our bounds, can unfocus
                return base.ProcessGestures(args, apply);
            }
        }

        return this;
    }

    public static readonly BindableProperty MaxLinesProperty = BindableProperty.Create(nameof(MaxLines),
        typeof(int), typeof(SkiaMauiEditor), -1);
    /// <summary>
    /// WIth 1 will behave like an ordinary Entry, with -1 (auto) or explicitly set you get an Editor
    /// </summary>
    public int MaxLines
    {
        get { return (int)GetValue(MaxLinesProperty); }
        set { SetValue(MaxLinesProperty, value); }
    }


#if ONPLATFORM
    public override void SetNativeVisibility(bool state)
    {
        base.SetNativeVisibility(state);

        if (state && IsFocused)
        {
            SetFocusInternal(true);
        }
    }
#endif

    #region CAN LOCALIZE

    public static string ActionGo = "Go";
    public static string ActionNext = "Next";
    public static string ActionSend = "Send";
    public static string ActionSearch = "Search";
    public static string ActionDone = "Done";

    #endregion

    #region EVENTS

    public event EventHandler<string> TextChanged;

    public event EventHandler<bool> FocusChanged;

    public event EventHandler<string> TextSubmitted;

    #endregion

    public override bool WillClipBounds => true;


    protected virtual void MapProps(MauiEditor control)
    {
        var alias = SkiaFontManager.GetRegisteredAlias(this.FontFamily, this.FontWeight);
        control.FontFamily = alias;
        control.FontSize = FontSize;
        control.TextColor = this.TextColor;
        control.ReturnType = this.ReturnType;
        control.Keyboard = this.KeyboardType;
        control.Text = Text;
        //todo customize
        control.Placeholder = this.Placeholder;
    }

    protected virtual Editor GetOrCreateControl()
    {
        if (Control == null)
        {
            var alias = SkiaFontManager.GetRegisteredAlias(this.FontFamily, this.FontWeight);
            Control = new MauiEditor();
            MapProps(Control);
            AdaptControlSize();

            SubscribeToControl(true);

            Content = Control;

            if (IsFocused)
                SetFocusInternal(true);
        }
        return Control;
    }

    public MauiEditor Control { get; protected set; }

    protected void FocusNative()
    {
        //Debug.WriteLine($"[SKiaMauiEntry] Focusing native control..");
        Control.Focus();
    }

    protected void UnfocusNative()
    {
        //Debug.WriteLine($"[SKiaMauiEntry] Unfocusing native control..");
#if IOS || MACCATALYST //dealing with maui differences
        Control?.Unfocus();
#endif
        IsFocused = false;
    }

    public void SetFocus(bool focus)
    {
        if (Control != null)
        {
            if (focus)
            {
                FocusNative();
            }
            else
            {
                UnfocusNative();
            }

            UpdateControl();
        }
    }


    protected virtual void AdaptControlSize()
    {

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (Control != null)
            {
                Control.WidthRequest = this.Width;
                Control.HeightRequest = this.Height;
            }
        });


    }

    public virtual void UpdateControl()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (Control != null)
            {
                MapProps(Control);
                Update();
            }

            AdaptControlSize();
        });
    }

    protected override void OnLayoutChanged()
    {
        base.OnLayoutChanged();

        AdaptControlSize();
    }

    protected void SubscribeToControl(bool subscribe)
    {
        if (Control != null)
        {
            if (subscribe)
            {
                Control.Unfocused += OnControlUnfocused;
                Control.Focused += OnControlFocused;
                Control.TextChanged += OnControlTextChanged;
                Control.Completed += OnControlCompleted;
            }
            else
            {
                Control.Unfocused -= OnControlUnfocused;
                Control.Focused -= OnControlFocused;
                Control.TextChanged -= OnControlTextChanged;
                Control.Completed -= OnControlCompleted;
            }
        }
    }


    private void OnControlCompleted(object sender, EventArgs e)
    {
        if (!LockFocus)
        {
            Control.Unfocus();
        }
        TextSubmitted?.Invoke(this, Text);
        CommandOnSubmit?.Execute(Text);
    }

    private void OnControlTextChanged(object sender, TextChangedEventArgs e)
    {
        this.Text = Control.Text;
    }

    static object lockFocus = new();

    /// <summary>
    /// Invoked by Maui control
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnControlFocused(object sender, FocusEventArgs e)
    {
        //Debug.WriteLine($"[SKiaMauiEntry] Focused by native");
        lock (lockFocus)
        {
            Superview.FocusedChild = this;
        }
    }

    /// <summary>
    /// Invoked by Maui control
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnControlUnfocused(object sender, FocusEventArgs e)
    {
        //Debug.WriteLine($"[SKiaMauiEntry] Unfocused by native");
        lock (lockFocus)
        {
            if (Superview.FocusedChild == this)
            {
                Superview.FocusedChild = null;
            };
        }
    }

    /// <summary>
    /// Called by DrawnUi when the focus changes
    /// </summary>
    /// <param name="focus"></param>
    public bool OnFocusChanged(bool focus)
    {
        lock (lockFocus)
        {
            if (Control != null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (!focus)
                        UnfocusNative();
                    else
                        FocusNative();
                });
            }
            return true;
        }
    }

    public override void OnDisposing()
    {
        if (Control != null)
        {
            SubscribeToControl(false);
            Control.DisposeControlAndChildren();
            Control = null;
        }
        base.OnDisposing();
    }

    public override ScaledSize Measure(float widthConstraint, float heightConstraint, float scale)
    {
        if (IsDisposed || IsDisposing)
            return ScaledSize.Default;

        GetOrCreateControl();

        return base.Measure(widthConstraint, heightConstraint, scale);
    }

    protected void SetFocusInternal(bool value)
    {
        if (Control != null)
        {
            if (!Control.IsFocused && value || Control.IsFocused && !value)
            {
                Tasks.StartDelayed(TimeSpan.FromMilliseconds(100), () =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (value)
                            FocusNative();
                        else
                            UnfocusNative();
                    });
                });
            }
        }
    }

    private static void NeedUpdateControl(BindableObject bindable, object oldvalue, object newvalue)
    {
        if (bindable is SkiaMauiEditor control)
        {
            control.UpdateControl();
        }
    }

    private static void OnControlTextChanged(BindableObject bindable, object oldvalue, object newvalue)
    {
        if (bindable is SkiaMauiEditor control)
        {
            control.TextChanged?.Invoke(control, (string)newvalue);
            control.CommandOnTextChanged?.Execute((string)newvalue);
            control.UpdateControl();
        }
    }

    /// <summary>
    /// TODO
    /// </summary>
    bool LockFocus { get; set; }


    #region   PROPERTIES

    public static readonly BindableProperty KeyboardTypeProperty = BindableProperty.Create(
        nameof(KeyboardType),
        typeof(Keyboard),
        typeof(SkiaMauiEditor),
        Keyboard.Default,
        propertyChanged: NeedUpdateControl);

    [System.ComponentModel.TypeConverter(typeof(Microsoft.Maui.Converters.KeyboardTypeConverter))]
    public Keyboard KeyboardType
    {
        get { return (Keyboard)GetValue(KeyboardTypeProperty); }
        set { SetValue(KeyboardTypeProperty, value); }
    }

    public static readonly BindableProperty FontFamilyProperty = BindableProperty.Create(nameof(FontFamily),
       typeof(string), typeof(SkiaMauiEditor), string.Empty, propertyChanged: NeedUpdateControl);
    public string FontFamily
    {
        get { return (string)GetValue(FontFamilyProperty); }
        set { SetValue(FontFamilyProperty, value); }
    }

    public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
        nameof(TextColor), typeof(Color), typeof(SkiaMauiEditor),
        Colors.GreenYellow,
        propertyChanged: NeedUpdateControl);
    public Color TextColor
    {
        get { return (Color)GetValue(TextColorProperty); }
        set { SetValue(TextColorProperty, value); }
    }

    public static readonly BindableProperty FontWeightProperty = BindableProperty.Create(
        nameof(FontWeight),
        typeof(int),
        typeof(SkiaMauiEditor),
        0,
        propertyChanged: NeedUpdateControl);

    public int FontWeight
    {
        get { return (int)GetValue(FontWeightProperty); }
        set { SetValue(FontWeightProperty, value); }
    }

    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(nameof(FontSize),
        typeof(double), typeof(SkiaMauiEditor), 12.0,
        propertyChanged: NeedUpdateControl);

    public double FontSize
    {
        get { return (double)GetValue(FontSizeProperty); }
        set { SetValue(FontSizeProperty, value); }
    }

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text),
        typeof(string),
        typeof(SkiaMauiEditor),
        default(string),
        BindingMode.TwoWay,
        propertyChanged: OnControlTextChanged);


    public string Text
    {
        get { return (string)GetValue(TextProperty); }
        set { SetValue(TextProperty, value); }
    }

    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
        nameof(Placeholder),
        typeof(string),
        typeof(SkiaMauiEditor),
        default(string),
        propertyChanged: NeedUpdateControl);


    public string Placeholder
    {
        get { return (string)GetValue(PlaceholderProperty); }
        set { SetValue(PlaceholderProperty, value); }
    }

    public static readonly BindableProperty ReturnTypeProperty = BindableProperty.Create(
        nameof(ReturnType),
        typeof(ReturnType),
        typeof(SkiaMauiEditor),
        ReturnType.Default);

    public ReturnType ReturnType
    {
        get { return (ReturnType)GetValue(ReturnTypeProperty); }
        set { SetValue(ReturnTypeProperty, value); }
    }

    public static readonly BindableProperty CommandOnSubmitProperty = BindableProperty.Create(
        nameof(CommandOnSubmit),
        typeof(ICommand),
        typeof(SkiaMauiEditor),
        null);

    public ICommand CommandOnSubmit
    {
        get { return (ICommand)GetValue(CommandOnSubmitProperty); }
        set { SetValue(CommandOnSubmitProperty, value); }
    }

    public static readonly BindableProperty CommandOnFocusChangedProperty = BindableProperty.Create(
        nameof(CommandOnFocusChanged),
        typeof(ICommand),
        typeof(SkiaMauiEditor),
        null);

    public ICommand CommandOnFocusChanged
    {
        get { return (ICommand)GetValue(CommandOnFocusChangedProperty); }
        set { SetValue(CommandOnFocusChangedProperty, value); }
    }

    public static readonly BindableProperty CommandOnTextChangedProperty = BindableProperty.Create(
        nameof(CommandOnTextChanged),
        typeof(ICommand),
        typeof(SkiaMauiEditor),
        null);

    public ICommand CommandOnTextChanged
    {
        get { return (ICommand)GetValue(CommandOnTextChangedProperty); }
        set { SetValue(CommandOnTextChangedProperty, value); }
    }

    public new static readonly BindableProperty IsFocusedProperty = BindableProperty.Create(
        nameof(IsFocused),
        typeof(bool),
        typeof(SkiaMauiEditor),
        false,
        BindingMode.TwoWay,
        propertyChanged: OnNeedChangeFocus);

    private static void OnNeedChangeFocus(BindableObject bindable, object oldvalue, object newvalue)
    {
        if (bindable is SkiaMauiEditor control)
        {
            control.SetFocusInternal((bool)newvalue);
        }
    }

    public new bool IsFocused
    {
        get { return (bool)GetValue(IsFocusedProperty); }
        set { SetValue(IsFocusedProperty, value); }
    }

    #endregion

}