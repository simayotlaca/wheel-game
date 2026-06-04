using System;
using System.Collections.Generic;

namespace VertigoWheel
{
internal interface IRunEventReader
{
    void Subscribe<T>(Action<T> handler);
    void Unsubscribe<T>(Action<T> handler);
}

internal struct RunStateChangedEvent
{
    internal RunState current_state;

    internal RunStateChangedEvent(RunState current_state)
    {
        this.current_state = current_state;
    }
}

internal struct RunZoneChangedEvent
{
    internal int current_zone;

    internal RunZoneChangedEvent(int current_zone)
    {
        this.current_zone = current_zone;
    }
}

internal struct RunDeathHitEvent { }

internal struct RunCurrencyChangedEvent
{
    internal int cash;
    internal int gold;

    internal RunCurrencyChangedEvent(int cash, int gold)
    {
        this.cash = cash;
        this.gold = gold;
    }
}

internal struct RunPendingClearedEvent { }

internal struct ExitFlowStateChangedEvent
{
    internal ExitFlowState current_state;

    internal ExitFlowStateChangedEvent(ExitFlowState current_state)
    {
        this.current_state = current_state;
    }
}

internal class RunEventBus : IRunEventReader
{
    private Dictionary<Type, List<Delegate>> handlers = new Dictionary<Type, List<Delegate>>();

    void IRunEventReader.Subscribe<T>(Action<T> handler)
    {
        if (handler != null)
        {
            Type event_type = typeof(T);
            List<Delegate> event_handlers = GetHandlers(event_type);
            event_handlers.Add(handler);
        }
    }

    void IRunEventReader.Unsubscribe<T>(Action<T> handler)
    {
        if (handler != null)
        {
            Type event_type = typeof(T);
            if (handlers.ContainsKey(event_type))
            {
                List<Delegate> event_handlers = handlers[event_type];
                event_handlers.Remove(handler);
                if (event_handlers.Count == 0)
                {
                    handlers.Remove(event_type);
                }
            }
        }
    }

    internal void Publish<T>(T evt)
    {
        Type event_type = typeof(T);
        if (handlers.ContainsKey(event_type))
        {
            Delegate[] event_handlers = handlers[event_type].ToArray();
            for (int i = 0; i < event_handlers.Length; i++)
            {
                ((Action<T>)event_handlers[i]).Invoke(evt);
            }
        }
    }

    private List<Delegate> GetHandlers(Type event_type)
    {
        if (!handlers.ContainsKey(event_type))
        {
            handlers[event_type] = new List<Delegate>();
        }

        return handlers[event_type];
    }
}

internal class RunEventPass
{
    private interface IBinding
    {
        void Release(IRunEventReader events);
    }

    private class Binding<T> : IBinding
    {
        private Action<T> handler;

        internal Binding(Action<T> handler)
        {
            this.handler = handler;
        }

        void IBinding.Release(IRunEventReader events)
        {
            events.Unsubscribe(handler);
        }
    }

    private IRunEventReader events;
    private List<IBinding> bindings = new List<IBinding>();

    internal RunEventPass(IRunEventReader events)
    {
        this.events = events;
    }

    internal void Subscribe<T>(Action<T> handler)
    {
        events.Subscribe(handler);
        bindings.Add(new Binding<T>(handler));
    }

    internal void ReleaseAll()
    {
        for (int i = bindings.Count - 1; i >= 0; i--)
        {
            bindings[i].Release(events);
        }
        bindings.Clear();
    }
}
}
