using System.Numerics;

namespace Engine.Rendering
{
    public class Camera2D
    {
        public Vector2 Position;
        public float Zoom { get; set; } = 1f;
        public float Rotation { get; set; } = 0f;
        public int ViewportWidth { get; set; }
        public int ViewportHeight { get; set; }

        public Matrix4x4 GetViewMatrix()
        {
            Matrix4x4 translation = Matrix4x4.CreateTranslation(-Position.X, -Position.Y, 0);
            Matrix4x4 rotation = Matrix4x4.CreateRotationZ(Rotation);
            Matrix4x4 scale = Matrix4x4.CreateScale(Zoom, Zoom, 1);
            return translation * rotation * scale;
        }

        public Matrix4x4 GetProjectionMatrix()
        {
            return Matrix4x4.CreateOrthographicOffCenter(0, ViewportWidth, ViewportHeight, 0, -1, 1);
        }
    }
}