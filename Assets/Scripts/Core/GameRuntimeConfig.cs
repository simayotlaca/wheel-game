using UnityEngine;

public class GameRuntimeConfig : MonoBehaviour
{
    [SerializeField] private int target_frame_rate = 60;

    private void Awake()
    {
        Application.targetFrameRate = target_frame_rate;
    }
}
