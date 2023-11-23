using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;

namespace JobIt.Runtime.Abstract
{
    public abstract class JobScheduleCompleter : MonoBehaviour
    {
        public JobHandle Job { get => _handle; set { _handle = value; _handleSet = true; } }
        protected bool _handleSet = false;
        protected JobHandle _handle;
        public delegate void CompleteEvent();
        public event CompleteEvent OnComplete;

        protected virtual void CompleteJob()
        {
            if (!_handleSet) return;
            Profiler.BeginSample("On Job Complete");
            _handle.Complete();
            OnComplete?.Invoke();
            _handleSet = false;
            Profiler.EndSample();
        }

        protected virtual void OnDestroy()
        {
            CompleteJob();
        }
    }
}