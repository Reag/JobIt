using System;
using Unity.Jobs;
using UnityEngine;

namespace JobIt.Runtime.Abstract
{
    /// <summary>
    /// Base interface for a JobIt UpdateJob
    /// </summary>
    public interface IUpdateJob : IDisposable
    {
        internal void PreStartJob();
        public JobHandle StartJob(JobHandle dependsOn = default);
        public void EndJob();
        public GameObject GetGameObject();
    }
}