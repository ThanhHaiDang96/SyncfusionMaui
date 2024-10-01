using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SyncfusionIssue.Behaviors;

public class BehaviorBase<T> : Behavior<T> where T : BindableObject
{
    public T? AssociatedObject { get; private set; }

    protected override void OnAttachedTo(T bindable)
    {
        base.OnAttachedTo(bindable);
        AssociatedObject = bindable;

        if (bindable.BindingContext != null)
        {
            BindingContext = bindable.BindingContext;
        }

        bindable.BindingContextChanged += OnBindingContextChanged;
    }

    protected override void OnDetachingFrom(T bindable)
    {
        base.OnDetachingFrom(bindable);
        bindable.BindingContextChanged -= OnBindingContextChanged;
        AssociatedObject = null;
    }

    void OnBindingContextChanged(object? sender, EventArgs e)
    {
        OnBindingContextChanged();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        BindingContext = AssociatedObject?.BindingContext;
    }
}

public class EventToCommandBehavior : BehaviorBase<VisualElement>
{
    private Delegate? _eventHandler;

    public static readonly BindableProperty EventNameProperty = BindableProperty.Create("EventName", typeof(string), typeof(EventToCommandBehavior), null, propertyChanged: OnEventNameChanged);
    public static readonly BindableProperty CommandProperty = BindableProperty.Create("Command", typeof(ICommand), typeof(EventToCommandBehavior), null);
    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create("CommandParameter", typeof(object), typeof(EventToCommandBehavior), null);
    public static readonly BindableProperty InputConverterProperty = BindableProperty.Create("Converter", typeof(IValueConverter), typeof(EventToCommandBehavior), null);

    public string EventName
    {
        get => (string)GetValue(EventNameProperty);
        set => SetValue(EventNameProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public IValueConverter? Converter
    {
        get => (IValueConverter)GetValue(InputConverterProperty);
        set => SetValue(InputConverterProperty, value);
    }


    protected override void OnAttachedTo(VisualElement bindable)
    {
        base.OnAttachedTo(bindable);
        RegisterEvent(EventName);
    }
    protected override void OnDetachingFrom(VisualElement bindable)
    {
        base.OnDetachingFrom(bindable);
        DeregisterEvent(EventName);
    }

    void RegisterEvent(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        if (AssociatedObject != null)
        {
            var eventInfo = AssociatedObject.GetType().GetRuntimeEvent(name);
            if (eventInfo == null)
            {
                throw new ArgumentException($"EventToCommandBehavior: Can't register the '{EventName}' event.");
            }
            var methodInfo = typeof(EventToCommandBehavior).GetTypeInfo().GetDeclaredMethod("OnEvent");
            _eventHandler = methodInfo?.CreateDelegate(eventInfo.EventHandlerType!, this);
            eventInfo.AddEventHandler(AssociatedObject, _eventHandler);
        }

    }

    void DeregisterEvent(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        if (_eventHandler == null)
        {
            return;
        }
        var eventInfo = AssociatedObject?.GetType().GetRuntimeEvent(name);
        if (eventInfo == null)
        {
            throw new ArgumentException($"EventToCommandBehavior: Can't de-register the '{EventName}' event.");
        }
        eventInfo.RemoveEventHandler(AssociatedObject, _eventHandler);
        _eventHandler = null;
    }

    void OnEvent(object sender, object eventArgs)
    {
        if (Command == null)
        {
            return;
        }

        var obj = (sender, eventArgs);
        object resolvedParameter;
        if (CommandParameter != null)
        {
            resolvedParameter = CommandParameter;
        }
        else if (Converter != null)
        {
            resolvedParameter = Converter.Convert(obj, typeof(object), AssociatedObject, null!)!;
        }
        else
        {
            resolvedParameter = obj;
        }

        if (Command.CanExecute(resolvedParameter))
        {
            Command.Execute(resolvedParameter);
        }
    }

    static void OnEventNameChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var behavior = (EventToCommandBehavior)bindable;

        var oldEventName = (string)oldValue;
        var newEventName = (string)newValue;

        behavior.DeregisterEvent(oldEventName);
        behavior.RegisterEvent(newEventName);
    }
}

