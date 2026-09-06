using System;
using System.Numerics;
using Silk.NET.Input;
using SS15.Client.Core;

namespace SS15.Client.Input
{
    public class InputHandler
    {
        private IKeyboard _keyboard;
        private IMouse _mouse;
        private GameMap _map;
        private Func<Vector2> _getCameraPosition;
        private Func<float> _getZoom;
        private Func<Vector2> _getScale;

        // События
        public event Action<int, int> LeftClick;
        public event Action<int, int> PlaceWall;
        public event Action<int, int> PlaceDoor;
        public event Action<int, int> PlaceWire;
        public event Action<int, int> PlaceGenerator;
        public event Action<int, int> PlaceLamp;
        public event Action<int, int> RemoveObject;
        public event Action SwitchHand;
        public event Action PlaceInPocket;
        public event Action UseItem;
        public event Action DropItem;
        public event Action<int> SelectSlot;
        public event Action<int, int, float> ModifyOxygen;
        public event Action<int, int, float> ModifyNitrogen;
        public event Action<int, int, float> ModifyCarbonDioxide;
        public event Action<int, int, float> ModifyTemperature;

        private bool _leftPrev = false;
        private bool _qPrev = false;
        private bool _ePrev = false;
        private bool _tPrev = false;
        private bool _bPrev = false;
        private bool _xPrev = false;
        private bool _zPrev = false;
        private bool _vPrev = false;
        private bool _cPrev = false;
        private bool _gPrev = false;
        private bool _rPrev = false;
        private bool[] _slotPrev = new bool[10];

        public InputHandler(
            IKeyboard keyboard,
            IMouse mouse,
            GameMap map,
            Func<Vector2> getCameraPosition,
            Func<float> getZoom,
            Func<Vector2> getScale)
        {
            _keyboard = keyboard;
            _mouse = mouse;
            _map = map;
            _getCameraPosition = getCameraPosition;
            _getZoom = getZoom;
            _getScale = getScale;
        }

        public void Update(float deltaTime)
        {
            Vector2 mouseScreen = new Vector2(_mouse.Position.X, _mouse.Position.Y);
            Vector2 cameraPos = _getCameraPosition();
            float zoom = _getZoom();
            Vector2 scale = _getScale();

            // Экранные координаты -> виртуальные -> мировые
            Vector2 virtualScreen = mouseScreen / scale; // делим на масштаб, получаем виртуальные координаты
            Vector2 mouseWorld = cameraPos + virtualScreen / zoom;
            int mouseTileX = (int)(mouseWorld.X / _map.TileSize);
            int mouseTileY = (int)(mouseWorld.Y / _map.TileSize);

            // Левая кнопка мыши
            if (_mouse.IsButtonPressed(MouseButton.Left) && !_leftPrev)
            {
                LeftClick?.Invoke(mouseTileX, mouseTileY);
            }
            _leftPrev = _mouse.IsButtonPressed(MouseButton.Left);

            // Клавиши
            if (IsPressed(Key.Q, ref _qPrev))
                DropItem?.Invoke();
            if (IsPressed(Key.E, ref _ePrev))
                PlaceInPocket?.Invoke();
            if (IsPressed(Key.R, ref _rPrev))
                UseItem?.Invoke();
            if (IsPressed(Key.T, ref _tPrev))
                PlaceWall?.Invoke(mouseTileX, mouseTileY);
            if (IsPressed(Key.B, ref _bPrev))
                PlaceDoor?.Invoke(mouseTileX, mouseTileY);
            if (IsPressed(Key.X, ref _xPrev))
                SwitchHand?.Invoke();
            if (IsPressed(Key.Z, ref _zPrev))
                PlaceWire?.Invoke(mouseTileX, mouseTileY);
            if (IsPressed(Key.V, ref _vPrev))
                PlaceGenerator?.Invoke(mouseTileX, mouseTileY);
            if (IsPressed(Key.C, ref _cPrev))
                PlaceLamp?.Invoke(mouseTileX, mouseTileY);
            if (IsPressed(Key.G, ref _gPrev))
                RemoveObject?.Invoke(mouseTileX, mouseTileY);

            // Выбор активного слота
            for (int i = 0; i <= 9; i++)
            {
                if (IsPressed(Key.Number0 + i, ref _slotPrev[i]))
                    SelectSlot?.Invoke(i);
            }

            // Модификаторы атмосферы (зажатие)
            if (_keyboard.IsKeyPressed(Key.U))
                ModifyOxygen?.Invoke(mouseTileX, mouseTileY, 1f);
            if (_keyboard.IsKeyPressed(Key.J))
                ModifyOxygen?.Invoke(mouseTileX, mouseTileY, -1f);
            if (_keyboard.IsKeyPressed(Key.O))
                ModifyNitrogen?.Invoke(mouseTileX, mouseTileY, 1f);
            if (_keyboard.IsKeyPressed(Key.L))
                ModifyNitrogen?.Invoke(mouseTileX, mouseTileY, -1f);
            if (_keyboard.IsKeyPressed(Key.N))
                ModifyCarbonDioxide?.Invoke(mouseTileX, mouseTileY, 0.1f);
            if (_keyboard.IsKeyPressed(Key.M))
                ModifyCarbonDioxide?.Invoke(mouseTileX, mouseTileY, -0.1f);
            if (_keyboard.IsKeyPressed(Key.I))
                ModifyTemperature?.Invoke(mouseTileX, mouseTileY, 10f);
            if (_keyboard.IsKeyPressed(Key.K))
                ModifyTemperature?.Invoke(mouseTileX, mouseTileY, -10f);
        }

        private bool IsPressed(Key key, ref bool previous)
        {
            bool current = _keyboard.IsKeyPressed(key);
            bool result = current && !previous;
            previous = current;
            return result;
        }
    }
}