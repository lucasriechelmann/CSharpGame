namespace Engine.Core.Tests.Fixtures;

/// <summary>Test asset that counts how many times it has been disposed.</summary>
internal sealed class DisposableAsset : IDisposable
{
    public int DisposeCount { get; private set; }

    public void Dispose() => DisposeCount++;
}
