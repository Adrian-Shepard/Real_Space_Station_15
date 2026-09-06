using System;
using System.Numerics;
using Engine.Rendering;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SS15.Client.Core;
using SS15.Client.Input;
using SS15.Client.NTUI;
using SS15.Client.Render;
using SS15.Client.Render.GeneratedSprites;
using SS15.Client.Rendering;
using SS15.Client.Systems;
using SS15.Client.Objects;

namespace SS15.Client
{
    public class MainGame : GraphicsDevice
    {
        private SpriteBatch _spriteBatch;
        private Camera2D _camera;
        private IKeyboard _keyboard;
        private IMouse _mouse;

        private GameState _gameState;
        private WorldRenderer _worldRenderer;
        private HudRenderer _hudRenderer;
        private InputHandler _inputHandler;

        // Текстуры
        private Texture2D _floorTex;
        private Texture2D _spaceTex;
        private Texture2D _wallSingleTex, _wallNorthSouthTex, _wallWestEastTex;
        private Texture2D _wallEastSouthTex, _wallNorthEastTex, _wallSouthWestTex, _wallWestNorthTex;
        private Texture2D _gridTex, _playerTex, _itemTex, _whiteTexture, _slotTexture;
        private Texture2D _wireTex, _generatorTex, _lampOnTex, _lampOffTex;
        private Texture2D _doorOpenTex, _doorClosedTex;
        private Texture2D _armsPreviewTex;
        private Texture2D _pocketTex;
        private Texture2D _beltTex;
        private Texture2D _backTex;

        // Производительность
        private System.Diagnostics.Stopwatch _fpsTimer;
        private int _frameCount;
        private float _fps;
        private float _playerUpdateMs;
        private float _atmosUpdateMs;
        private float _powerUpdateMs;
        private float _healthUpdateMs;
        private float _renderMs;

        private int _activeSlotIndex = 0;
        private int _activeHandIndex = 0;

        private const float Zoom = 1.6f;
        private const int VirtualWidth = 800;
        private const int VirtualHeight = 600;

        private float _actualWidth;
        private float _actualHeight;
        private Vector2 _windowScale;

        public MainGame() : base(800, 600, "Space Station 15")
        {
        }

