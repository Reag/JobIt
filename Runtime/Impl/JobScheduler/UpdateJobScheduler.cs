using System.Collections.Generic;
using JobIt.Runtime.Abstract;
using UnityEngine;

namespace JobIt.Runtime.Impl.JobScheduler
{
    public sealed class UpdateJobScheduler
    {
        #region Singleton Setup
        private UpdateJobScheduler() { }
        private static UpdateJobScheduler Instance { get => Nested.instance; }
        private class Nested { 
            static Nested() { }
            internal static UpdateJobScheduler instance = new();
            //Force Reset for unity load
            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
            static void PlaymodeInit()
            {
                instance = new();
            }
        }
        #endregion

        private readonly Dictionary<System.Type, IUpdateJob> jobObjectLookup = new();

        public static void Register<T,S>(MonoBehaviour o, S args) where T : UpdateJob<S> where S : struct
        {
            if (!Application.isPlaying || o == null) 
                return;
            if(!Instance.jobObjectLookup.TryGetValue(typeof(T), out var job))
            {
                var jobObject = new GameObject($"{typeof(T)}")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                GameObject.DontDestroyOnLoad(jobObject);
                job = jobObject.AddComponent<T>();
                Instance.jobObjectLookup[typeof(T)] = job;
            }
            (job as T)?.RegisterItem(o, args);
        }

        public static void Withdraw<T,S>(MonoBehaviour o) where T : UpdateJob<S> where S : struct
        {
            if (!Application.isPlaying || o == null) 
                return;
            if (!Instance.jobObjectLookup.TryGetValue(typeof(T), out var job))
            {
                Debug.LogWarning($"Attempted to Withdraw from a non existing job of type{typeof(T)}!", o);
                return;
            }
            (job as T)?.WithdrawItem(o);
        }

        public static void UpdateJobData<T,S>(MonoBehaviour o, S args) where T : UpdateJob<S> where S : struct
        {
            if (!Application.isPlaying || o == null)
                return;
            if (!Instance.jobObjectLookup.TryGetValue(typeof(T), out var job))
            {
                Debug.LogWarning($"Attempted to UpdateJobData for a non existing job of type {typeof(T)}!", o);
                return;
            }
            (job as T)?.UpdateItem(o, args);
        }

        public static T GetJobObject<T,S>() where T : UpdateJob<S> where S :struct
        {
            if (Instance.jobObjectLookup.TryGetValue(typeof(T), out var job))
                return job as T;
            return null;
        }

        public static System.Action GetJobCompleteEvent<T, S>() where T : UpdateJob<S> where S : struct
        {
            var job = GetJobObject<T, S>();
            if (job == null) return null;
            return job.OnJobComplete;
        }

        public static void CleanJobs()
        {
            var jobs = Instance.jobObjectLookup.Values;
            foreach(var j in jobs)
            {
                if (j == null) continue;
                var g = j.GetGameObject();
                j.Dispose();
                if (g != null)
                {
                    
                    GameObject.Destroy(g);
                }
            }
            Instance.jobObjectLookup.Clear();
        }
    }
}