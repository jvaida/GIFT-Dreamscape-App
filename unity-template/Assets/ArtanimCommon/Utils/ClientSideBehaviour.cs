using UnityEngine;
using System.Collections;
using Artanim.Location.Network;

namespace Artanim
{

/// <summary>
/// Abstract MonoBehaviour using Awake to enable/disable itself based on the component type of the application.
/// </summary>
public abstract class ClientSideBehaviour : MonoBehaviour
{
    public bool RunOnServer = true;

    virtual protected void Awake()
    {
        enabled = RunOnServer || NetworkInterface.Instance.IsClient;
    }
}

}