using System;
using System.Collections.Generic;
using JobIt.Runtime.Structs;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.TestTools;

namespace JobIt.Runtime.Abstract
{
    public abstract class UpdateJob<T> : MonoBehaviour, IUpdateJob where T : struct
    {
        private struct QueueItem
        {
            public JobDataAction action;
            public MonoBehaviour owner;
            public T data;
        }

        public GameObject GetGameObject()
        {
            return (IsDisposed) ? null : gameObject;
        }

        public int JobSize { get => ownerList.Count; }

        protected List<MonoBehaviour> ownerList = new();
        public virtual bool IsRunning { get => !_handle.IsCompleted; }
        public Action OnJobComplete;
        public virtual bool CanRunJob { get => true; }
        public bool IsDisposed { get; private set; }

        protected abstract int JobPriority { get; }
        private JobHandle _handle = default;
        private readonly Queue<QueueItem> _actionQueue = new();
        //This maps HashCodes to list indexes
        private readonly Dictionary<int, int> _hashToIndexLookup = new();

        protected abstract void DisposeLogic();

        public virtual void WithdrawItem(MonoBehaviour o)
        {
            _actionQueue.Enqueue(new QueueItem { owner = o, action = JobDataAction.Remove, data = default });
        }

        /// <summary>
        /// A Late Update Job must define its parameters for its args, if it has any. 
        /// Should you see this tooltip, a job has not been commented correctly
        /// </summary>
        /// <param name="o">The game object to be registered</param>
        /// <param name="data">Some struct of params</param>
        public virtual void RegisterItem(MonoBehaviour o, T data)
        {
            _actionQueue.Enqueue(new QueueItem { owner = o, action = JobDataAction.Add, data = data });
        }

        public virtual void UpdateItem(MonoBehaviour o, T data)
        {
            _actionQueue.Enqueue(new QueueItem { owner = o, action = JobDataAction.Update, data = data});
        }

        public virtual bool TryReadItem(MonoBehaviour o, out T data)
        {
            if (IsRunning)
            {
                Debug.LogWarning("Job is currently running, readback is disabled! Consider adjusting your timing");
                data = default;
                return false;
            }
            if (_hashToIndexLookup.TryGetValue(o.GetHashCode(), out int i))
            {
                data = ReadJobDataAtIndex(i);
                return true;
            }
            data = default;
            return false;
        }


        public JobHandle StartJob(JobHandle dependency = default)
        {
            _handle.Complete();
            if (IsDisposed || !CanRunJob) return dependency;
            try
            {
                ProcessQueue();
                _handle = ScheduleJob(dependency);
                return _handle;
            } 
            catch (Exception e)
            {
                Debug.LogWarning($"Exception in job {GetType()}: " + e);
                return dependency;
            }
        }

        private void ProcessQueue()
        {
            while (_actionQueue.Count > 0)
            {
                var action = _actionQueue.Dequeue();
                var e = action.owner;
                int hash = e.GetHashCode();
                if (e == null)
                {
                    if(_hashToIndexLookup.TryGetValue(hash, out int i))
                    {
                        RemoveJobDataAndSwapBack(i);
                        RemoveAndSwapBackInternal(i);
                    }
                    continue;
                }
                //index -1 -> not in HashMap
                int index = _hashToIndexLookup.TryGetValue(hash, out int v) ? v : -1;
                switch (action.action)
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
                    case JobDataAction.Update when index >=0:
                        UpdateJobData(index, action.data);
                        break;
                    case JobDataAction.Add:
                        if (_hashToIndexLookup.ContainsKey(hash))
                        {
                            Debug.LogWarning($"Attempted to add a job that is already added to {GetType()}", this);
                            return;
                        }
                        ownerList.Add(e);
                        _hashToIndexLookup.Add(hash, ownerList.Count - 1);
                        AddJobData(action.data);
                        break;
                    case JobDataAction.Update:
                        Debug.LogWarning($"Attempted to update job data for an unregistered job element in {GetType()}", this);
                        break;
                    default:
                        break;
                }
            }
        }

        protected abstract void AddJobData(T data);

        protected abstract T ReadJobDataAtIndex(int index);

        private void RemoveAndSwapBackInternal(int index)
        {
            int hashToRemove = ownerList[index].GetHashCode();
            int lastIndexHash = 0;
            if (ownerList.Count > 0)
            {
                lastIndexHash = ownerList[^1].GetHashCode();
            }

            _hashToIndexLookup.Remove(hashToRemove);
            if(_hashToIndexLookup.ContainsKey(lastIndexHash))
                _hashToIndexLookup[lastIndexHash] = index;
            ownerList.RemoveAtSwapBack(index);
        }

        /// <summary>
        /// ALL operations here should follow REMOVE AND SWAP BACK
        /// failure to do so will make the job fail to run correctly
        /// </summary>
        /// <param name="index"></param>
        protected abstract void RemoveJobDataAndSwapBack(int index);

        protected abstract void UpdateJobData(int index, T data);

        public void EndJob()
        {
            _handle.Complete();
            if (IsDisposed) return;
            CompleteJob();
            OnJobComplete?.Invoke();
        }

        protected abstract JobHandle ScheduleJob(JobHandle dependency = default);

        protected abstract void CompleteJob();

        [ExcludeFromCoverage]
        public virtual void Awake()
        {
#if UNITY_EDITOR
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
                Dispose();
            }
        }
#endif
        protected virtual void OnDestroy()
        {
            Dispose();
        }

        public virtual void Dispose()
        {
            IsDisposed = true;
            EndJob();
            _actionQueue.Clear();
            ownerList.Clear();
            _hashToIndexLookup.Clear();
            DisposeLogic();
            OnJobComplete = null;
        }
    }
}