using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] public int wood;
    private bool collecting;
    private float collectionCooldown = 0.1f;
    private float lastCollectionTime;
    private HashSet<GameObject> processedWood;
    // Start is called before the first frame update
    void Awake()
    {
        wood = 100;
        collecting = false;
        processedWood = new HashSet<GameObject>();
        lastCollectionTime = -collectionCooldown;
    }

    public void AddWood(int amount)
    {
        wood += amount;
        collecting = false;
    }

    public void RemoveWood(int amount)
    {
        wood = Mathf.Max(0, wood - amount);
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (processedWood.Contains(collision.gameObject)) return;

        if (Time.time - lastCollectionTime < collectionCooldown) return;

        if (collecting) return;

        if (collision.gameObject.CompareTag("Wood"))
        {
            collecting = true;
            processedWood.Add(collision.gameObject);
            LogCount logCount = collision.gameObject.GetComponent<LogCount>();
            int count = logCount.count;
            lastCollectionTime = Time.time;
            StartCoroutine(ProcessWood(count, collision.gameObject));
        }
    }

    private IEnumerator ProcessWood(int count, GameObject wood)
    {
        AddWood(count);
        yield return new WaitForEndOfFrame();
        processedWood.Remove(wood);
        Destroy(wood);
    }

    private void OnDisable()
    {
        processedWood.Clear();
    }
}
