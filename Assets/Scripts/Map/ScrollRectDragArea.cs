using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ScrollRectDragArea : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    public ScrollRect targetScrollRect;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (targetScrollRect != null)
        {
            targetScrollRect.OnBeginDrag(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (targetScrollRect != null)
        {
            targetScrollRect.OnDrag(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (targetScrollRect != null)
        {
            targetScrollRect.OnEndDrag(eventData);
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (targetScrollRect != null)
        {
            targetScrollRect.OnScroll(eventData);
        }
    }
}