        protected override void OnLoad()
        {
            // Загрузка текстур
            _floorTex = SpriteGenerator.LoadTexture(Gl, "Sprites/Tiles/Floors/floor.png", (gl) => SpriteGenerator.CreateFloorTexture(gl, 32));
            _spaceTex = SpriteGenerator.LoadTexture(Gl, "Sprites/Tiles/space.png", (gl) => SpriteGenerator.CreateSpaceTexture(gl, 32));
            _wallSingleTex = SpriteGenerator.LoadTexture(Gl, "Sprites/Tiles/Walls/Wall_METAL/Wall_METAL_SINGLE.png", (gl) => SpriteGenerator.CreateWallTexture(gl, 32));
            _wallNorthSouthTex = SpriteGenerator.LoadTexture(Gl, "Sprites/Tiles/Walls/Wall_METAL/Directions/Wall_METAL_North_South.png", (gl) => SpriteGenerator.CreateWallTexture(gl, 32));
            _wallWestEastTex = SpriteGenerator.LoadTexture(Gl, "Sprites/Tiles/Walls/Wall_METAL/Directions/Wall_METAL_West_East.png", (gl) => SpriteGenerator.CreateWallTexture(gl, 32));
            _wallEastSouthTex = SpriteGenerator.LoadTexture(Gl, "Sprites/Tiles/Walls/Wall_METAL/Directions/Edges/Wall_METAL_East_South.png", (gl) => SpriteGenerator.CreateWallTexture(gl, 32));
            _wallNorthEastTex = SpriteGenerator.LoadTexture(Gl, "Sprites/Tiles/Walls/Wall_METAL/Directions/Edges/Wall_METAL_North_East.png", (gl) => SpriteGenerator.CreateWallTexture(gl, 32));
            _wallSouthWestTex = SpriteGenerator.LoadTexture(Gl, "Sprites/Tiles/Walls/Wall_METAL/Directions/Edges/Wall_METAL_South_West.png", (gl) => SpriteGenerator.CreateWallTexture(gl, 32));
            _wallWestNorthTex = SpriteGenerator.LoadTexture(Gl, "Sprites/Tiles/Walls/Wall_METAL/Directions/Edges/Wall_METAL_West_North.png", (gl) => SpriteGenerator.CreateWallTexture(gl, 32));
            _playerTex = SpriteGenerator.LoadTexture(Gl, "Sprites/player.png", (gl) => SpriteGenerator.CreatePlayerTexture(gl, 32));

            _wireTex = WireSpriteGenerator.LoadOrCreate(Gl);
            _generatorTex = GeneratorSpriteGenerator.LoadOrCreate(Gl);
            _lampOffTex = LampSpriteGenerator.LoadOrCreate(Gl);
            _lampOnTex = LampSpriteGenerator.LoadOrCreateOn(Gl);
            _doorOpenTex = DoorSpriteGenerator.LoadOrCreateOpen(Gl);
            _doorClosedTex = DoorSpriteGenerator.LoadOrCreateClosed(Gl);

            _armsPreviewTex = SpriteGenerator.LoadTexture(Gl, "Sprites/Misc/UI/Arms_Preview.png");
            _pocketTex = SpriteGenerator.LoadTexture(Gl, "Sprites/Misc/UI/UI_Pocket.png");
            _beltTex = SpriteGenerator.LoadTexture(Gl, "Sprites/Misc/UI/UI_Belt.png");
            _backTex = SpriteGenerator.LoadTexture(Gl, "Sprites/Misc/UI/UI_Back.png");

            _gridTex = SpriteGenerator.CreateSolidTexture(Gl, new byte[] { 0, 0, 0, 255 });
            _itemTex = SpriteGenerator.CreateSolidTexture(Gl, new byte[] { 255, 255, 255, 255 });
            _whiteTexture = SpriteGenerator.CreateSolidTexture(Gl, new byte[] { 255, 255, 255, 255 });
            _slotTexture = SpriteGenerator.CreateSolidTexture(Gl, new byte[] { 50, 50, 50, 255 });

            _spriteBatch = new SpriteBatch(Gl);
            _camera = new Camera2D { ViewportWidth = VirtualWidth, ViewportHeight = VirtualHeight };

            // Фактические размеры окна и viewport
            _actualWidth = GameWindow.Size.X;
            _actualHeight = GameWindow.Size.Y;
            Gl.Viewport(0, 0, (uint)_actualWidth, (uint)_actualHeight);
            UpdateWindowScale();

            // Производительность
            _fpsTimer = System.Diagnostics.Stopwatch.StartNew();
            _frameCount = 0;
            _fps = 0;
            _playerUpdateMs = 0;
            _atmosUpdateMs = 0;
            _powerUpdateMs = 0;
            _healthUpdateMs = 0;
            _renderMs = 0;

            var inputContext = GameWindow.CreateInput();
            _keyboard = inputContext.Keyboards[0];
            _mouse = inputContext.Mice[0];

            // Игровое состояние
            _gameState = new GameState();
            _gameState.Initialize(_keyboard);

            // Рендерер мира
            _worldRenderer = new WorldRenderer(_spriteBatch, _camera, _gameState.Map, _gameState.Player, _gameState.Power);
            _worldRenderer.SetTextures(
                _floorTex, _spaceTex, _wallSingleTex,
                _wallNorthSouthTex, _wallWestEastTex,
                _wallEastSouthTex, _wallNorthEastTex, _wallSouthWestTex, _wallWestNorthTex,
                _gridTex, _playerTex, _itemTex,
                _whiteTexture,
                _generatorTex, _lampOnTex, _lampOffTex,
                _doorOpenTex, _doorClosedTex);

            // Рендерер HUD
            _hudRenderer = new HudRenderer(
                _spriteBatch, _camera, _mouse,
                _gameState.Player, _gameState.Atmosphere, _gameState.Power,
                new FontRenderer(Gl), _slotTexture, _whiteTexture, _itemTex,
                _armsPreviewTex, _pocketTex, _beltTex, _backTex);

            // Обработчик ввода
            _inputHandler = new InputHandler(_keyboard, _mouse, _gameState.Map,
                () => _camera.Position, () => _camera.Zoom, () => _windowScale);
            SubscribeInputEvents();

            UpdateCamera();
        }

