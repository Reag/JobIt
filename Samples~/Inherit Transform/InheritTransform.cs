using System.Collections;
using System.Collections.Generic;
using JobIt.Runtime.Impl.JobScheduler;
using UnityEngine;

public class InheritTransform : MonoBehaviour
{
    public Transform Parent
    {
        get => parent;
        set => SetParent(value);
    }
    public Transform parent;

    public void OnEnable()
    {
        if (parent == null) return;
        var jobElement = new InheritTransformJobElement
        {
            Parent = parent,
            Child = transform
        };
        UpdateJobScheduler.Register<InheritTransformJob, InheritTransformJobElement>(this, jobElement);
    }

    public void SetParent(Transform newParent)
    {
        if (newParent == null) return;
        parent = newParent;
        var jobElement = new InheritTransformJobElement
        {
            Parent = parent,
            Child = transform
        };
        UpdateJobScheduler.UpdateJobData<InheritTransformJob, InheritTransformJobElement>(this, jobElement);
    }

    public void OnDisable()
    {
        UpdateJobScheduler.Withdraw<InheritTransformJob, InheritTransformJobElement>(this);
    }
}
