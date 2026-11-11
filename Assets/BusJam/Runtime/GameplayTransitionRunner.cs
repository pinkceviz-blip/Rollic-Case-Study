using System.Collections;
using UnityEngine;
namespace Rollic.BusJam
{
    // Kept separate so rebuilding a level can safely cancel its passenger coroutines.
    public sealed class GameplayTransitionRunner:MonoBehaviour
    {
        public void Play(IEnumerator sequence){StartCoroutine(Run(sequence));}
        IEnumerator Run(IEnumerator sequence){yield return sequence;Destroy(gameObject);}
    }
}
