using General;

/// <summary>
/// Dedicated pool for AudioSourcePoolable objects, inheriting from SimplePool template.
/// </summary>
public class AudioPool : SimplePool<AudioSourcePoolable>
{
    public new static AudioPool Instance => FindFirstObjectByType<AudioPool>();
}