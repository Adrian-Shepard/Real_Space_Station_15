using System;
using System.Collections.Generic;
using System.Numerics;
using SS15.Client.Core;

namespace SS15.Client.Systems
{
    public class AtmosphereSystem
    {
        private GameMap _map;
        private AtmosphereData[,] _atmosGrid;
        private float _updateTimer = 0f;
        private const float UpdateInterval = 0.1f;
        private const float DiffusionRate = 0.8f;
        private const float MaxTemperature = 10000f; // ограничение от NaN
        private bool _hasPlasma = false;

        public void Initialize(GameMap map)
        {
            _map = map;
            _atmosGrid = new AtmosphereData[map.Width, map.Height];

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    if (IsWalkable(x, y))
                        _atmosGrid[x, y] = new AtmosphereData();
                    else
                        _atmosGrid[x, y] = null;
                }
            }
        }

        public void Update(float deltaTime)
        {
            _updateTimer += deltaTime;
            if (_updateTimer < UpdateInterval)
                return;
            _updateTimer = 0f;

            DiffuseGases();

            if (_hasPlasma)
                ProcessCombustion();
        }

        public AtmosphereData GetAtmosphereAt(int tileX, int tileY)
        {
            if (tileX < 0 || tileX >= _map.Width || tileY < 0 || tileY >= _map.Height)
                return null;
            return _atmosGrid[tileX, tileY];
        }

        public void ModifyOxygen(int tileX, int tileY, float delta)
        {
            var atmos = GetAtmosphereAt(tileX, tileY);
            if (atmos != null)
                atmos.MolesOxygen = Math.Max(0, atmos.MolesOxygen + delta);
        }

        public void ModifyNitrogen(int tileX, int tileY, float delta)
        {
            var atmos = GetAtmosphereAt(tileX, tileY);
            if (atmos != null)
                atmos.MolesNitrogen = Math.Max(0, atmos.MolesNitrogen + delta);
        }

        public void ModifyCarbonDioxide(int tileX, int tileY, float delta)
        {
            var atmos = GetAtmosphereAt(tileX, tileY);
            if (atmos != null)
                atmos.MolesCarbonDioxide = Math.Max(0, atmos.MolesCarbonDioxide + delta);
        }

        public void ModifyPlasma(int tileX, int tileY, float delta)
        {
            var atmos = GetAtmosphereAt(tileX, tileY);
            if (atmos != null)
            {
                atmos.MolesPlasma = Math.Max(0, atmos.MolesPlasma + delta);
                if (atmos.MolesPlasma > 0.01f)
                    _hasPlasma = true;
            }
        }

        public void ModifyTemperature(int tileX, int tileY, float delta)
        {
            var atmos = GetAtmosphereAt(tileX, tileY);
            if (atmos != null)
                atmos.Temperature = Math.Clamp(atmos.Temperature + delta, 0f, MaxTemperature);
        }

        public void SetWall(int tileX, int tileY, bool wall)
        {
            if (tileX < 0 || tileX >= _map.Width || tileY < 0 || tileY >= _map.Height)
                return;

            _map.SetTile(tileX, tileY, wall ? TileType.Wall : TileType.Floor);
            if (wall)
                _atmosGrid[tileX, tileY] = null;
            else
                if (_atmosGrid[tileX, tileY] == null)
                    _atmosGrid[tileX, tileY] = GetNeighborAverage(tileX, tileY) ?? new AtmosphereData();
        }

        public void SetTileToSpace(int tileX, int tileY)
        {
            if (tileX < 0 || tileX >= _map.Width || tileY < 0 || tileY >= _map.Height)
                return;
            _map.SetTile(tileX, tileY, TileType.Space);
            _atmosGrid[tileX, tileY] = null;
        }

        public void SetTileToFloor(int tileX, int tileY)
        {
            if (tileX < 0 || tileX >= _map.Width || tileY < 0 || tileY >= _map.Height)
                return;
            _map.SetTile(tileX, tileY, TileType.Floor);
            if (_atmosGrid[tileX, tileY] == null)
                _atmosGrid[tileX, tileY] = GetNeighborAverage(tileX, tileY) ?? new AtmosphereData();
        }

        public void SetDoorState(int tileX, int tileY, bool open)
        {
            if (tileX < 0 || tileX >= _map.Width || tileY < 0 || tileY >= _map.Height)
                return;

            if (open)
            {
                if (_atmosGrid[tileX, tileY] == null)
                    _atmosGrid[tileX, tileY] = GetNeighborAverage(tileX, tileY) ?? new AtmosphereData();
            }
            else
            {
                _atmosGrid[tileX, tileY] = null;
            }
        }

        private bool IsWalkable(int x, int y)
        {
            var tile = _map.GetTile(x, y);
            if (tile == TileType.Wall || tile == TileType.ReinforcedWall || tile == TileType.Space)
                return false;
            if (tile == TileType.Door && !_map.IsDoorOpen(x, y))
                return false;
            return true;
        }

        private void DiffuseGases()
        {
            var newGrid = new AtmosphereData[_map.Width, _map.Height];
            for (int y = 0; y < _map.Height; y++)
                for (int x = 0; x < _map.Width; x++)
                    if (_atmosGrid[x, y] != null)
                        newGrid[x, y] = new AtmosphereData
                        {
                            Temperature = _atmosGrid[x, y].Temperature,
                            MolesOxygen = _atmosGrid[x, y].MolesOxygen,
                            MolesNitrogen = _atmosGrid[x, y].MolesNitrogen,
                            MolesCarbonDioxide = _atmosGrid[x, y].MolesCarbonDioxide,
                            MolesPlasma = _atmosGrid[x, y].MolesPlasma
                        };

            for (int y = 0; y < _map.Height; y++)
            {
                for (int x = 0; x < _map.Width; x++)
                {
                    if (_atmosGrid[x, y] == null) continue;

                    TryLeakToSpace(newGrid, x, y, x + 1, y);
                    TryLeakToSpace(newGrid, x, y, x - 1, y);
                    TryLeakToSpace(newGrid, x, y, x, y + 1);
                    TryLeakToSpace(newGrid, x, y, x, y - 1);

                    ExchangeWithNeighbor(newGrid, x, y, x + 1, y);
                    ExchangeWithNeighbor(newGrid, x, y, x - 1, y);
                    ExchangeWithNeighbor(newGrid, x, y, x, y + 1);
                    ExchangeWithNeighbor(newGrid, x, y, x, y - 1);
                }
            }

            _atmosGrid = newGrid;
        }

        private void TryLeakToSpace(AtmosphereData[,] grid, int x1, int y1, int x2, int y2)
        {
            if (x2 < 0 || x2 >= _map.Width || y2 < 0 || y2 >= _map.Height) return;
            if (_map.GetTile(x2, y2) != TileType.Space) return;

            grid[x1, y1].MolesOxygen *= 0.05f;
            grid[x1, y1].MolesNitrogen *= 0.05f;
            grid[x1, y1].MolesCarbonDioxide *= 0.05f;
            grid[x1, y1].MolesPlasma *= 0.05f;
            grid[x1, y1].Temperature = 3f;
        }

        private void ExchangeWithNeighbor(AtmosphereData[,] grid, int x1, int y1, int x2, int y2)
        {
            if (x2 < 0 || x2 >= _map.Width || y2 < 0 || y2 >= _map.Height) return;
            if (grid[x1, y1] == null || grid[x2, y2] == null) return;

            float tempDiff = grid[x2, y2].Temperature - grid[x1, y1].Temperature;
            float tempTransfer = tempDiff * DiffusionRate * 0.5f;
            grid[x1, y1].Temperature = Math.Clamp(grid[x1, y1].Temperature + tempTransfer, 0f, MaxTemperature);
            grid[x2, y2].Temperature = Math.Clamp(grid[x2, y2].Temperature - tempTransfer, 0f, MaxTemperature);

            ExchangeMoles(grid, x1, y1, x2, y2, nameof(AtmosphereData.MolesOxygen));
            ExchangeMoles(grid, x1, y1, x2, y2, nameof(AtmosphereData.MolesNitrogen));
            ExchangeMoles(grid, x1, y1, x2, y2, nameof(AtmosphereData.MolesCarbonDioxide));
            ExchangeMoles(grid, x1, y1, x2, y2, nameof(AtmosphereData.MolesPlasma));
        }

        private void ExchangeMoles(AtmosphereData[,] grid, int x1, int y1, int x2, int y2, string componentName)
        {
            float a1 = GetMoles(grid[x1, y1], componentName);
            float a2 = GetMoles(grid[x2, y2], componentName);
            float diff = a2 - a1;
            float transfer = diff * DiffusionRate * 0.5f;
            SetMoles(grid[x1, y1], componentName, a1 + transfer);
            SetMoles(grid[x2, y2], componentName, a2 - transfer);
        }

        private float GetMoles(AtmosphereData data, string name)
        {
            return name switch
            {
                nameof(AtmosphereData.MolesOxygen) => data.MolesOxygen,
                nameof(AtmosphereData.MolesNitrogen) => data.MolesNitrogen,
                nameof(AtmosphereData.MolesCarbonDioxide) => data.MolesCarbonDioxide,
                nameof(AtmosphereData.MolesPlasma) => data.MolesPlasma,
                _ => 0f
            };
        }

        private void SetMoles(AtmosphereData data, string name, float value)
        {
            switch (name)
            {
                case nameof(AtmosphereData.MolesOxygen): data.MolesOxygen = Math.Max(0, value); break;
                case nameof(AtmosphereData.MolesNitrogen): data.MolesNitrogen = Math.Max(0, value); break;
                case nameof(AtmosphereData.MolesCarbonDioxide): data.MolesCarbonDioxide = Math.Max(0, value); break;
                case nameof(AtmosphereData.MolesPlasma): data.MolesPlasma = Math.Max(0, value); break;
            }
        }

        private void ProcessCombustion()
        {
            bool stillHasPlasma = false;
            for (int y = 0; y < _map.Height; y++)
            {
                for (int x = 0; x < _map.Width; x++)
                {
                    var atmos = _atmosGrid[x, y];
                    if (atmos == null) continue;
                    if (atmos.MolesPlasma <= 0) continue;
                    stillHasPlasma = true;
                    if (atmos.MolesOxygen <= 0) continue;

                    float tempC = atmos.Temperature - 273.15f;
                    if (tempC < 100f) continue;

                    float oxygenRatio = atmos.OxygenPercent / 21f;
                    float burnRate = 0.05f * oxygenRatio;
                    float plasmaBurned = Math.Min(atmos.MolesPlasma, burnRate);
                    float oxygenUsed = plasmaBurned;

                    atmos.MolesPlasma -= plasmaBurned;
                    atmos.MolesOxygen = Math.Max(0, atmos.MolesOxygen - oxygenUsed);
                    atmos.MolesCarbonDioxide += plasmaBurned;
                    atmos.Temperature = Math.Min(atmos.Temperature + 20f * plasmaBurned, MaxTemperature);
                }
            }
            _hasPlasma = stillHasPlasma;
        }

        private AtmosphereData GetNeighborAverage(int x, int y)
        {
            if (x > 0 && _atmosGrid[x - 1, y] != null) return CloneAtmos(_atmosGrid[x - 1, y]);
            if (x < _map.Width - 1 && _atmosGrid[x + 1, y] != null) return CloneAtmos(_atmosGrid[x + 1, y]);
            if (y > 0 && _atmosGrid[x, y - 1] != null) return CloneAtmos(_atmosGrid[x, y - 1]);
            if (y < _map.Height - 1 && _atmosGrid[x, y + 1] != null) return CloneAtmos(_atmosGrid[x, y + 1]);
            return null;
        }

        private AtmosphereData CloneAtmos(AtmosphereData source)
        {
            return new AtmosphereData
            {
                Temperature = source.Temperature,
                MolesOxygen = source.MolesOxygen,
                MolesNitrogen = source.MolesNitrogen,
                MolesCarbonDioxide = source.MolesCarbonDioxide,
                MolesPlasma = source.MolesPlasma
            };
        }
    }
}