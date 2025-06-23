using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -9.81f;

    private CharacterController _controller;
    private Vector3 _velocity;

    private Camera _camera;
    private Transform _cameraTransform;

    public override void OnNetworkSpawn()
    {
        _controller = GetComponent<CharacterController>();

        if (IsOwner)
        {
            _camera = Camera.main;
            _cameraTransform = _camera.transform;

            // Привязываем камеру к игроку
            _cameraTransform.SetParent(transform);
            _cameraTransform.localPosition = new Vector3(0, 10, -10);
            _cameraTransform.localEulerAngles = new Vector3(45, 0, 0);
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        HandleMovement();
    }

    private void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 input = new Vector3(h, 0, v).normalized;
        Vector3 move = input * moveSpeed;

        // Обновляем вертикальную скорость
        if (_controller.isGrounded && _velocity.y < 0f)
        {
            _velocity.y = -2f; // Чтобы стабильно прилипал к земле
        }
        else
        {
            _velocity.y += gravity * Time.deltaTime;
        }

        // Финальное движение
        Vector3 finalMove = (move + _velocity) * Time.deltaTime;
        _controller.Move(finalMove);
    }
}