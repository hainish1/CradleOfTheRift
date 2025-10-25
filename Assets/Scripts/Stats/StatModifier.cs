using UnityEngine;
using System;

public abstract class StatModifier : IDisposable
{
    public StatType Type { get; }
    

    public bool MarkedForRemoval { get; private set; }
    public event Action<StatModifier> OnDispose = delegate { };

    readonly CountdownTimer timer;

    protected StatModifier(float duration)
    {
        if (duration <= 0) return; // this means that this particular STAT is PERMANENT, until we remove it

        timer = new CountdownTimer(duration);

        timer.OnTimerStop += () => MarkedForRemoval = true;
        timer.Start();
    }

    public void Update(float deltaTime) => timer?.Tick(deltaTime);

    public abstract void Handle(object sender, Query query);


    public void Dispose()
    {
        
        OnDispose.Invoke(this);
    }

}