        private void UpdateWindowScale()
        {
            _windowScale = new Vector2(_actualWidth / VirtualWidth, _actualHeight / VirtualHeight);
        }

        protected override void OnResize(Vector2D<int> size)
        {
            base.OnResize(size);
            _actualWidth = size.X;
            _actualHeight = size.Y;
            Gl.Viewport(0, 0, (uint)size.X, (uint)size.Y);
            UpdateWindowScale();
        }

        private void SubscribeInputEvents()
        {
            _inputHandler.LeftClick += OnLeftClick;
            _inputHandler.PlaceWall += OnPlaceWall;
            _inputHandler.PlaceDoor += OnPlaceDoor;
            _inputHandler.PlaceWire += OnPlaceWire;
            _inputHandler.PlaceGenerator += OnPlaceGenerator;
            _inputHandler.PlaceLamp += OnPlaceLamp;
            _inputHandler.RemoveObject += OnRemoveObject;
            _inputHandler.SwitchHand += () => _activeHandIndex = 1 - _activeHandIndex;
            _inputHandler.SelectSlot += (i) => _activeSlotIndex = i;
            _inputHandler.PlaceInPocket += OnPlaceInPocket;
            _inputHandler.UseItem += OnUseItem;
            _inputHandler.DropItem += OnDropItem;
            _inputHandler.ModifyOxygen += OnModifyOxygen;
            _inputHandler.ModifyNitrogen += OnModifyNitrogen;
            _inputHandler.ModifyCarbonDioxide += OnModifyCarbonDioxide;
            _inputHandler.ModifyTemperature += OnModifyTemperature;
        }

