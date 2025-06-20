﻿# DrawnUI Fluent C# Extensions - Developer Guide

version 1.1a

This guide covers the essential patterns of DrawnUI fluent extensions for drawn controls.
Not all methods are listed here, as extensions are evolving.


## Table of Contents

1. [Core Philosophy](#core-philosophy)
2. [Actions and References](#actions-and-references)
3. [Property Observation](#property-observation)
4. [Common Patterns](#common-patterns)
5. [Layout Extensions](#layout-extensions)
6. [Gesture Handling](#gesture-handling)
7. [Control Helpers](#control-helpers)
8. [Best Practices](#best-practices)
9. [Troubleshooting](#troubleshooting)

## Core Philosophy

Use these extensions to build your UI with C# using fluent chaining for clean, readable code.  

- **Fluent chaining** - Readable, declarative UI code
- **Performance first** - Runs efficiently, does not require UI thread
- **Automatic cleanup** - No memory leaks, subscriptions auto-dispose
- **Framework-independent** - Multi-way property observations without framework bindings

**.NET MAUI** notice: 
1. While you can still create bindings in code, these extensions allow you to use MVVM without traditional bindings. 
2. Drawn controls do not require their properties to be accessed on UI-thread.

```csharp
new SkiaLabel()
{
    UseCache = SkiaCacheType.Operations
}
.Observe(Model, (label, prop) => 
{
    if (prop.IsEither(nameof(BindingContext), nameof(Model.DisplayName)))
    {
        label.Text = Model.DisplayName;
    }
})
.OnTapped((me) => 
{ 
    Model.CommandAddRequest.Execute(null); 
})
.CenterX()
.SetHeight(44);
```

## Actions and References

You can execute conditional code during construction and initialization of controls, 
and access controls by assigned references.

Execute simple code within the fluent chain:

```csharp
new SkiaMauiEditor()
{
    MaxLines = 1,
    HeightRequest = 32,
    Placeholder = "...",
    Padding = new Thickness(0, 2, 0, 4),
}
.Adapt(me =>
{
    if (_multiline)
    {
        me.MaxLines = -1;
        me.HeightRequest = 180;
    }
})
```

### Assign References

You can declare a variable holding a reference to a control and assign it during control creation.

```csharp
//declared in some scope
SkiaLabel labelText;

//assign variable during control creation
new SkiaLabel("Hello World!").Assign(out labelText)
```

### Getting References During Construction

The variable you set with `Assign` will be available after the fluent chain has been completely built. 
If you need to access them for initialization, use the `Initialize` method.  
For observing variables that are still null at the time of UI construction use access by action inside the `Observe`:

```csharp
SkiaLabel statusLabel;
SkiaButton button;
int counter = 0;

var layout = new VStack
{
    Children = new List<SkiaControl>
    {           
        new SkiaLabel("0")
            .Assign(out statusLabel),

        new SkiaLabel()             
        .Observe(() => statusLabel, (me, prop) => //notice access by action!
        {
            if (prop.IsEither(nameof(BindingContext), nameof(Text)))
            {
                me.Text = $"Label text changed to: {statusLabel.Text}"";
            }
        }),

        new SkiaButton("Click Me")
        {
            BackgroundColor = Colors.Grey
        }
        .Assign(out button) //  <--- assign
        .OnTapped(me => { statusLabel.Text = $"{++counter}"; })
    }
}                   
.Initialize(me =>
{
    //assigned variables <--- access
    button.BackgroundColor = Colors.Green;
});
``` 

### Parent Assignment

You would normally include children like this:

```csharp
new VStack
{
    Children = new List<SkiaControl>
    {           
        new SkiaLabel("0"),
        new SkiaButton("Click Me")
    }
}    
```

Or you might prefer this approach:

```csharp
new VStack()
    .WithChildren(
        new SkiaLabel("0"),
        new SkiaButton("Click Me")
    )
```

In case you need to assign a single control to a parent properly:

```csharp
var child = new SkiaLabel("I'm a child")
    .AssignParent(parentLayout)  // Adds to parent automatically
    .CenterX();
```

or

```csharp
var child = new SkiaLabel("I'm a child");
parentLayout.AddSubview(child);
```

To properly remove children by code:

```csharp
layout.Children.RemoveAt(0); //remove the first one

layout.RemoveSubview(child); //remove child

layout.ClearChildren(); //clear them all
```

## Property Observation

**Key Points:**
- Observe properties of any `INotifyPropertyChanged` source
- Always check for `nameof(BindingContext)` for initial default value setup
- Extension will automatically unsubscribe/cleanup when control is disposed
- Can use `propertyName.IsEither(prop1, prop2)` for multiple properties

### 1. `.Observe(vm, callback)` - Basic Pattern

Observes property changes on any `INotifyPropertyChanged` source:

```csharp
//BindingMode.OneWay alternative
new SkiaLabel()
.Observe(Model, (label, prop) => {
    if (prop.IsEither(nameof(BindingContext), nameof(Model.DisplayName)))
    {
        //get value from viewmodel      
        label.Text = Model.DisplayName;
    }
});
```

### 2. `.ObserveSelf(callback)` - Self Observation

Observes the control's own property changes:

```csharp
//BindingMode.OneWayToSource alternative
wheelPicker
    .ObserveSelf((me, prop) => {          
        if (prop.IsEither(nameof(BindingContext), nameof(me.SelectedIndex)))
        {
            //set viewmodel property
            viewModel.CurrentIndex = me.SelectedIndex;
        }
    });
```

### 3. `.ObserveBindingContext<TControl, TViewModel>(callback)` - Typed ViewModel

Type-safe observation of the control's BindingContext:

```csharp
new SkiaLabel()
.ObserveBindingContext<SkiaLabel, ChatViewModel>((me, vm, prop) => {
    if (prop.IsEither(nameof(BindingContext), nameof(vm.MessageCount)))
    {
        me.Text = $"Messages: {vm.MessageCount}";
    }
});
```

```csharp
// Sync slider value with viewModel
var slider = new SkiaSlider()

     //simulate BindingMode.TwoWay

    .ObserveBindingContext<SkiaSlider, MyViewModel>((sld, vm, prop) => {
        bool attached = prop == nameof(BindingContext);
        if (attached || prop == nameof(vm.Volume))
        {
            if (Math.Abs(sld.Value - vm.Volume) > 0.01) // Prevent loops
                sld.Value = vm.Volume;
        }
    })
    .ObserveSelf((sld, prop) => {
        if (prop == nameof(sld.Value))
        {
            if (BindingContext is MyViewModel vm)
                vm.Volume = sld.Value;
        }
    });
```

### 4. `.ObserveDeep()` - Deep Property Watching

For nested property chains like `vm.Country.SkiaFlag`:

```csharp
new SkiaSvg()
{
    Aspect = TransformAspect.Fill,
    Margin = new Microsoft.Maui.Thickness(0, 0, 4, 0),
    HeightRequest = 24,
    VerticalOptions = LayoutOptions.Center,
    WidthRequest = 34
}
.ObserveDeep<SkiaSvg, LoginSmsViewModel, Country, string>(
    vm => vm.Country,  
    nameof(LoginSmsViewModel.Country),  
    country => country.SkiaFlag,  
    nameof(Country.SkiaFlag),  
    (me, value) => 
    {
        me.SvgString = value;
    }
)
```

At the same time, this method is provided for backward compatibility with existing codebases. 
For this example scenario, it is safer and faster to call `OnPropertyChanged(nameof(Country))` in the viewmodel 
after potentially changing any of its properties so that the control can react to this event only to apply all linked properties.
This would be similar to calling `UpdateState` inside Blazor after changing the Country model.
 

## Common Patterns

### Observe injected ViewModel

When you inject your ViewModel in the page/screen constructor you can observe a fixed reference:

```csharp

public class MyScreen : AppScreen //subclassed custom SkiaLayout
{
    public readonly InjectedViewModel Model;

    public ScreenChat(InjectedViewModel vm)
    {
        Model = vm;
        BindingContext = Model;
        ...

        CreateContent();
    }

}

protected void CreateContent()
{
    HorizontalOptions = LayoutOptions.Fill;
    VerticalOptions = LayoutOptions.Fill;
    Type = LayoutType.Column;
    Spacing = 0;
    Padding = 16;
    Children = new List<SkiaControl>()
    {
        new SkiaLabel()
        .Observe(Model, (me, prop) => //observe Model reference directly
        {
            bool attached = prop == nameof(BindingContext);
            if (attached || prop == nameof(Model.Title))
            {
                me.Text = Model.Title;
            }
            if (attached || prop == nameof(Model.Error))
            {
                me.TextColor = Model.Error ? Colors.Red : Colors.Black;
            }
        }),
        ...
    };
        
    }

```

### Observe another control property

```csharp

SkiaLabel labelTitle;

new SkiaLabel()
.Observe(Model, (me, prop) =>
{
    bool attached = prop == nameof(BindingContext);
    if (attached || prop == nameof(Model.Title))
    {
        me.Text = Model.Title;
    }
})
.Assign(out labelTitle),

new SkiaLabel()
.Observe(() => labelTitle, (me, prop) =>
{
    bool attached = prop == nameof(BindingContext);
    if (attached || prop == nameof(Text))
    {
        me.Text = $"The title was: {labelTitle.Text}"";
    }
}),

```

### Two-Way bindings

```csharp
new WheelPicker()
.ObserveSelf((me, prop) =>
{
    if (prop.IsEither(nameof(BindingContext), nameof(WheelPicker.SelectedIndex)))
    {
        IndexIso = me.SelectedIndex; //update local property from control
    }
})
.Observe(this, (me, prop) =>
{
    if (prop.IsEither(nameof(BindingContext), nameof(IndexIso)))
    {
        me.SelectedIndex = IndexIso; //update control property from local
    }
}),
```

### Reactive Button States

```csharp
var submitButton = new SkiaButton("Submit")
    .ObserveBindingContext<SkiaButton, MyViewModel>((btn, vm, prop) => {
        bool attached = prop == nameof(BindingContext);
        if (attached || prop == nameof(vm.CanSubmit))
        {
            btn.IsEnabled = vm.CanSubmit;
            btn.Opacity = vm.CanSubmit ? 1.0 : 0.5;
        }
        if (attached || prop == nameof(vm.IsReadOnly))
        {
            btn.IsVisible = !vm.IsReadOnly;             
        }
    })
    .OnTapped((me) => { viewModel.SubmitCommand.Execute(null); });
```

### Conditional Visibility

```csharp
var errorView = new SkiaLabel()
    .ObserveBindingContext<SkiaLabel, MyViewModel>((lbl, vm, prop) => {
        bool attached = prop == nameof(BindingContext);
        if (attached || prop == nameof(vm.HasError))
        {
            lbl.IsVisible = vm.HasError;
            lbl.Text = vm.ErrorMessage;
        }
    });
```

### Loading States

```csharp
var loadingIndicator = new ActivityIndicator()
    .ObserveBindingContext<ActivityIndicator, MyViewModel>((indicator, vm, prop) => {
        bool attached = prop == nameof(BindingContext);
        if (attached || prop == nameof(vm.IsLoading))
        {
            indicator.IsVisible = vm.IsLoading;
            indicator.IsRunning = vm.IsLoading;
        }
    });
```

### List Content Management

```csharp
var listView = new CellsStack()
    .ObserveBindingContext<CellsStack, MyViewModel>((list, vm, prop) => {
        bool attached = prop == nameof(BindingContext);
        
        if (attached || prop == nameof(vm.HasData))
        {
            list.ItemsSource = vm.HasData ? vm.Items : null;
        }
        
        if (attached || prop == nameof(vm.HasError))
        {
            if (vm.HasError)
                list.ItemsSource = null;
        }
    });
```

### Two-Way Property Synchronization

```csharp
// Sync slider value with viewModel
var slider = new SkiaSlider()
    .ObserveBindingContext<SkiaSlider, MyViewModel>((sld, vm, prop) => {
        bool attached = prop == nameof(BindingContext);
        if (attached || prop == nameof(vm.Volume))
        {
            if (Math.Abs(sld.Value - vm.Volume) > 0.01) // Prevent loops
                sld.Value = vm.Volume;
        }
    })
    .ObserveSelf((sld, prop) => {
        if (prop == nameof(sld.Value))
        {
            if (BindingContext is MyViewModel vm)
                vm.Volume = sld.Value;
        }
    });
```


## Layout and UI Extensions

### Positioning and Sizing

```csharp
new SkiaLabel("Hello")
    .Center()           // Centers both X and Y
    .CenterX()          // Centers horizontally only
    .CenterY()          // Centers vertically only
    .Fill()             // Fills both directions
    .FillX()            // Fills horizontally
    .FillY()            // Fills vertically
    .StartX()           // Aligns to start horizontally
    .EndY()             // Aligns to end vertically
    .SetHeight(100)     // Sets height
    .SetWidth(200)      // Sets width
    .SetMargin(16)      // Uniform margin
    .SetMargin(16, 8)   // Horizontal, vertical
    .SetMargin(16, 8, 16, 8); // Left, top, right, bottom
```

### Layout-Specific Extensions

```csharp
new SkiaLayout()
    .WithPadding(16)              // Uniform padding
    .WithPadding(16, 8)           // Horizontal, vertical
    .WithChildren(child1, child2) // Add multiple children
    .WithContent(singleChild);    // For IWithContent containers
```

### Grid Layout Extensions

#### Basic Grid Setup

```csharp
new SkiaLayout()
    .WithRowDefinitions("Auto,*,Auto")      // Auto, Star, Auto
    .WithColumnDefinitions("100,*,2*,Auto") // Fixed, Star, 2*Star, Auto
```

#### Positioning Children

```csharp
child1.WithRow(0).WithColumn(0);           // Position at (0,0)
child2.SetGrid(1, 0);                      // Position at column 1, row 0
child3.SetGrid(0, 1, 2, 1);               // column, row, columnSpan, rowSpan
child4.WithRowSpan(2).WithColumnSpan(3);  // Spanning
```

#### Complete Grid Example

```csharp
new SkiaLayout()
{
    Type = LayoutType.Grid,
    Children = new List<SkiaControl>
    {
        new SkiaLabel("Header").WithRow(0).WithColumnSpan(2),
        new SkiaLabel("Left").WithRow(1).WithColumn(0),
        new SkiaLabel("Right").WithRow(1).WithColumn(1),
        new SkiaButton("Footer").WithRow(2).WithColumnSpan(2)
    }
}
.WithRowDefinitions("Auto,*,Auto")
.WithColumnDefinitions("*,*");
```

## Gesture Handling

### Basic Gestures

Can add gesture handling effects to any control:

```csharp
anyControl
.OnTapped(btn => 
{
    viewModel.CommandExecute(null);
})
.OnLongPressing(btn => 
{
    ShowContextMenu();
});
```

### Advanced Gesture Handling

Controls that implement `ISkiaGestureListener` (deriving from `SkiaLayout` etc) can use this extension. 
Technically, this calls a delegate `OnGestures` action before executing the `base.ProcessGestures` code.  
The same logic can be implemented by subclassing a control and overriding `ProcessGestures`.  
Return this control reference if you consumed a gesture, return `null` if not.  
The UP gesture should be marked as consumed ONLY for specific scenarios; please return `null` if unsure.  

```csharp
layout.WithGestures((me, args, apply) => {
    ISkiaGestureListener consumed = null;
    
    //your logic
    if (args.Type == TouchActionResult.Panning)
    {
        ...
        consumed = this; //we consumed this one
    }

    //return consumed state
    if (consumed != null && args.Type != TouchActionResult.Up)
    {
        return consumed; //do not let others use this gesture anymore
    }
    return null; //will send this gesture to other controls
});
```

## Control Helpers

You might want to create helpers to be reused within your app, for example:

```csharp
//define once
public class AppButton : SkiaButton
{
    public AppButton(string caption)
    {
        UseCache = SkiaCacheType.Image;
        HorizontalOptions = LayoutOptions.Center;
        WidthRequest = 250;
        HeightRequest = 44;
        Text = caption;
    }
}

//Use everywhere
new AppButton("Click Me")
```

For convenience, some helpers come out of the box:

* `SkiaLayer` - absolute layout, children will be super-positioned, create layers and anything. This is a `SkiaLayout` with horizontal Fill by default.
* `VStack` - Vertical stack, like MAUI VerticalStackLayout. This is a `SkiaLayout` type `Column` with horizontal Fill by default.
* `HStack` - Horizontal stack, like MAUI HorizontalStackLayout. This is a `SkiaLayout` type `Row`.
* `SkiaStack` - A powerful flexible control that arranges children in a responsive way according to available size. This is a `SkiaLayout` type `Wrap` with horizontal Fill by default.
* `SkiaGrid` - MAUI Grid alternative to use rows and columns at will. If you are used to a MAUI grid with a single row/col just to position items one over the other, please use `SkiaLayer` instead!


## Best Practices

### Always Check for BindingContext

```csharp
// ✅ CORRECT
.ObserveBindingContext<Control, ViewModel>((ctrl, vm, prop) => {
    bool attached = prop == nameof(BindingContext);
    if (attached || prop == nameof(vm.MyProperty))
    {
        // Handle both initial setup and property changes
    }
});

// ❌ WRONG - misses initial setup
.ObserveBindingContext<Control, ViewModel>((ctrl, vm, prop) => {
    if (prop == nameof(vm.MyProperty)) // Only triggers on changes
    {
        // Will miss initial value!
    }
});
```

### Use IsEither for Multiple Properties

```csharp
// ✅ CORRECT
if (prop.IsEither(nameof(BindingContext), nameof(vm.Prop1), nameof(vm.Prop2)))

// ❌ VERBOSE
if (prop == nameof(BindingContext) || prop == nameof(vm.Prop1) || prop == nameof(vm.Prop2))
```

### Prevent Circular Updates

```csharp
// ✅ CORRECT - prevents infinite loops
.ObserveSelf((control, prop) => {
    if (prop == nameof(control.Value))
    {
        if (Math.Abs(viewModel.Value - control.Value) > 0.01)
            viewModel.Value = control.Value;
    }
});
```

### Chain Related Operations

```csharp
new SkiaButton("Save")
    .SetHeight(44)
    .CenterX()
    .SetMargin(16, 8)
    .ObserveBindingContext<SkiaButton, MyViewModel>((btn, vm, prop) => {
        // Reactive logic
    })
    .OnTapped((me) => 
    {   
        // Action logic
    });
```

## Troubleshooting

### Problem: Observer Not Triggering

**Symptoms:** UI doesn't update when ViewModel properties change

**Solutions:**
1. Ensure ViewModel implements `INotifyPropertyChanged`
2. Check that property either has a static bindable property or calls `OnPropertyChanged()` in the setter.

```csharp
// ✅ Make sure ViewModel raises PropertyChanged
public class MyViewModel : INotifyPropertyChanged
{
    private string _name;
    public string Name 
    { 
        get => _name; 
        set 
        { 
            if (value == _name)
                return;
            _name = value; 
            OnPropertyChanged(); // Must call this!
        } 
    }
}
```

3. Check that property names match exactly; use `nameof()`.
4. Ensure all your overrides, if any, of `void OnPropertyChanged([CallerMemberName] string propertyName = null)` have a `[CallerMemberName]` attribute.
5. Verify you're checking for `nameof(BindingContext)` for initial setup.
 
### Problem: Null Reference Exceptions

**Symptoms:** Crashes when accessing ViewModel properties

**Solutions:**

1. Make sure you are not accessing an assigned control reference from `Adapt`; use `Initialize` instead.
2. Check that the viewmodel was created and set. 

### Problem: Performance Issues

**Symptoms:** UI stuttering or slow updates

**Solutions:**
1. Always use cache for layers of controls:
   * Do NOT cache scrolls/heavily animated controls and above
   * `UseCache = SkiaCacheType.Operations` for labels and svg
   * `UseCache = SkiaCacheType.Image` for complex layouts, buttons etc
   * `UseCache = SkiaCacheType.ImageComposite` for complex layouts where a region changes while others remain static, like a stack with different user-handled controls.
   * `UseCache = SkiaCacheType.ImageDoubleBuffered` for equally sized recycled cells. Will show old cache while preparing new one in background.
   * `UseCache = SkiaCacheType.GPU` for small static overlays like headers, navbars.
2. Check that you do not have logs spamming the console on every rendering frame.

## Advanced Patterns

### Custom Extension Method

You can easily create your own methods (propose a PR if you think it could serve everyone!).

Example:

```csharp
public static T SetupLoadingStates<T, TViewModel>(this T control) 
    where T : SkiaControl 
    where TViewModel : INotifyPropertyChanged
{
    return control.ObserveBindingContext<T, TViewModel>((ctrl, vm, prop) => {
        // Common loading state logic
        bool attached = prop == nameof(BindingContext);
        if (attached || prop == nameof(vm.IsLoading))
        {
            ctrl.Opacity = vm.IsLoading ? 0.5 : 1.0;
        }
    });
}

// Usage
new MyControl().SetupLoadingStates<MyControl, MyViewModel>();
```
