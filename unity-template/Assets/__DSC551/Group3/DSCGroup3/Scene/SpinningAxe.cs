using UnityEngine;

public class SpinningAxe : MonoBehaviour
{
    [SerializeField] bool _ccw = false;

    private void Update()
    {
        transform.Rotate(0f, (_ccw ? -1 : 1) * 75f * Time.deltaTime, 0f);
    }
}
