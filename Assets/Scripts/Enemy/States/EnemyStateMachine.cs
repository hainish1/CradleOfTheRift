using UnityEngine;

// this class will be responsibe the entering and changing of enemy states
public class EnemyStateMachine
{
    public EnemyState currentState { get; private set; }

    public void Initialize(EnemyState startState)
    {
        currentState = startState;
        currentState.Enter();
    }

    public void ChangeState(EnemyState nextState)
    {
        if (currentState == nextState) return; // we are already in that state
        currentState?.Exit(); // first exit the current one
        currentState = nextState; // set current to next state
        currentState?.Enter(); // enter that state
    }

    public void Tick() => currentState?.Update();
}
