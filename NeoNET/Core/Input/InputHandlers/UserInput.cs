
using Mirror;
using UnityEngine;

namespace NeoFPS.Mirror
{
    /// <summary>
    /// Data received from the server after applying user input.
    /// </summary>
    public struct UserInputResult
    {
        public UserInputResult(double clientNetworkTime, double serverNetworkTime, Vector3 position, float verticalVelocity, Vector3 externalForces)
        {
            ClientNetworkTime = clientNetworkTime;
            ServerNetworkTime = serverNetworkTime;
            Position = position;
            VerticalVelocity = verticalVelocity;
            ExternalForces = externalForces;

        }

        
        public double ClientNetworkTime;
        public double ServerNetworkTime;
        public Vector3 Position;
        public float VerticalVelocity;
        public Vector3 ExternalForces;
    }
    
    
    /// <summary>
    /// Data send to the server about players input.
    /// </summary>
    public class UserInput
    {
        public UserInput() { }
        public UserInput(double networkTime, Vector3 position, Vector3 rotation, Vector2 move, float mag)
        {
            NetworkTime = networkTime;
            Position = position;
            Rotation = rotation;
            Direction = move;
            DirectionScale = mag;
        }


        public double NetworkTime;
        public Vector3 Position;
        public Vector3 Rotation;
        public Vector2 Direction;
        public float DirectionScale;
    }


    /// <summary>
    /// Serializer for UserInput.
    /// </summary>
    public static class UserInputReaderWriter
    {
        public static void WriteDateTime(this NetworkWriter writer, UserInput ui)
        {
            writer.WriteDouble(ui.NetworkTime);
            writer.WriteVector3(ui.Position); //Position, not used right now. Can be used by server to help determine if client is trying to hack.            
            writer.WriteVector3(ui.Rotation); //Rotation, not used right now.
            writer.WriteVector2(ui.Direction);
            writer.WriteSingle(ui.DirectionScale);
        }

        public static UserInput ReadDateTime(this NetworkReader reader)
        {
            UserInput ui = new UserInput(
                reader.ReadDouble(), //NetworkTime.
                reader.ReadVector3(), //Position.
                reader.ReadVector3(), //Rotation.
                reader.ReadVector2(), //MoveDirection.
                reader.ReadSingle()
                );

            return ui;
        }
    }
}