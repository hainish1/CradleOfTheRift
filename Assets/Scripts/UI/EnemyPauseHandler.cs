// using UnityEngine;
// using UnityEngine.AI;
// using System.Collections.Generic;
// using System.Linq;


// public class EnemyPauseManager : MonoBehaviour
// {
//     [SerializeField] private LayerMask enemyLayer; // Assign the Enemy layer here in Inspector
//     private List<IPausable> pausableEnemies = new List<IPausable>();

//     private class EnemyData
//     {
//         public NavMeshAgent agent;
//         public Rigidbody rb;
//         public Animator anim;
//         public bool wasStopped;
//         public float animSpeed;
//     }



//     private EnemyData[] allEnemies;
// private void Update()
// {
//     if (PauseManager.GameIsPaused)
//         PauseAll();
//     else
//         ResumeAll();
// }

// private void PauseAll()
// {
//     RebuildPausableList();
//     foreach (var enemy in pausableEnemies)
//         enemy.Pause();
// }

// private void ResumeAll()
// {
//     RebuildPausableList();
//     foreach (var enemy in pausableEnemies)
//         enemy.Resume();
// }

// private void RebuildPausableList()
// {
//     pausableEnemies.Clear();
//     IPausable[] enemies = GameObject.FindObjectsByType<MonoBehaviour>(
//         FindObjectsInactive.Include,// include inactive objects
//         FindObjectsSortMode.None// don’t sort
//     )
//     .OfType<IPausable>()
//     .ToArray();
//     foreach (var enemy in enemies)
//         pausableEnemies.Add(enemy);
// }

// }
