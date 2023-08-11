using ECS3D.ECSEngine.Internal;
using OpenTK;
using System;

namespace ECS3D.ECSEngine.Components
{
    public class CameraComponent : EntityComponent
    {
        // Camera properties
        public Vector3 position { get; set; }
        public Vector3 front { get; set; }
        public Vector3 up { get; set; }
        public float yaw { get; set; }
        public float pitch { get; set; }
        public float movementSpeed { get; set; } = .1f;
        public float mouseSensitivity { get; set; } = .1f;
        public enum CameraMovement { Forward, Backward, Left, Right }
        public float FieldOfView { get; set; }
        public float AspectRatio { get; set; }
        public float NearPlane { get; set; }
        public float FarPlane { get; set; }



        // Calculate the view matrix
        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(position, position + front, up);
        }


        // Calculate the projection matrix
        public Matrix4 GetProjectionMatrix()
        {
            return Matrix4.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, NearPlane, FarPlane);
        }

        public void Move(CameraMovement direction)
        {
            if (direction == CameraMovement.Forward)
                position += front * movementSpeed;
            if (direction == CameraMovement.Backward)
                position -= front * movementSpeed;
            if (direction == CameraMovement.Left)
                position -= Vector3.Normalize(Vector3.Cross(front, up)) * movementSpeed;
            if (direction == CameraMovement.Right)
                position += Vector3.Normalize(Vector3.Cross(front, up)) * movementSpeed;
        }

        // Function to rotate the camera using mouse input

        public void Rotate(Vector2 mouseDelta)
        {
            yaw += mouseDelta.X * mouseSensitivity;
            pitch -= mouseDelta.Y * mouseSensitivity;

            if (pitch > 89.0f)
                pitch = 89.0f;
            if (pitch < -89.0f)
                pitch = -89.0f;

            UpdateFrontVector();
        }

        private void UpdateFrontVector()
        {
            Vector3 newFront = new Vector3();
            newFront.X = (float)(Math.Cos(MathHelper.DegreesToRadians(yaw)) * Math.Cos(MathHelper.DegreesToRadians(pitch)));
            newFront.Y = (float)Math.Sin(MathHelper.DegreesToRadians(pitch));
            newFront.Z = (float)(Math.Sin(MathHelper.DegreesToRadians(yaw)) * Math.Cos(MathHelper.DegreesToRadians(pitch)));

            front = Vector3.Normalize(newFront);
        }


    }


}
