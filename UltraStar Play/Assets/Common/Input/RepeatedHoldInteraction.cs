using System.ComponentModel;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Scripting;

#if UNITY_EDITOR
    using UnityEngine.InputSystem.Editor;
    using UnityEditor;
#endif

/**
 * Fires immediately when the Control is actuated.
 * Afterwards, (if still held down) waits for the initialPause to pass and then fires again.
 * Afterwards, (if still held down) waits for the repeatedPause to pass and fires repeatedly.
 */
#if UNITY_EDITOR
    [InitializeOnLoad] // Automatically register in editor.
#endif
[Preserve]
[DisplayName("Repeated Hold")]
public class RepeatedHoldInteraction : IInputInteraction
{
    [RuntimeInitializeOnLoadMethod]
    static void Init()
    {
        // Trigger static constructor
    }
    static RepeatedHoldInteraction()
    {
        InputSystem.RegisterInteraction<RepeatedHoldInteraction>();
    }
    
    private static readonly float defaultButtonPressPoint = 0.5f;
    
    public float initialPause = 0.5f;
    public float repeatedPause = 0.2f;
    
    public float pressPoint = 0.5f;

    private float InitialPauseOrDefault => initialPause > 0.0 ? initialPause : InputSystem.settings.defaultHoldTime;
    private float RepeatedPauseOrDefault => repeatedPause > 0.0 ? repeatedPause : InputSystem.settings.defaultHoldTime;
    private float PressPointOrDefault => pressPoint > 0.0 ? pressPoint : defaultButtonPressPoint;

    private double timePressed;

    /// <inheritdoc />
    public void Process(ref InputInteractionContext context)
    {
        switch (context.phase)
        {
            case InputActionPhase.Waiting:
                if (context.ControlIsActuated(PressPointOrDefault))
                {
                    timePressed = context.time;

                    context.Started();
                    context.PerformedAndStayStarted();
                    context.SetTimeout(InitialPauseOrDefault);
                }
                break;

            case InputActionPhase.Started:
                if (!context.ControlIsActuated())
                {
                    context.Canceled();
                }
                else if (context.time - timePressed >= RepeatedPauseOrDefault)
                {
                    // Perform action but stay in the started phase, because we want to fire again after durationOrDefault 
                    context.PerformedAndStayStarted();
                    // Reset time to fire again after durationOrDefault
                    timePressed = context.time;
                    context.SetTimeout(RepeatedPauseOrDefault);
                }
                break;

            case InputActionPhase.Performed:
                if (!context.ControlIsActuated(PressPointOrDefault))
                {
                    context.Canceled();
                }
                break;
        }
    }
    
    /// <inheritdoc />
    public void Reset()
    {
        timePressed = 0;
    }
}
