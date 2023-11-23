using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;

namespace JobIt.Runtime.Abstract
{
    public abstract class JobScheduleCompleter : MonoBehaviour
    {
        public JobHandle Job { get => Handle; set { Handle = value; HandleSet = true; } }
        protected bool HandleSet = false;
        protected JobHandle Handle;
        public delegate void CompleteEvent();
        public event CompleteEvent OnComplete;

        protected virtual void CompleteJob()
        {
            if (!HandleSet) return;
            Profiler.BeginSample("On Job Complete");
            Handle.Complete();
            OnComplete?.Invoke();
            HandleSet = false;
            Profiler.EndSample();
        }

        protected virtual void OnDestroy()
        {
            CompleteJob();
        }
    }
}