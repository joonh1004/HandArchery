namespace HandArchery.Core
{
    public interface IHandInputSource
    {
        bool TryGetLatest(out HandInputFrame frame);
        string StatusMessage { get; }
    }
}
