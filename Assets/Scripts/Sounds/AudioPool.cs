using General;
using UnityEngine;

/// <summary>
/// Dedicated pool for AudioSourcePoolable objects, inheriting from SimplePool template.
/// </summary>
public class AudioPool : SimplePool<AudioSourcePoolable>
{
    private static AudioPool _audioPool;

    /*
     * Kept once it is found rather than searched for again. This is asked
     * several times per sound, and a search that walks every object in the
     * game is not something to do at that rate. The search still happens when
     * what was kept has gone, which is what makes it survive a scene change.
     */
    public new static AudioPool Instance
    {
        get
        {
            if (_audioPool == null)
            {
                _audioPool = FindFirstObjectByType<AudioPool>();
            }

            return _audioPool;
        }
    }
}
