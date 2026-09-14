using UnityEngine;

public class DuplicatesEnabler : MonoBehaviour
{
    public GameObject targetObject;

    public void ToggleObject()
    {
        targetObject.SetActive(!targetObject.activeSelf);
    }
}