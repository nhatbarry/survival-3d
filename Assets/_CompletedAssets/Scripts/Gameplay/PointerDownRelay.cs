using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CompleteProject
{
    /// <summary>
    /// Raises Pressed as soon as a finger or mouse goes down on this UI element (snappier than Button, which waits for release).
    /// </summary>
    public class PointerDownRelay : MonoBehaviour, IPointerDownHandler
    {
        public event Action Pressed;


        public void OnPointerDown (PointerEventData eventData)
        {
            Pressed?.Invoke ();
        }
    }
}
