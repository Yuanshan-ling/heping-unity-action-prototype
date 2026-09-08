using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeHealth : MonoBehaviour
{
    [Header("ÄÍ¾Ã")]
    [Min(1)]
    [SerializeField] private int hitsToFell = 3;

    [SerializeField] private float hitFlashDuration = 0.12f;

    [Header("µôÂä")]
    [SerializeField] private ItemDefinition woodItem;

    [Min(1)]
    [SerializeField] private int woodAmount = 1;

    private int currentHits;
    private bool isFelled;
    private Coroutine flashCoroutine;

    private readonly List<SpriteRenderer> treeSprites = new();
    private readonly List<Color> originalColors = new();

    private void Awake()
    {
        SpriteRenderer[] renderers =
            GetComponentsInChildren<SpriteRenderer>();

        foreach (SpriteRenderer renderer in renderers)
        {
            treeSprites.Add(renderer);
            originalColors.Add(renderer.color);
        }
    }

    public bool TryChop(PlayerInventory inventory)
    {
        if (isFelled || inventory == null || woodItem == null)
            return false;

        currentHits++;

        if (currentHits >= hitsToFell)
        {
            for (int i = 0; i < woodAmount; i++)
            {
                if (!inventory.AddItem(woodItem))
                    return false;
            }

            isFelled = true;
            Destroy(gameObject);
            return true;
        }

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashRed());

        return true;
    }

    private IEnumerator FlashRed()
    {
        foreach (SpriteRenderer renderer in treeSprites)
        {
            if (renderer != null)
                renderer.color = Color.red;
        }

        yield return new WaitForSeconds(hitFlashDuration);

        for (int i = 0; i < treeSprites.Count; i++)
        {
            if (treeSprites[i] != null)
                treeSprites[i].color = originalColors[i];
        }

        flashCoroutine = null;
    }
}