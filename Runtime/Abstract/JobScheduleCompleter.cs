using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.TestTools;

namespace JobIt.Runtime.Abstract
{
    /// <summary>
    /// This Abstract Class represents some strategy for completing a managed job.
    /// </summary>
    public abstract class JobScheduleCompleter : MonoBehaviour
    {
        /// <summary>
        /// This is the JobHandle for the Completer to manage.
        /// It will be completed when CompleteJob is called.
        /// </summary>
        public JobHandle Job { get => Handle; set { Handle = value; HandleSet = true; } }
        protected bool HandleSet = false;
        protected JobHandle Handle;
        public delegate void CompleteEvent();
        /// <summary>
        /// This Event is invoked whenever the Job has been set and then completed.
        /// It will not be invoked if CompleteJob() is called on an already completed job.
        /// </summary>
        public event CompleteEvent OnComplete;

        /// <summary>
        /// Completes what ever Job this completer references.
        /// Child classes are tasked with implementing a strategy for calling this method
        /// </summary>
        protected virtual void CompleteJob()
        {
            if (!HandleSet) return;
            Profiler.BeginSample("On Job Complete");
            Handle.Complete();
            OnComplete?.Invoke();
            HandleSet = false;
            Profiler.EndSample();
        }

        [ExcludeFromCoverage] 
        // Ensure safe exit when running in the editor
        protected virtual void OnDestroy()
        {
            CompleteJob();
        }
    }
}