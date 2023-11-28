using UnityEngine;

[RequireComponent(typeof(Animator))]
public class BridgeController : MonoBehaviour
{
    Animator _animator;

    const string _animTriggerName = "CloseBridge";
    const int NUM_PARTICIPANTS = 2;

    int _peopleFinished = 0;
    bool _completed = false;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("Hi");
            RegisterPersonFinished();
        }
    }

    public void RegisterPersonFinished()
    {
        if (!_completed)
        {
            _peopleFinished++;
            if (_peopleFinished >= NUM_PARTICIPANTS)
            {
                _animator.SetTrigger(_animTriggerName);
                _completed = true;
            }
        }
    }
}
