using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private CharacterController controller;
    private Vector3 move;
    public float forwardSpeed = 10f;
    public float maxSpeed = 20f;

    private int desiredLane = 1; // 0: left, 1: middle, 2: right
    public float laneDistance = 2.5f; // Distance between lanes

    public bool isGrounded;
    public LayerMask groundLayer;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;

    public float gravity = -12f;
    public float jumpHeight = 2f;
    private Vector3 velocity;

    public Animator animator;
    private bool isSliding = false;
    public float slideDuration = 1.5f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Time.timeScale = 1.2f;
    }

    private void FixedUpdate()
    {
        if (!PlayerManager.isGameStarted || PlayerManager.gameOver)
            return;

        // Increase Speed gradually
        if (forwardSpeed < maxSpeed)
            forwardSpeed += 0.1f * Time.fixedDeltaTime;
    }

    void Update()
    {
        if (!PlayerManager.isGameStarted || PlayerManager.gameOver)
            return;

        animator.SetBool("isGameStarted", true);
        move.z = forwardSpeed;

        // Check if grounded
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        animator.SetBool("isGrounded", isGrounded);

        if (isGrounded && velocity.y < 0)
            velocity.y = -1f;

        // Jump
        if (isGrounded && (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)))
            Jump();

        // Slide
        if ((Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) && !isSliding)
        {
            if (isGrounded)
                StartCoroutine(Slide());
            else
                velocity.y = -10; // Quick fall
        }

        // Lane switching
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            desiredLane++;
            if (desiredLane == 3)
                desiredLane = 2;
        }

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            desiredLane--;
            if (desiredLane == -1)
                desiredLane = 0;
        }

        // Target position for lanes
        Vector3 targetPosition = transform.position.z * transform.forward + transform.position.y * transform.up;
        if (desiredLane == 0)
            targetPosition += Vector3.left * laneDistance;
        else if (desiredLane == 2)
            targetPosition += Vector3.right * laneDistance;

        // Smooth lane movement
        Vector3 diff = targetPosition - transform.position;
        Vector3 moveDir = diff.normalized * 30 * Time.deltaTime;
        if (moveDir.sqrMagnitude < diff.magnitude)
            controller.Move(moveDir);
        else
            controller.Move(diff);

        // Gravity
        velocity.y += gravity * Time.deltaTime;

        // Apply movement
        controller.Move((move + velocity) * Time.deltaTime);
    }

    private void Jump()
    {
        StopCoroutine("Slide");
        animator.SetBool("isSliding", false);
        animator.SetTrigger("jump");
        controller.center = Vector3.zero;
        controller.height = 2;
        isSliding = false;

        velocity.y = Mathf.Sqrt(jumpHeight * 2 * -gravity);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.transform.CompareTag("Obstacle"))
        {
            PlayerManager.gameOver = true;
            FindObjectOfType<AudioManager>().PlaySound("GameOver");
        }
    }

    private IEnumerator Slide()
    {
        isSliding = true;
        animator.SetBool("isSliding", true);
        yield return new WaitForSeconds(0.25f / Time.timeScale);
        controller.center = new Vector3(0, -0.5f, 0);
        controller.height = 1;

        yield return new WaitForSeconds((slideDuration - 0.25f) / Time.timeScale);

        animator.SetBool("isSliding", false);
        controller.center = Vector3.zero;
        controller.height = 2;
        isSliding = false;
    }
}
