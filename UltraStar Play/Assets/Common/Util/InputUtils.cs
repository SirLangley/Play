using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using PrimeInputActions;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public static class InputUtils
{
    public const float DoubleClickThresholdInSeconds = 0.3f;
    
    public static EKeyboardModifier GetCurrentKeyboardModifier()
    {
        if (Keyboard.current == null)
        {
            return EKeyboardModifier.None;
        }
        
        bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
        bool shift = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
        bool alt = Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed;

        if (ctrl && !shift && !alt)
        {
            return EKeyboardModifier.Ctrl;
        }
        else if (!ctrl && shift && !alt)
        {
            return EKeyboardModifier.Shift;
        }
        else if (!ctrl && !shift && alt)
        {
            return EKeyboardModifier.Alt;
        }
        else if (ctrl && shift && !alt)
        {
            return EKeyboardModifier.CtrlShift;
        }
        else if (ctrl && !shift && alt)
        {
            return EKeyboardModifier.CtrlAlt;
        }
        else if (!ctrl && shift && alt)
        {
            return EKeyboardModifier.ShiftAlt;
        }
        else if (ctrl && shift && alt)
        {
            return EKeyboardModifier.CtrlShiftAlt;
        }
        return EKeyboardModifier.None;
    }

    public static bool AnyKeyboardModifierPressed()
    {
        return InputManager.GetInputAction(R.InputActions.usplay_anyKeyboardModifier).ReadValue<float>() > 0;
    }

    public static bool AnyKeyboardOrMouseOrTouchPressed()
    {
        return AnyKeyboardButtonPressed()
               || AnyMouseButtonPressed()
               || AnyTouchscreenPressed();
    }
    
    public static bool AnyKeyboardButtonPressed()
    {
        return Keyboard.current != null
               && Keyboard.current.anyKey.ReadValue() > 0;
    }

    public static bool AnyMouseButtonPressed()
    {
        return Mouse.current != null
               && (Mouse.current.leftButton.isPressed
                   || Mouse.current.rightButton.isPressed
                   || Mouse.current.middleButton.isPressed);
    }
    
    public static bool AnyTouchscreenPressed()
    {
        return Touch.activeTouches.Count > 0;
    }

    public static bool IsKeyboardShiftPressed()
    {
        return Keyboard.current != null
               && (Keyboard.current.leftShiftKey.isPressed
                   || Keyboard.current.rightShiftKey.isPressed);
    }
    
    public static bool IsKeyboardControlPressed()
    {
        return Keyboard.current != null
               && (Keyboard.current.leftCtrlKey.isPressed
                   || Keyboard.current.rightCtrlKey.isPressed);
    }
    
    public static bool IsKeyboardAltPressed()
    {
        return Keyboard.current != null
               && (Keyboard.current.leftAltKey.isPressed
                   || Keyboard.current.rightAltKey.isPressed);
    }

    public static bool WasPressedOrReleasedInThisFrame(KeyControl key)
    {
        return key.wasPressedThisFrame || key.wasReleasedThisFrame;
    }

    public static Vector2 GetMousePosition()
    {
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
    }

    public static Vector2 GetPointerPositionInPanelCoordinates(PanelHelper panelHelper, bool invertY = false)
    {
        Vector2 pointerScreenCoordinates = new Vector2(Pointer.current.position.x.ReadValue(), Pointer.current.position.y.ReadValue());
        Vector2 pointerPanelCoordinates = panelHelper.ScreenToPanel(pointerScreenCoordinates);
        if (invertY)
        {
            Vector2 screenSizePanelCoordinates = panelHelper.ScreenToPanel(new Vector2(Screen.width, Screen.height));
            return new Vector2(pointerPanelCoordinates.x, screenSizePanelCoordinates.y - pointerPanelCoordinates.y);
        }

        return pointerPanelCoordinates;
    }
}
