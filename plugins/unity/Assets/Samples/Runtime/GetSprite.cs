using SpriteDicing;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class GetSprite : MonoBehaviour
{
    public DicedSpriteAtlas Atlas;
    public string SpriteName;

    private SpriteRenderer spriteRenderer;

    private void Awake ()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable ()
    {
        spriteRenderer.sprite = Atlas.GetSprite(SpriteName);
    }

    private void OnDisable ()
    {
        spriteRenderer.sprite = null;
    }
}
