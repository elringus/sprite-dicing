using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Animate : MonoBehaviour
{
    public Sprite[] Keys;
    [Range(0.01f, 0.25f)]
    public float Delay = 0.08f;

    private IEnumerator Start()
    {
        var renderer = GetComponent<SpriteRenderer>();

        foreach (var key in Keys)
        {
            yield return new WaitForSeconds(Delay);
            renderer.sprite = key;
        }
    }
}
