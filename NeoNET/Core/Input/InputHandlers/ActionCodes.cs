
namespace NeoFPS.Mirror
{

    /// <summary>
    /// Codes to specify certain actions in the motor.
    /// </summary>
    public enum ActionCodes : byte
    {
        Move = 0,
        Jump = 1,
        Crouch = 2,
        Walking = 4
    }

    /// <summary>
    /// Ways to process action codes within UserInputMove.
    /// </summary>
    public enum ActionCodeProcessingTypes : byte
    {
        Ignore = 1,
        ClientActual = 2,
        ClientReplay = 4,
        ServerAuthoritive = 8
    }

}