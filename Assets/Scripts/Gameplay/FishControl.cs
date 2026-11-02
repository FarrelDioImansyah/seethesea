using UnityEngine;

public class FishPatrol : MonoBehaviour
{
    [Header("Pergerakan Renang")]
    public float moveSpeed = 1.5f;
    public float patrolDistance = 4f; // Jarak renang kiri-kanan
    public bool startFacingRight = true;

    private Vector3 initialPos;
    private bool movingRight;

    private void Start()
    {
        initialPos = transform.position;
        movingRight = startFacingRight;
    }

    private void Update()
    {
        // Renang bolak-balik horizontal (sumbu X)
        float currentOffset = transform.position.x - initialPos.x;

        if (movingRight)
        {
            transform.position += Vector3.right * moveSpeed * Time.deltaTime;
            if (currentOffset >= patrolDistance)
            {
                movingRight = false;
                FlipSprite(false);
            }
        }
        else
        {
            transform.position += Vector3.left * moveSpeed * Time.deltaTime;
            if (currentOffset <= -patrolDistance)
            {
                movingRight = true;
                FlipSprite(true);
            }
        }
    }

    private void FlipSprite(bool faceRight)
    {
        // Putar rotasi Y 180 derajat (atau sesuaikan scale X jika pakai sprite 2D)
        Vector3 rot = transform.eulerAngles;
        rot.y = faceRight ? 0f : 180f;
        transform.eulerAngles = rot;
    }
}