using UnityEngine;

public class DuplicatesEnabler : MonoBehaviour
{
    public GameObject object1;
    public void EnableObjects(bool value)
    {
        if (object1 != null) object1.SetActive(value);
    }
}