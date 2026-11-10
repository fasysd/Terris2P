using UnityEngine;
using UnityEngine.InputSystem;

namespace Terris2P
{
    /// <summary>
    /// Xử lý đầu vào điều khiển độc lập cho Người chơi 1 và Người chơi 2 trên cùng một bàn phím.
    /// P1 (Phía trên / Người tấn công): A/D di chuyển, W xoay, S tăng tốc rơi, Space/LeftShift thả tức thì.
    /// P2 (Phía dưới / Người phòng thủ): ←/→ di chuyển, ↓ xoay, ↑ tăng tốc rơi, RightShift/Enter thả tức thì.
    /// Hỗ trợ DAS (Delayed Auto Shift) và ARR (Auto Repeat Rate) giúp di chuyển ngang mượt mà.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        public PlayerSide side = PlayerSide.Top;

        [Header("Cài đặt DAS & ARR")]
        [Tooltip("Độ trễ trước khi bắt đầu lặp lại di chuyển khi giữ phím trái/phải (giây)")]
        public float dasDelay = 0.16f;
        [Tooltip("Khoảng thời gian giữa các lần di chuyển lặp lại (giây)")]
        public float arrInterval = 0.045f;

        private float leftHoldTimer;
        private float rightHoldTimer;
        private bool leftWasDown;
        private bool rightWasDown;

        public bool RotateTriggered { get; private set; }
        public bool HardDropTriggered { get; private set; }
        public bool SoftDropActive { get; private set; }
        public int MoveHorizontalDelta { get; private set; }

        public void Initialize(PlayerSide playerSide)
        {
            side = playerSide;
            leftHoldTimer = 0f;
            rightHoldTimer = 0f;
            leftWasDown = false;
            rightWasDown = false;
            RotateTriggered = false;
            HardDropTriggered = false;
            SoftDropActive = false;
            MoveHorizontalDelta = 0;
        }

        public void PollInput(float deltaTime)
        {
            MoveHorizontalDelta = 0;
            RotateTriggered = false;
            HardDropTriggered = false;
            SoftDropActive = false;

            bool leftIsPressed = false;
            bool rightIsPressed = false;

            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (side == PlayerSide.Top)
                {
                    // ==========================================
                    // NGƯỜI CHƠI 1 (TRÊN / TẤN CÔNG)
                    // ==========================================
                    // Di chuyển: A / D
                    leftIsPressed = kb.aKey.isPressed;
                    rightIsPressed = kb.dKey.isPressed;

                    // Xoay: W (hoặc F)
                    if (kb.wKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame)
                        RotateTriggered = true;

                    // Tăng tốc độ rơi: S
                    if (kb.sKey.isPressed)
                        SoftDropActive = true;

                    // Thả khối tức thì: Space (hoặc LeftShift)
                    if (kb.spaceKey.wasPressedThisFrame || kb.leftShiftKey.wasPressedThisFrame)
                        HardDropTriggered = true;
                }
                else
                {
                    // ==========================================
                    // NGƯỜI CHƠI 2 (DƯỚI / PHÒNG THỦ)
                    // ==========================================
                    // Di chuyển: Mũi tên Trái / Phải (hoặc Numpad 4 / 6)
                    leftIsPressed = kb.leftArrowKey.isPressed || (kb.numpad4Key != null && kb.numpad4Key.isPressed);
                    rightIsPressed = kb.rightArrowKey.isPressed || (kb.numpad6Key != null && kb.numpad6Key.isPressed);

                    // Xoay: Mũi tên Xuống (DownArrow hoặc Numpad 2 hoặc RightCtrl)
                    if (kb.downArrowKey.wasPressedThisFrame ||
                        (kb.numpad2Key != null && kb.numpad2Key.wasPressedThisFrame) ||
                        (kb.rightCtrlKey != null && kb.rightCtrlKey.wasPressedThisFrame))
                    {
                        RotateTriggered = true;
                    }

                    // Tăng tốc độ rơi: Mũi tên Lên (UpArrow hoặc Numpad 8)
                    if (kb.upArrowKey.isPressed || (kb.numpad8Key != null && kb.numpad8Key.isPressed))
                        SoftDropActive = true;

                    // Thả khối tức thì: RightShift (hoặc Enter / NumpadEnter)
                    if (kb.rightShiftKey.wasPressedThisFrame ||
                        kb.enterKey.wasPressedThisFrame ||
                        (kb.numpadEnterKey != null && kb.numpadEnterKey.wasPressedThisFrame))
                    {
                        HardDropTriggered = true;
                    }
                }
            }

            // Xử lý DAS & ARR cho việc di chuyển mượt mà
            // Giữ phím Trái
            if (leftIsPressed)
            {
                if (!leftWasDown)
                {
                    MoveHorizontalDelta = -1;
                    leftHoldTimer = 0f;
                }
                else
                {
                    leftHoldTimer += deltaTime;
                    if (leftHoldTimer >= dasDelay)
                    {
                        MoveHorizontalDelta = -1;
                        leftHoldTimer = dasDelay - arrInterval;
                    }
                }
                leftWasDown = true;
            }
            else
            {
                leftWasDown = false;
                leftHoldTimer = 0f;
            }

            // Giữ phím Phải
            if (rightIsPressed && MoveHorizontalDelta == 0)
            {
                if (!rightWasDown)
                {
                    MoveHorizontalDelta = 1;
                    rightHoldTimer = 0f;
                }
                else
                {
                    rightHoldTimer += deltaTime;
                    if (rightHoldTimer >= dasDelay)
                    {
                        MoveHorizontalDelta = 1;
                        rightHoldTimer = dasDelay - arrInterval;
                    }
                }
                rightWasDown = true;
            }
            else if (!rightIsPressed)
            {
                rightWasDown = false;
                rightHoldTimer = 0f;
            }
        }
    }
}
