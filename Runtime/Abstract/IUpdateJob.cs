using System;
using Unity.Jobs;
using UnityEngine;

namespace JobIt.Runtime.Abstract
{
    public interface IUpdateJob : IDisposable
    {
        public JobHandle StartJob(JobHandle dependency = default);
        public void EndJob();
        public GameObject GetGameObject();
    }
}