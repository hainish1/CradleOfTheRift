using System;
using UnityEngine;


// I AM ONLY GOING TO BE USING THIS TO JUST TEST OUT MY STATS THINGS
public abstract class Pickup : MonoBehaviour, IVisitor
{

    protected abstract void ApplyPickupEffect(Entity entity);

    public void Visit<T>(T visitable) where T : Component, IVisitable
    {
        if (visitable is Entity entity)
        {
            ApplyPickupEffect(entity);
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        other.GetComponent<IVisitable>()?.Accept(this);
        Destroy(gameObject);
    }
}
