using UnityEngine;

public class RopeZone : MonoBehaviour
{
    [SerializeField] private Transform exitPoint;

    public Transform ExitPoint => exitPoint;
}