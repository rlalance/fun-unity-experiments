using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField]
    private float fireRate = 0.5f;
    [SerializeField]
    private float bulletSpeed = 10f;

    [SerializeField]
    private float playerMovementSpeed = 10f;

    [SerializeField]
    private float mouseSensitivity = 2f;

    private float nextFire = 0f;
    private const string BULLET_TAG = "Bullet";
    private Camera cam;
    private float yaw = 0f;
    private float pitch = 0f;

    private void Awake()
    {
        cam = Camera.main;
        
        yaw = cam.transform.eulerAngles.y;
        pitch = cam.transform.eulerAngles.x;
    }

    void Update()
    {
        // Mouse look controls
        if (Input.GetMouseButton(1)) // Right mouse button for looking around
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            yaw += mouseX;
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, -89f, 89f); // Prevent flipping

            cam.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
            move += cam.transform.forward;
        if (Input.GetKey(KeyCode.S))
            move -= cam.transform.forward;
        if (Input.GetKey(KeyCode.A))
            move -= cam.transform.right;
        if (Input.GetKey(KeyCode.D))
            move += cam.transform.right;

        if (move != Vector3.zero)
            cam.transform.position += move.normalized * playerMovementSpeed * Time.deltaTime;        

        if (Input.GetButton("Fire1") && Time.time > nextFire)
        {
            nextFire = Time.time + fireRate;

            // Get a bullet from the pool instead of instantiating a new one.
            var bullet = ObjectPooler.Instance.GetFromPool(BULLET_TAG, cam.transform.position, Quaternion.identity);

            // Shoot the bullet if it was successfully retrieved from the pool.
            if (bullet != null)
            {
                // Assuming the bullet has a script that handles its movement.
                // You might want to set some properties on the bullet here, like speed or direction.
                bullet.transform.forward = cam.transform.forward; // Set the bullet's direction.
                bullet.SetActive(true); // Activate the bullet if it was deactivated.
                // Optionally, you can set a speed or other properties on the bullet here.
                // For example, if the bullet has a Rigidbody component:
                Rigidbody rb = bullet.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    rb.velocity = cam.transform.forward * bulletSpeed; // Set the bullet's speed.
                }
                else
                {
                    Debug.LogWarning("Bullet does not have a Rigidbody component.");
                }
            }
            else
            {
                Debug.LogWarning("Failed to retrieve a bullet from the pool."); // This won't happen if the pool is set up correctly.
            }
        }
    }
}
