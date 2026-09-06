#nullable disable

using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Rendering
{
    // Простые структуры цвета и прямоугольника
    public struct Color
    {
        public float R, G, B, A;
        public Color(float r, float g, float b, float a = 1f)
        {
            R = r; G = g; B = b; A = a;
        }
        public static Color White => new Color(1, 1, 1, 1);
    }

    public struct Rectangle
    {
        public int X, Y, Width, Height;
        public Rectangle(int x, int y, int width, int height)
        {
            X = x; Y = y; Width = width; Height = height;
        }
    }

    public class SpriteBatch : IDisposable
    {
        private GL _gl;
        private Shader _shader;
        private uint _vao, _vbo, _ebo;
        private int _maxQuads;
        private List<Vertex> _vertices = new();
        private List<uint> _indices = new();
        private Texture2D _currentTexture; // текстура текущего батча

        private const string VertexShaderSource = @"
#version 460 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec4 aColor;

out vec2 vTexCoord;
out vec4 vColor;

uniform mat4 uProjection;
uniform mat4 uView;

void main()
{
    gl_Position = uProjection * uView * vec4(aPosition, 1.0);
    vTexCoord = aTexCoord;
    vColor = aColor;
}";

        private const string FragmentShaderSource = @"
#version 460 core
in vec2 vTexCoord;
in vec4 vColor;

out vec4 FragColor;

uniform sampler2D uTexture;

void main()
{
    FragColor = texture(uTexture, vTexCoord) * vColor;
}";

        [StructLayout(LayoutKind.Sequential)]
        private struct Vertex
        {
            public Vector3 Position;
            public Vector2 TexCoord;
            public Vector4 Color;
        }

        public SpriteBatch(GL gl, int maxQuads = 10000)
        {
            _gl = gl;
            _maxQuads = maxQuads;
            _shader = new Shader(gl, VertexShaderSource, FragmentShaderSource);

            _vao = _gl.GenVertexArray();
            _vbo = _gl.GenBuffer();
            _ebo = _gl.GenBuffer();

            _gl.BindVertexArray(_vao);

            // Выделяем память в GPU без начальных данных
            unsafe
            {
                _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(_maxQuads * 4 * Marshal.SizeOf<Vertex>()), (void*)0, BufferUsageARB.DynamicDraw);

                _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
                _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(_maxQuads * 6 * sizeof(uint)), (void*)0, BufferUsageARB.DynamicDraw);
            }

            // Настройка вершинных атрибутов
            uint stride = (uint)Marshal.SizeOf<Vertex>();
            unsafe
            {
                _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
                _gl.EnableVertexAttribArray(0);

                _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));
                _gl.EnableVertexAttribArray(1);

                _gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, stride, (void*)(5 * sizeof(float)));
                _gl.EnableVertexAttribArray(2);
            }

            _gl.BindVertexArray(0);
        }

        public void Begin(Matrix4x4 projection, Matrix4x4 view)
        {
            _shader.Use();
            _shader.SetMatrix4("uProjection", projection);
            _shader.SetMatrix4("uView", view);
            _vertices.Clear();
            _indices.Clear();
            _currentTexture = null; // сброс текстуры
        }

        public void Draw(Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Vector2 scale, float rotation, Vector2 origin, Color color)
        {
            // Если текстура сменилась, сбрасываем накопленные спрайты
            if (_currentTexture != texture)
            {
                Flush(); // рисуем предыдущий батч
                _currentTexture = texture;
                texture.Bind();
                _shader.SetInt("uTexture", 0); // используем текстурный юнит 0
            }

            // Если буфер переполнен, сбрасываем
            if (_vertices.Count >= _maxQuads * 4)
                Flush();

            // Вычисляем углы квада с учётом origin и scale
            float left = position.X - origin.X * scale.X;
            float top = position.Y - origin.Y * scale.Y;
            float width = (sourceRectangle?.Width ?? texture.Width) * scale.X;
            float height = (sourceRectangle?.Height ?? texture.Height) * scale.Y;

            Vector2[] positions = new Vector2[]
            {
                new Vector2(left, top),
                new Vector2(left + width, top),
                new Vector2(left + width, top + height),
                new Vector2(left, top + height)
            };

            // UV-координаты
            Vector2[] texCoords;
            if (sourceRectangle.HasValue)
            {
                float u0 = sourceRectangle.Value.X / (float)texture.Width;
                float v0 = sourceRectangle.Value.Y / (float)texture.Height;
                float u1 = (sourceRectangle.Value.X + sourceRectangle.Value.Width) / (float)texture.Width;
                float v1 = (sourceRectangle.Value.Y + sourceRectangle.Value.Height) / (float)texture.Height;
                texCoords = new Vector2[]
                {
                    new Vector2(u0, v0),
                    new Vector2(u1, v0),
                    new Vector2(u1, v1),
                    new Vector2(u0, v1)
                };
            }
            else
            {
                texCoords = new Vector2[]
                {
                    Vector2.Zero,
                    Vector2.UnitX,
                    Vector2.One,
                    Vector2.UnitY
                };
            }

            // Применяем вращение
            if (rotation != 0)
            {
                float cos = MathF.Cos(rotation);
                float sin = MathF.Sin(rotation);
                for (int i = 0; i < positions.Length; i++)
                {
                    var pos = positions[i];
                    positions[i] = new Vector2(
                        cos * pos.X - sin * pos.Y,
                        sin * pos.X + cos * pos.Y
                    );
                }
            }

            // Добавляем вершины и индексы
            uint baseIndex = (uint)_vertices.Count;
            for (int i = 0; i < 4; i++)
            {
                _vertices.Add(new Vertex
                {
                    Position = new Vector3(positions[i], 0),
                    TexCoord = texCoords[i],
                    Color = new Vector4(color.R, color.G, color.B, color.A)
                });
            }

            _indices.AddRange(new uint[]
            {
                baseIndex, baseIndex + 1, baseIndex + 2,
                baseIndex, baseIndex + 2, baseIndex + 3
            });
        }

        public void End()
        {
            Flush();
            _gl.BindVertexArray(0);
            _gl.UseProgram(0);
            _currentTexture = null;
        }

        private unsafe void Flush()
        {
            if (_vertices.Count == 0) return;

            _gl.BindVertexArray(_vao);

            // Загружаем вершины в VBO
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            var vertexArray = _vertices.ToArray();
            fixed (Vertex* ptr = vertexArray)
            {
                _gl.BufferSubData(BufferTargetARB.ArrayBuffer, IntPtr.Zero, (nuint)(vertexArray.Length * sizeof(Vertex)), ptr);
            }

            // Загружаем индексы в EBO
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
            var indexArray = _indices.ToArray();
            fixed (uint* ptr = indexArray)
            {
                _gl.BufferSubData(BufferTargetARB.ElementArrayBuffer, IntPtr.Zero, (nuint)(indexArray.Length * sizeof(uint)), ptr);
            }

            // Рисуем
            _shader.Use();
            _gl.BindVertexArray(_vao);
            _gl.DrawElements(PrimitiveType.Triangles, (uint)_indices.Count, DrawElementsType.UnsignedInt, null);
            _gl.BindVertexArray(0);

            _vertices.Clear();
            _indices.Clear();
        }

        public void Dispose()
        {
            _gl.DeleteVertexArray(_vao);
            _gl.DeleteBuffer(_vbo);
            _gl.DeleteBuffer(_ebo);
            _shader.Dispose();
        }
    }
}