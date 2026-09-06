using System;
using System.Numerics;
using Silk.NET.Input;
using SS15.Client.Core;

namespace SS15.Client.Player
{
    public class PlayerController
    {
        public Vector2 Position { get; set; }
        public float Speed { get; set; } = 250f;
        public float PlayerHalf { get; } = 12f;
        public int Health { get; private set; } = 100;
        public Inventory Inventory { get; } = new();

        private GameMap _map;
        private IKeyboard _keyboard;

        public PlayerController(GameMap map, IKeyboard keyboard, Vector2 startPos)
        {
            _map = map;
            _keyboard = keyboard;
            Position = startPos;
        }

        public void Heal(int amount)
        {
            Health = Math.Clamp(Health + amount, 0, 100);
        }

        public void TakeDamage(int amount)
        {
            Health = Math.Clamp(Health - amount, 0, 100);
        }

        public void Respawn(Vector2 newPos)
        {
            Position = newPos;
            Health = 100;
        }

        public void Update(float deltaTime)
        {
            float distance = Speed * deltaTime;
            Vector2 movement = Vector2.Zero;

            if (_keyboard.IsKeyPressed(Key.W)) movement.Y -= distance;
            if (_keyboard.IsKeyPressed(Key.S)) movement.Y += distance;
            if (_keyboard.IsKeyPressed(Key.A)) movement.X -= distance;
            if (_keyboard.IsKeyPressed(Key.D)) movement.X += distance;

            // Движение по оси X
            Vector2 newPos = new Vector2(Position.X + movement.X, Position.Y);
            if (_map.IsWalkable(newPos, PlayerHalf))
                Position = new Vector2(newPos.X, Position.Y);

            // Движение по оси Y
            newPos = new Vector2(Position.X, Position.Y + movement.Y);
            if (_map.IsWalkable(newPos, PlayerHalf))
                Position = new Vector2(Position.X, newPos.Y);
        }
    }
}