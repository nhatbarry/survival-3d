using CaptainPinkTurd.Core.Utils;
using CaptainPinkTurd.Core.Utilities;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CaptainPinkTurd.UI.Popup
{
    public class PopupText : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;   
        [SerializeField] private Image[] images;
        [SerializeField] private float popupTime = .25f;
        [SerializeField] private Ease popupEaseType = Ease.Linear;
        [SerializeField] private float fadeOutTime = .5f;
        [SerializeField] private Ease fadeOutEaseType = Ease.InQuint;
        
        public void InitializeText(string textContent, Vector3 spawnPos, float targetHeight)
        {
            PopupText textObject = ObjectPoolManager.Instance.SpawnObject(gameObject, spawnPos, 
                Quaternion.identity, ObjectPoolManager.PoolType.PopupText).GetComponent<PopupText>();

            textObject.text.text = textContent;
            ToggleTextPopupVisual(textObject, popupTime, true);

            textObject.transform.DOMoveY(spawnPos.y + targetHeight, popupTime)
                .OnComplete(() =>
            {
                ToggleTextPopupVisual(textObject, fadeOutTime, false);
                textObject.StartCoroutine(CoroutineUtils.WaitForSeconds(fadeOutTime + Time.deltaTime,
                    () =>
                    {
                        ObjectPoolManager.Instance.ReturnObjectToPool(textObject.gameObject);
                    }));
            });
        }

        private void ToggleTextPopupVisual(PopupText textObject, float visualDuration, bool visible)
        {
            foreach (var image in textObject.images)
            {
                image.DOFade(visible ? 0 : 1, 0);
                image.DOFade(visible ? 1 : 0, visualDuration)
                    .SetEase(visible ? popupEaseType : fadeOutEaseType);
            }
            
            textObject.text.DOFade(visible ? 0 : 1, 0);
            textObject.text.DOFade(visible ? 1 : 0, visualDuration)
                .SetEase(visible ? popupEaseType : fadeOutEaseType);
        }
    }
}