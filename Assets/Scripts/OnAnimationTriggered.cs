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
    private void StartRegistering() => _playerMeleeController.StartRegistering();

    /// <summary>
    ///   <para>
    ///     Animation event to deactivate player melee registration.
    ///   </para>
    /// </summary>
    private void StopRegistering() => _playerMeleeController.StopRegistering();

    /// <summary>
    ///   <para>
    ///     Animation event to begin the player weapon flip animation coroutine.
    ///   </para>
    /// </summary>
    private void WeaponThrowAnimBegin() => _playerShooter.WeaponThrowAnimBegin();

    /// <summary>
    ///   <para>
    ///     Animation event to shoot a player projectile and make the player's weapon disappear.
    ///   </para>
    /// </summary>
    private void WeaponThrow() => _playerShooter.WeaponThrow();

    /// <summary>
    ///   <para>
    ///     Animation event to begin the player's weapon regain animation coroutine.
    ///   </para>
    /// </summary>
    private void WeaponThrowAnimEnd() => _playerShooter.WeaponThrowAnimEnd();

    /// <summary>
    ///   <para>
    ///     Animation event to make a golem throw a rock.
    ///   </para>
    /// </summary>
    private void GolemRockThrow()
    {
        _enemyGolem.ThrowRock();
        _enemyGolem.HideRockHand();
    }

    /// <summary>
    ///   <para>
    ///     Animation event to make a golem regain its rock hand.
    ///   </para>
    /// </summary>
    private void GolemHandRegain() => _enemyGolem.ShowRockHand();

    /// <summary>
    ///   <para>
    ///     Animation event to make a golem deal slam damage.
    ///   </para>
    /// </summary>
    private void GolemSlamDamage() => _enemyGolem.MeleeSlamAttack();

    /// <summary>
    ///   <para>
    ///     Animation event to make a golem throw a rock.
    ///   </para>
    /// </summary>
    private void TitanRockThrow()
    {
        _enemyGolem.ThrowRock();
        _enemyGolem.HideRockHand();
    }

    /// <summary>
    ///   <para>
    ///     Animation event to make a titan fling a rock barrage.
    ///   </para>
    /// </summary>
    private void TitanRockBarrage() => _enemyTitan.RockBarrage();

    /// <summary>
    ///   <para>
    ///     Animation event to make a golem regain its rock hand.
    ///   </para>
    /// </summary>
    private void TitanHandRegain() => _enemyTitan.ShowRockHand();

    /// <summary>
    ///   <para>
    ///     Animation event to make a golem deal slam damage.
    ///   </para>
    /// </summary>
    private void TitanSlamDamage() => _enemyTitan.MeleeSlamAttack();

    /// <summary>
    ///   <para>
    ///     Animation event to make a titan deal sweep damage.
    ///   </para>
    /// </summary>
    private void TitanSweepDamageLeft() => _enemyTitan.MeleeSweepAttack(0);

    /// <summary>
    ///   <para>
    ///     Animation event to make a titan deal sweep damage.
    ///   </para>
    /// </summary>
    private void TitanSweepDamageRight() => _enemyTitan.MeleeSweepAttack(1);
}
