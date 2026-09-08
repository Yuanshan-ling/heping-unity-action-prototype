using System;
using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    [Min(0)]
    [SerializeField] private int coins = 0;

    public int Coins => coins;

    public event Action Changed;

    public void AddCoins(int amount)
    {
        if (amount <= 0)
            return;

        coins += amount;
        Changed?.Invoke();
    }

    public bool TrySpend(int amount)
    {
        if (amount < 0 || coins < amount)
            return false;

        coins -= amount;
        Changed?.Invoke();
        return true;
    }
}