        private void OnLeftClick(int tileX, int tileY)
        {
            if (_gameState.Map.GetTile(tileX, tileY) == TileType.Door)
            {
                bool open = _gameState.Map.IsDoorOpen(tileX, tileY);
                _gameState.Map.SetDoorOpen(tileX, tileY, !open);
                _gameState.Atmosphere.SetDoorState(tileX, tileY, !open);
            }
            else
            {
                var items = _gameState.Map.GetItemsAtTile(tileX, tileY);
                if (items != null && items.Count > 0)
                {
                    var item = items[0];
                    _gameState.Map.RemoveItemAtTile(tileX, tileY, item);
                    if (!_gameState.Player.Inventory.AddToHand(item))
                    {
                        if (!_gameState.Player.Inventory.AddToPocket(item))
                        {
                            if (!_gameState.Player.Inventory.AddToBelt(item))
                            {
                                if (!_gameState.Player.Inventory.AddToBack(item))
                                {
                                    _gameState.Map.AddItemToTile(tileX, tileY, item);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void OnPlaceWall(int tileX, int tileY) => _gameState.Atmosphere.SetWall(tileX, tileY, true);
        private void OnPlaceDoor(int tileX, int tileY) { SetTile(tileX, tileY, TileType.Door); _gameState.Atmosphere.SetDoorState(tileX, tileY, false); }
        private void OnPlaceWire(int tileX, int tileY) => SetTile(tileX, tileY, TileType.Wire);
        private void OnPlaceGenerator(int tileX, int tileY) => SetTile(tileX, tileY, TileType.Generator);
        private void OnPlaceLamp(int tileX, int tileY) => SetTile(tileX, tileY, TileType.Lamp);

        private void OnRemoveObject(int tileX, int tileY)
        {
            var tile = _gameState.Map.GetTile(tileX, tileY);
            if (tile == TileType.Floor)
            {
                _gameState.Map.SetTile(tileX, tileY, TileType.Space);
                _gameState.Atmosphere.SetTileToSpace(tileX, tileY);
            }
            else if (tile == TileType.Space)
            {
                _gameState.Map.SetTile(tileX, tileY, TileType.Floor);
                _gameState.Atmosphere.SetTileToFloor(tileX, tileY);
            }
            else
            {
                _gameState.Map.SetTile(tileX, tileY, TileType.Floor);
                _gameState.Atmosphere.SetTileToFloor(tileX, tileY);
            }
        }

        private void SetTile(int x, int y, TileType type)
        {
            if (_gameState.Map.GetTile(x, y) != TileType.Wall &&
                _gameState.Map.GetTile(x, y) != TileType.ReinforcedWall &&
                _gameState.Map.GetTile(x, y) != TileType.Door)
            {
                var obj = ObjectFactory.Create(type);
                if (obj != null)
                {
                    _gameState.Map.SetTile(x, y, obj.TileType);
                    obj.OnPlaced(_gameState.Map, x, y);
                }
            }
        }

        private void OnPlaceInPocket()
        {
            int handIndex = _activeHandIndex;
            var item = _gameState.Player.Inventory.RemoveItem(handIndex);
            if (item != null)
            {
                if (!_gameState.Player.Inventory.AddToPocket(item))
                {
                    if (!_gameState.Player.Inventory.AddToBelt(item))
                    {
                        if (!_gameState.Player.Inventory.AddToBack(item))
                        {
                            _gameState.Player.Inventory.SetItem(handIndex, item);
                        }
                    }
                }
            }
        }

        private void OnUseItem()
        {
            int handIndex = _activeHandIndex;
            var item = _gameState.Player.Inventory.GetItem(handIndex);
            if (item != null)
            {
                _gameState.Player.Inventory.UseItem(handIndex, _gameState.Player);
            }
        }

        private void OnDropItem()
        {
            int handIndex = _activeHandIndex;
            var item = _gameState.Player.Inventory.RemoveItem(handIndex);
            if (item != null)
            {
                int tileX = (int)(_gameState.Player.Position.X / _gameState.Map.TileSize);
                int tileY = (int)(_gameState.Player.Position.Y / _gameState.Map.TileSize);
                _gameState.Map.AddItemToTile(tileX, tileY, item);
            }
        }

        private void OnModifyOxygen(int x, int y, float delta) => _gameState.Atmosphere.ModifyOxygen(x, y, delta);
        private void OnModifyNitrogen(int x, int y, float delta) => _gameState.Atmosphere.ModifyNitrogen(x, y, delta);
        private void OnModifyCarbonDioxide(int x, int y, float delta) => _gameState.Atmosphere.ModifyCarbonDioxide(x, y, delta);
        private void OnModifyTemperature(int x, int y, float delta) => _gameState.Atmosphere.ModifyTemperature(x, y, delta);

        private void UpdateCamera()
        {
            _camera.Zoom = Zoom;
            _camera.Position = _gameState.Player.Position - new Vector2(
                _camera.ViewportWidth / (2 * Zoom),
                _camera.ViewportHeight / (2 * Zoom)
            );
        }

        protected override void OnUpdate(double deltaTime)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            _gameState.Update((float)deltaTime);
            _playerUpdateMs = (float)sw.Elapsed.TotalMilliseconds;

            _inputHandler.Update((float)deltaTime);

            UpdateCamera();
        }

        protected override void OnRender(double deltaTime)
        {
            var renderSw = System.Diagnostics.Stopwatch.StartNew();

            Gl.ClearColor(0.1f, 0.1f, 0.1f, 1f);
            Gl.Clear(ClearBufferMask.ColorBufferBit);

            // Мир: обычная проекция на виртуальное разрешение, вьюпорт уже растянут
            var projection = Matrix4x4.CreateOrthographicOffCenter(0, _camera.ViewportWidth, _camera.ViewportHeight, 0, -1, 1);
            var view = _camera.GetViewMatrix();

            _spriteBatch.Begin(projection, view);
            _worldRenderer.Draw();
            _spriteBatch.End();

            // HUD: рисуем с фактическими размерами
            _hudRenderer.Draw(_fps, _playerUpdateMs, _atmosUpdateMs, _powerUpdateMs, _healthUpdateMs, _renderMs,
                              _activeSlotIndex, _activeHandIndex, _actualWidth, _actualHeight);

            renderSw.Stop();
            _renderMs = (float)renderSw.Elapsed.TotalMilliseconds;

            _frameCount++;
            if (_fpsTimer.Elapsed.TotalSeconds >= 1.0)
            {
                _fps = _frameCount / (float)_fpsTimer.Elapsed.TotalSeconds;
                _frameCount = 0;
                _fpsTimer.Restart();
            }
        }
    }
}