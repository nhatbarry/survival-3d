using UnityEngine;
using UnityEngine.UI;

namespace CompleteProject
{
    /// <summary>
    /// Helpers for building uGUI elements from code.
    /// </summary>
    public static class RuntimeUI
    {
        public static RectTransform CreateRect (string objectName, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject (objectName, typeof (RectTransform));

            RectTransform rect = (RectTransform) go.transform;
            rect.SetParent (parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            return rect;
        }


        public static void Stretch (RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2 (0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }


        public static Image CreateImage (string objectName, Transform parent, Vector2 anchor, Vector2 position, Vector2 size,
            Color colour, bool raycastTarget = false)
        {
            RectTransform rect = CreateRect (objectName, parent, anchor, position, size);

            Image image = rect.gameObject.AddComponent <Image> ();
            image.color = colour;
            image.raycastTarget = raycastTarget;

            return image;
        }


        public static Text CreateText (string objectName, Transform parent, Font font, Vector2 anchor, Vector2 position, Vector2 size,
            int fontSize, TextAnchor alignment, Color colour, bool outline = true)
        {
            RectTransform rect = CreateRect (objectName, parent, anchor, position, size);

            Text text = rect.gameObject.AddComponent <Text> ();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = colour;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            // Never block touches meant for the joysticks or buttons underneath.
            text.raycastTarget = false;

            if(outline)
            {
                Outline textOutline = rect.gameObject.AddComponent <Outline> ();
                textOutline.effectColor = new Color (0f, 0f, 0f, 0.6f);
            }

            return text;
        }
    }
}
