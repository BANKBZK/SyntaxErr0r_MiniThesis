using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomLight : MonoBehaviour
{

    public List<GameObject> objectsToToggle;

    public float minInterval = 2f;

    public float maxInterval = 5f;

    private void Start()
    {
        StartCoroutine(RandomToggleLoop());
    }

    private IEnumerator RandomToggleLoop()
    {
        while (true)
        {
            float waitTime = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(waitTime);

            // Choose a random GameObject from the list
            if (objectsToToggle.Count > 0)
            {
                int index = Random.Range(0, objectsToToggle.Count);
                GameObject selectedObj = objectsToToggle[index];

                // Toggle its active state
                selectedObj.SetActive(!selectedObj.activeSelf);
            }
        }
    }
}
