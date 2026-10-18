namespace Engine.Audio;

/// <summary>
/// Bookkeeping for a fixed set of playback voices: which are busy, which sound each is on (via generations), and which to cut
/// off when every voice is taken. Kept separate from OpenAL so the policy can be unit-tested without a sound card.
/// </summary>
/// <remarks>
/// Why a fixed pool: sound hardware and OpenAL cap simultaneous sources, and creating them mid-game is slow. So the device
/// creates its sources once and this pool hands the slots out. When a burst of sounds exceeds the pool, the <i>oldest</i> sound
/// is stolen: it is the likeliest to be nearly finished, and the newest sound (the one the player just caused) always plays.
/// </remarks>
public sealed class VoicePool
{
    private readonly bool[] _busy;
    private readonly int[] _generation;
    private readonly long[] _startedAt;
    private long _clock;

    /// <summary>Creates a pool of <paramref name="capacity"/> free voices.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The capacity is not positive.</exception>
    public VoicePool(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "A voice pool needs at least one voice.");
        _busy = new bool[capacity];
        _generation = new int[capacity];
        _startedAt = new long[capacity];
    }

    /// <summary>Total number of voices.</summary>
    public int Capacity => _busy.Length;

    /// <summary>Number of voices currently busy.</summary>
    public int ActiveCount { get; private set; }

    /// <summary>True when the slot is currently playing a sound.</summary>
    public bool IsBusy(int index) => _busy[index];

    /// <summary>
    /// Claims a voice for a new sound: the lowest free slot, or, when all are busy, the one that started longest ago.
    /// </summary>
    /// <param name="stolen">True when a busy voice was taken over, so the caller must stop its current sound first.</param>
    /// <returns>A fresh handle (new generation) for the claimed slot.</returns>
    public VoiceId Acquire(out bool stolen)
    {
        int slot = -1;
        for (int i = 0; i < _busy.Length; i++)
        {
            if (_busy[i]) continue;
            slot = i;
            break;
        }

        stolen = slot < 0;
        if (stolen)
        {
            slot = 0;
            for (int i = 1; i < _busy.Length; i++)
                if (_startedAt[i] < _startedAt[slot]) slot = i;
        }
        else
        {
            _busy[slot] = true;
            ActiveCount++;
        }

        _generation[slot]++;
        _startedAt[slot] = ++_clock;
        return new VoiceId(slot, _generation[slot]);
    }

    /// <summary>Marks a slot free (its sound finished or was stopped). Freeing an already free slot does nothing.</summary>
    public void Release(int index)
    {
        if (!_busy[index]) return;

        _busy[index] = false;
        ActiveCount--;
    }

    /// <summary>True when the handle still refers to the sound it was issued for: same generation and the slot not yet released.</summary>
    public bool IsCurrent(VoiceId voice) =>
        !voice.IsNone
        && (uint)voice.Index < (uint)_busy.Length
        && _busy[voice.Index]
        && _generation[voice.Index] == voice.Generation;
}
