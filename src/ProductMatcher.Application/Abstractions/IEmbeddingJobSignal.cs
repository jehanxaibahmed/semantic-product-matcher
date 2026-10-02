namespace ProductMatcher.Application.Abstractions;

/// <summary>Wakes the background embedding job after the catalogue changes.</summary>
public interface IEmbeddingJobSignal
{
    void Notify();
}
