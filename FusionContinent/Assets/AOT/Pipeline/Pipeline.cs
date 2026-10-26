namespace QFramework
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class Pipeline
    {
        private readonly List<IPipelineStep> mSteps = new List<IPipelineStep>();

        public Pipeline AddStep(IPipelineStep step)
        {
            if (step != null) mSteps.Add(step);
            return this;
        }

        public IEnumerator Run()
        {
            for (var i = 0; i < mSteps.Count; i++)
            {
                yield return mSteps[i].Execute();
            }
        }

        public static Coroutine Run(MonoBehaviour host, Pipeline pipeline)
        {
            return host.StartCoroutine(pipeline.Run());
        }
    }
}
