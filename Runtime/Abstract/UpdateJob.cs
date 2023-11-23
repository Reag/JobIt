using System;
using System.Collections.Generic;
using JobIt.Runtime.Structs;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.TestTools;

namespace JobIt.Runtime.Abstract
{
    /// <summary>
    /// The core logic for an UpdateJob. Provides the base functionality to allow for a simplified job system
    /// </summary>
    /// <typeparam name="T">The data required to register to this job. This data should be managed internally via NativeContainers.
    /// T does NOT have to pass burst tests.</typeparam>
    public abstract class UpdateJob<T> : MonoBehaviour, IUpdateJob where T : struct
    {
        /// <summary>
        /// Represents a queued action to be performed next time the job is scheduled
        /// </summary>
        private struct QueueItem
        {
            public JobDataAction JobAction;
            public Component Owner;
            public T Data;
        }

        /// <summary>
        /// Returns a reference to the GameObject the job is attached to.
        /// </summary>
        /// <returns>The GameObject this job is attached to, or null if disposed</returns>
        public GameObject GetGameObject()
        {
            return (IsDisposed) ? null : gameObject;
        }

        /// <summary>
        /// Number of Components currently subscribed to this job
        /// </summary>
        public int JobSize => OwnerList.Count;

        protected List<Component> OwnerList = new();
        public virtual bool IsRunning => !_isCompleted;
        /// <summary>
        /// Invoked as the final step of completing a job. Data is safe to read at this point
        /// </summary>
        public Action OnJobComplete;

        /// <summary>
        /// Public property to allow user controlled disabling of a job, such as if the game is paused
        /// </summary>
        public virtual bool CanRunJob => true;
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Defines the execution order of the job when managed by the UpdateJobScheduler. Lower values will execute first
        /// </summary>
        protected abstract int JobPriority { get; }
        private JobHandle _handle = default;
        private bool _isCompleted = true;
        private readonly Queue<QueueItem> _actionQueue = new();
        //This maps HashCodes to list indexes
        private readonly Dictionary<int, int> _hashToIndexLookup = new();

        /// <summary>
        /// Called when the job is disposed. Be sure to clean up any NativeContainers allocated or you will have a memory leak!
        /// </summary>
        protected abstract void DisposeLogic();

        /// <summary>
        /// Remove a registered Component from the UpdateJob. Takes effect the next time the job is scheduled
        /// </summary>
        /// <param name="o">The Component to withdraw</param>
        public virtual void WithdrawItem(Component o)
        {
            _actionQueue.Enqueue(new QueueItem { Owner = o, JobAction = JobDataAction.Remove, Data = default });
        }

        /// <summary>
        /// Register a Component to the UpdateJob. Takes effect the next time the job is scheduled
        /// </summary>
        /// <param name="o">The Component to be registered</param>
        /// <param name="data">Some struct that will be used as a data element in the job</param>
        public virtual void RegisterItem(Component o, T data)
        {
            _actionQueue.Enqueue(new QueueItem { Owner = o, JobAction = JobDataAction.Add, Data = data });
        }

        /// <summary>
        /// Update the data for a Component in the UpdateJob. Takes effect the next time the job is scheduled.
        /// WARNING: this mean that stale data will be in the jobs internal buffers until ScheduleJob is called
        /// </summary>
        /// <param name="o">The Component to be updated</param>
        /// <param name="data">The data to update with</param>
        public virtual void UpdateItem(Component o, T data)
        {
            _actionQueue.Enqueue(new QueueItem { Owner = o, JobAction = JobDataAction.Update, Data = data });
        }

        /// <summary>
        /// Tries to read the current data for a Component registered to this UpdateJob. Will fail if the UpdateJob is currently running.
        /// </summary>
        /// <param name="o">The Component to read</param>
        /// <param name="data">The data associated with the Component</param>
        /// <returns>True if the data could be read, false otherwise (Job is Running or Component is not in List)</returns>
        public virtual bool TryReadItem(Component o, out T data)
        {
            if (IsRunning)
            {
                Debug.LogWarning("Job is currently running, readback is disabled! Consider adjusting your timing");
                data = default;
                return false;
            }
            if (_hashToIndexLookup.TryGetValue(o.GetHashCode(), out var i))
            {
                data = ReadJobDataAtIndex(i);
                return true;
            }
            data = default;
            return false;
        }

        /// <summary>
        /// Starts the UpdateJob.
        /// </summary>
        /// <param name="dependsOn">The JobHandle that this job will depend on. Optional</param>
        /// <returns>The JobHandle for this job</returns>
        public JobHandle StartJob(JobHandle dependsOn = default)
        {
            _handle.Complete();
            if (IsDisposed || !CanRunJob) return dependsOn;
            ProcessQueue();
            _handle = ScheduleJob(dependsOn);
            _isCompleted = false;
            return _handle;
        }

