using SpriteDicing;
using UnityEngine;

[RequireComponent(typeof(DicedSpriteRenderer))]
public class GetSpriteTest : MonoBehaviour
{
    public DicedSpriteAtlas Atlas;
    public string SpriteName;

    private DicedSpriteRenderer dicedSpriteRenderer;

    private void Awake ()
    {
        dicedSpriteRenderer = GetComponent<DicedSpriteRenderer>();
    }

    private void OnEnable ()
    {
        //dicedSpriteRenderer.SetDicedSprite(Atlas.GetSprite(SpriteName));
    }

    private void OnDisable ()
    {
        dicedSpriteRenderer.SetDicedSprite(null);
    }
}
