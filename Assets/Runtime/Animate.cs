using SpriteDicing;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(DicedSpriteRenderer))]
public class Animate : MonoBehaviour
{
    public DicedSpriteAtlas Atlas;
    public float Delay;

    private IEnumerator Start ()
    {
        var renderer = GetComponent<DicedSpriteRenderer>();
        var waitForSeconds = new WaitForSeconds(Delay);
        var sprites = Atlas.GetAllSprites();

        foreach (var sprite in sprites)
        {
            renderer.DicedSprite = sprite;
            yield return waitForSeconds;
        }
    }
}
