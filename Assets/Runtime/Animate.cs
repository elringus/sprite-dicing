using SpriteDicing;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Animate : MonoBehaviour
{
    public DicedSpriteAtlas Atlas;
    public float Delay;

    private IEnumerator Start ()
    {
        var renderer = GetComponent<SpriteRenderer>();
        var waitForSeconds = new WaitForSeconds(Delay);
        var sprites = Atlas.GetAllSprites();

        foreach (var sprite in sprites)
        {
            renderer.sprite = sprite;
            yield return waitForSeconds;
        }
    }
}
