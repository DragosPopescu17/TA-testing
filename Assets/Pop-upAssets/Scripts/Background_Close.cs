using UnityEngine;
using UnityEngine.EventSystems;

public class BackgroundClose : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject background;

    public void OnPointerClick(PointerEventData eventData)
    {
        background.SetActive(false);
    }
}