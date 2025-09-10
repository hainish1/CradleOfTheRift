using System;
using Unity.VisualScripting;
using UnityEngine;

public class WizardStaff : Weapon
{
    public ParticleSystem burnParticles;
    public override void Fire()
    {
        if (!canAttack)
        {
            return;
        }
        
        print("Weapon is fired!");
        burnParticles.Play();
        StartCoolDown();
    }
}
