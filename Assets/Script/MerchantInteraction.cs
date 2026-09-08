using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(MerchantShop))]
public class MerchantInteraction : MonoBehaviour
{
    [SerializeField] private ShopUI shopUI;

    private MerchantShop merchantShop;
    private bool playerInRange;

    private void Awake()
    {
        merchantShop = GetComponent<MerchantShop>();
    }

    private void Update()
    {
        if (!playerInRange || shopUI == null)
            return;

        if (Keyboard.current != null &&
            Keyboard.current.tKey.wasPressedThisFrame)
        {
            shopUI.Open(merchantShop);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerInventory>() != null)
            playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PlayerInventory>() != null)
            playerInRange = false;
    }
}