using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    [Min(1)]
    [SerializeField] private int value = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerWallet wallet = other.GetComponent<PlayerWallet>();

        if (wallet == null)
            return;

        wallet.AddCoins(value);
        Destroy(gameObject);
    }
}