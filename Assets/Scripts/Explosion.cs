using UnityEngine;

public class Explosion : MonoBehaviour
{
    private void Awake()
    {
        SoundManager.Instance.PlayAtPosition("Explosion", transform.position);
    }
}