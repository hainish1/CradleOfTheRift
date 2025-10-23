using UnityEngine;

/// <summary>
/// Class : Responsibe for the entering and changing of enemy states
/// </summary>
public class EnemyStateMachine
{
    public EnemyState currentState { get; private set; }

    /// <summary>
    /// State Enemy enters when they are Initialized
    /// </summary>
    /// <param name="startState"></param>
    public void Initialize(EnemyState startState)
    {
        currentState = startState;
        currentState.Enter();
    }

    /// <summary>
    /// Change the State an Enemy is in
    /// </summary>
    /// <param name="nextState"></param>
    public void ChangeState(EnemyState nextState)
    {
        if (currentState == nextState) return; // we are already in that state
        currentState?.Exit(); // first exit the current one
        currentState = nextState; // set current to next state
        currentState?.Enter(); // enter that state
    }

    /// <summary>
    /// Used to check if current state needs to be updated
    /// </summary>
    public void Tick() => currentState?.Update();
}
