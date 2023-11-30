using System.Collections.Generic;
using JobIt.Runtime.Abstract;
using UnityEngine;

namespace JobIt.Runtime.Impl.JobScheduler
{
    public sealed class UpdateJobScheduler
    {
        #region Singleton Setup
        private UpdateJobScheduler() { }
        private static UpdateJobScheduler Instance => Nested.Instance;

        private class Nested { 
            static Nested() { }
            internal static UpdateJobScheduler Instance = new();
            //Force Reset for unity load
            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
            static void PlaymodeInit()
            {
                Instance = new UpdateJobScheduler();
            }
        }
        #endregion

        private readonly Dictionary<System.Type, IUpdateJob> _jobObjectLookup = new();

        public static void Register<T,TS>(MonoBehaviour o, TS args) where T : UpdateJob<TS> where TS : struct
        {
            if (o == null) 
                return;
            if(!Instance._jobObjectLookup.TryGetValue(typeof(T), out var job))
            {
                var jobObject = new GameObject($"{typeof(T)}")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                Object.DontDestroyOnLoad(jobObject);
                job = jobObject.AddComponent<T>();
                Instance._jobObjectLookup[typeof(T)] = job;
            }
            (job as T)?.RegisterItem(o, args);
        }

        public static void Withdraw<T,TS>(MonoBehaviour o) where T : UpdateJob<TS> where TS : struct
        {
            if (!Instance._jobObjectLookup.TryGetValue(typeof(T), out var job) || job == null) //unity null check
            {
                Debug.LogWarning($"Attempted to Withdraw from a non existing job of type {typeof(T)}!", o);
                return;
            }
            (job as T)?.WithdrawItem(o);
        }

        public static void UpdateJobData<T,TS>(MonoBehaviour o, TS args) where T : UpdateJob<TS> where TS : struct
        {
            if (!Application.isPlaying || o == null)
                return;
            if (!Instance._jobObjectLookup.TryGetValue(typeof(T), out var job) || job == null) //unity null check
            {
                Debug.LogWarning($"Attempted to UpdateJobData for a non existing job of type {typeof(T)}!", o);
                return;
            }
            (job as T)?.UpdateItem(o, args);
        }

        public static T GetJobObject<T,TS>() where T : UpdateJob<TS> where TS :struct
        {
            if (Instance._jobObjectLookup.TryGetValue(typeof(T), out var job))
                return job as T;
            return null;
        }

        public static bool TryReadJobData<T, TS>(Component o, out TS data) where T : UpdateJob<TS> where TS : struct
        {
            return GetJobObject<T, TS>().TryReadItem(o, out data);
        }

        public static void CleanJobs()
        {
            var jobs = Instance._jobObjectLookup.Values;
            foreach(var j in jobs)
            {
                if (j == null) continue;
                var g = j.GetGameObject();
                j.Dispose();
                if (g != null)
                {
                    
                    Object.Destroy(g);
                }
            }
            Instance._jobObjectLookup.Clear();
        }
    }
}