        private void ProcessQueue()
        {
            while (_actionQueue.Count > 0)
            {
                var action = _actionQueue.Dequeue();
                var e = action.Owner;
                var hash = e.GetHashCode();
                if (e == null)
                {
                    if (_hashToIndexLookup.TryGetValue(hash, out var i))
                    {
                        RemoveJobDataAndSwapBack(i);
                        RemoveAndSwapBackInternal(i);
                    }
                    continue;
                }
                //index -1 -> not in HashMap
                var index = _hashToIndexLookup.TryGetValue(hash, out var v) ? v : -1;
                switch (action.JobAction)
                {
                    case JobDataAction.Remove:
                        if (index < 0)
                        {
                            Debug.LogWarning($"Attempted to remove an non existing job element from {GetType()}", this);
                            return;
                        }
                        RemoveJobDataAndSwapBack(index);
                        RemoveAndSwapBackInternal(index);
                        break;
                    case JobDataAction.Add when index >= 0:
                    case JobDataAction.Update when index >= 0:
                        UpdateJobData(index, action.Data);
                        break;
                    case JobDataAction.Add:
                        OwnerList.Add(e);
                        _hashToIndexLookup.Add(hash, OwnerList.Count - 1);
                        AddJobData(action.Data);
                        break;
                    case JobDataAction.Update:
                        Debug.LogWarning($"Attempted to update job data for an unregistered job element in {GetType()}", this);
                        break;
                }
            }
        }

        /// <summary>
        /// Adds the values contained in data to the jobs internal NativeContainers. Implementation is left abstract
        /// </summary>
        /// <param name="data">struct used to populate the internal NativeContainers</param>
        protected abstract void AddJobData(T data);

        /// <summary>
        /// Reads the values at an index of the internal NativeContainers and assembles a struct of type T that represents them. Implementation is left abstract
        /// </summary>
        /// <param name="index"></param>
        /// <returns>A struct of type T representing the data at the requested index</returns>
        protected abstract T ReadJobDataAtIndex(int index);

        private void RemoveAndSwapBackInternal(int index)
        {
            var hashToRemove = OwnerList[index].GetHashCode();
            var lastIndexHash = 0;
            if (OwnerList.Count > 0)
            {
                lastIndexHash = OwnerList[^1].GetHashCode();
            }

            _hashToIndexLookup.Remove(hashToRemove);
            if (_hashToIndexLookup.ContainsKey(lastIndexHash))
                _hashToIndexLookup[lastIndexHash] = index;
            OwnerList.RemoveAtSwapBack(index);
        }

        /// <summary>
        /// Removes and swaps back the data at an index of the internal NativeContainers. Implementation is left abstract.
        /// WARNING: Remove and RemoveAndSwapBack are different operations. Failure to implement a RemoveAndSwapBack operation here will break the job!
        /// </summary>
        /// <param name="index"></param>
        protected abstract void RemoveJobDataAndSwapBack(int index);

        /// <summary>
        /// Update the NativeContainers at an index with some new data. Implementation is left abstract.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="data">struct used to update the internal NativeContainers</param>
        protected abstract void UpdateJobData(int index, T data);

        /// <summary>
        /// Completes the job and allows reading of job data.
        /// </summary>
        public void EndJob()
        {
            _handle.Complete();
            _isCompleted = true;
            if (IsDisposed) return;
            CompleteJob();
            OnJobComplete?.Invoke();
        }

        /// <summary>
        /// The actual scheduling of the job. This will create the JobStruct, load it with data from the NativeContainers, and Schedule it. Implementation is left abstract
        /// </summary>
        /// <param name="dependsOn">previous handles that this job should depend on</param>
        /// <returns>the job handle created from scheduling the job</returns>
        protected abstract JobHandle ScheduleJob(JobHandle dependsOn = default);

        /// <summary>
        /// Called when a job has ended. After this, OnJobComplete is invoked
        /// </summary>
        protected virtual void CompleteJob()
        {
        }

        [ExcludeFromCoverage]
        public virtual void Awake()
        {
#if UNITY_EDITOR
            //We use this to prevent memory leaks when unexpectedly entering or exiting playmode.
            UnityEditor.EditorApplication.playModeStateChanged += PlayModeStateChange;
#endif
        }

#if UNITY_EDITOR
        [ExcludeFromCoverage]
        private void PlayModeStateChange(UnityEditor.PlayModeStateChange state)
        {
            if (state is UnityEditor.PlayModeStateChange.EnteredPlayMode or UnityEditor.PlayModeStateChange.ExitingEditMode) return;
            try
            {
                UnityEditor.EditorApplication.playModeStateChanged -= PlayModeStateChange;
            }
            catch
            {
                //Catch and release
            }
            finally
            {
                //Force a dispose of the job data when the playmode state changes
                //This prevents the native containers from leaking when in the editor.
                Dispose();
            }
        }
#endif
        [ExcludeFromCoverage]
        protected virtual void OnDestroy()
        {
            Dispose();
        }

        /// <summary>
        /// Dispose of this job, cleaning up all data associated with it
        /// </summary>
        public void Dispose()
        {
            IsDisposed = true;
            EndJob();
            _actionQueue.Clear();
            OwnerList.Clear();
            _hashToIndexLookup.Clear();
            DisposeLogic();
            OnJobComplete = null;
        }
    }
}