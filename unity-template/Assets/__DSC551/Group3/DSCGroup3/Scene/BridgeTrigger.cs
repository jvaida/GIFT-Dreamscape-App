using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class BridgeTrigger: MonoBehaviour
{
    [SerializeField] UnityEvent _onTriggerEnter;

    private void OnTriggerEnter(Collider other)
    {
        _onTriggerEnter.Invoke();
    }
}