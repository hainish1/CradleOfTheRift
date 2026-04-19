// <summary>
//   <authors>
//     Samuel Rigby
//   </authors>
//   <para>
//     Written by Samuel Rigby for GAMES 4510, University of Utah.
//   </para>
// </summary>

using UnityEngine;

public class OnAnimationTriggered : MonoBehaviour
{
    private PlayerMeleeControllerV2 _playerMeleeController;
    private PlayerShooter _playerShooter;
    private EnemyGolem _enemyGolem;
    private EnemyTitan _enemyTitan;

    void Awake()
    {
        _playerMeleeController = GetComponentInParent<PlayerMeleeControllerV2>();
        _playerShooter = GetComponentInParent<PlayerShooter>();
        _enemyGolem = GetComponentInParent<EnemyGolem>();
        _enemyTitan = GetComponentInParent<EnemyTitan>();
    }

    /// <summary>
    ///   <para>
    ///     Animation event to activate player melee registration.
    ///   </para>
    /// </summary>
    private void StartRegistering()
    {
        if (_playerMeleeController != null)
        {
            _playerMeleeController.StartRegistering();
        }
    }

    /// <summary>
    ///   <para>
    ///     Animation event to deactivate player melee registration.
    ///   </para>
    /// </summary>
    private void StopRegistering()
    {
        if (_playerMeleeController != null)
        {
            _playerMeleeController.StopRegistering();
        }
    }

    /// <summary>
    ///   <para>
    ///     Animation event to begin the player weapon flip animation coroutine.
    ///   </para>
    /// </summary>
    private void WeaponThrowAnimBegin()
    {
        if (_playerShooter != null)
        {
            _playerShooter.WeaponThrowAnimBegin();
        }
    }

    /// <summary>
    ///   <para>
    ///     Animation event to shoot a player projectile and make the player's weapon disappear.
    ///   </para>
    /// </summary>
    private void WeaponThrow()
    {
        if (_playerShooter != null)
        {
            _playerShooter.WeaponThrow();
        }
    }

    /// <summary>
    ///   <para>
    ///     Animation event to begin the player's weapon regain animation coroutine.
    ///   </para>
    /// </summary>
    private void WeaponThrowAnimEnd()
    {
        if (_playerShooter != null)
        {
            _playerShooter.WeaponThrowAnimEnd();
        }
    }

    /// <summary>
    ///   <para>
    ///     Animation event to make a golem throw a rock.
    ///   </para>
    /// </summary>
    private void GolemRockThrow()
    {
        if (_enemyGolem != null)
        {
            _enemyGolem.ThrowRock();
            _enemyGolem.HideRockHand();
        }
    }

    /// <summary>
    ///   <para>
    ///     Animation event to make a golem regain its rock hand.
    ///   </para>
    /// </summary>
    private void GolemHandRegain()
    {
        if (_enemyGolem != null)
        {
            _enemyGolem.ShowRockHand();
        }
    }

    /// <summary>
    ///   <para>
    ///     Animation event to make a golem deal slam damage.
    ///   </para>
    /// </summary>
    private void GolemSlamDamage()
    {
        if (_enemyGolem != null)
        {
            _enemyGolem.MeleeSlamAttack();
        }
    }

    /// <summary>
    ///   <para>
    ///     Animation event to make a golem throw a rock.
    ///   </para>
    /// </summary>
    private void TitanRockThrow()
    {
        EnemyGolem throwOwner = _enemyTitan != null ? _enemyTitan : _enemyGolem;
        if (throwOwner != null)
        {
            throwOwner.ThrowRock();
            throwOwner.HideRockHand();
        }
    }

    /// <summary>
    ///   <para>
    ///     Animation event to make a titan fling a rock barrage.
    ///   </para>
    /// </summary>
    private void TitanRockBarrage()
    {
        if (_enemyTitan != null)
        {
            _enemyTitan.RockBarrage();
        }
    }

    /// <summary>
    ///   <para>
    ///     Animation event to make a golem regain its rock hand.
    ///   </para>
    /// </summary>
    private void TitanHandRegain()
    {
        if (_enemyTitan != null)
        {
            _enemyTitan.ShowRockHand();
        }
    }

    /// <summary>
    ///   <para>
    ///     Animation event to make a golem deal slam damage.
    ///   </para>
    /// </summary>
    private void TitanSlamDamage()
    {
        if (_enemyTitan != null)
        {
            _enemyTitan.MeleeSlamAttack();
        }
    }

    /// <summary>
    ///   <para>
    ///     Animation event to make a titan deal sweep damage.
    ///   </para>
    /// </summary>
    private void TitanSweepDamageLeft()
    {
        if (_enemyTitan != null)
        {
            _enemyTitan.MeleeSweepAttack(0);
        }
    }

    /// <summary>
    ///   <para>
    ///     Animation event to make a titan deal sweep damage.
    ///   </para>
    /// </summary>
    private void TitanSweepDamageRight()
    {
        if (_enemyTitan != null)
        {
            _enemyTitan.MeleeSweepAttack(1);
        }
    }
}
