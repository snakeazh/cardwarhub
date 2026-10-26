namespace QFramework
{
    using System.Collections;

    public interface IPipelineStep
    {
        IEnumerator Execute();
        string Name { get; }
    }
}
