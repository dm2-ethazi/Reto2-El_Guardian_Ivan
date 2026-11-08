using UnityEngine;

namespace PuertaCastillo.Gameplay
{
    /// <summary>
    /// Pasa el dibujo de un objeto a un hijo "Visual" para poder animarlo (embestidas, temblores)
    /// sin mover el Transform raíz, que lleva el collider y el Rigidbody2D.
    /// </summary>
    public static class SpriteVisual
    {
        public static SpriteRenderer MoveToChild(GameObject owner)
        {
            if (!owner.TryGetComponent(out SpriteRenderer source))
            {
                return null;
            }

            var child = new GameObject("Visual");
            child.transform.SetParent(owner.transform, false);
            child.layer = owner.layer;

            SpriteRenderer copy = child.AddComponent<SpriteRenderer>();
            copy.sprite = source.sprite;
            copy.color = source.color;
            copy.flipX = source.flipX;
            copy.flipY = source.flipY;
            copy.drawMode = source.drawMode;
            copy.size = source.size;
            copy.sharedMaterial = source.sharedMaterial;
            copy.sortingLayerID = source.sortingLayerID;
            copy.sortingOrder = source.sortingOrder;
            copy.maskInteraction = source.maskInteraction;
            copy.spriteSortPoint = source.spriteSortPoint;

            source.enabled = false;
            return copy;
        }
    }
}
