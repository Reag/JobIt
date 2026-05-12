using JetBrains.Annotations;
using UnityEngine;

public class MatchPosition : MonoBehaviour
{
    public Transform Parent
    {
        get => parent;
        set => SetParent(value);
    }

    [SerializeField]
    private Transform parent;

    [CanBeNull] private MatchPositionJobWrapper _wrapper;

    public void OnEnable()
    {
        if (parent == null) return;
        _wrapper?.Dispose();
        var jobElement = new MatchPositionJobElement()
        {
            Parent = parent,
            Child = transform
        };
        _wrapper = new MatchPositionJobWrapper(this, jobElement);
    }

    public void SetParent(Transform newParent)
    {
        if (newParent == null) return;
        parent = newParent;
        if (_wrapper == null) return;

        var jobElement = new MatchPositionJobElement
        {
            Parent = parent,
            Child = transform
        };
        _wrapper?.UpdateJobData(jobElement);
    }

    public void OnDisable()
    {
        _wrapper?.Dispose();
        _wrapper = null;
    }
}
