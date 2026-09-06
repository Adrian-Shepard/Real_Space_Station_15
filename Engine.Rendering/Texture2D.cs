#nullable disable

using Silk.NET.OpenGL;
using StbImageSharp;
using System;
using System.IO;

namespace Engine.Rendering
{
    public class Texture2D : IDisposable
    {
        public uint Handle { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        private GL _gl;

        public Texture2D(GL gl, string path)
        {
            _gl = gl;
            LoadFromFile(path);
        }

        public Texture2D(GL gl, byte[] pixelData, int width, int height)
        {
            _gl = gl;
            Width = width;
            Height = height;
            CreateTexture(pixelData);
        }

        private void LoadFromFile(string path)
        {
            using var stream = File.OpenRead(path);
            ImageResult result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            Width = result.Width;
            Height = result.Height;
            CreateTexture(result.Data);
        }

        private void CreateTexture(byte[] data)
        {
            Handle = _gl.GenTexture();
            _gl.BindTexture(TextureTarget.Texture2D, Handle);

            // Используем unsafe только для получения указателя
            unsafe
            {
                fixed (byte* ptr = data)
                {
                    _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)Width, (uint)Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
                }
            }

            _gl.TextureParameter(Handle, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            _gl.TextureParameter(Handle, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            _gl.TextureParameter(Handle, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            _gl.TextureParameter(Handle, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        }

        public void Bind(TextureUnit textureUnit = TextureUnit.Texture0)
        {
            _gl.ActiveTexture(textureUnit);
            _gl.BindTexture(TextureTarget.Texture2D, Handle);
        }

        public void Dispose()
        {
            if (Handle != 0)
                _gl.DeleteTexture(Handle);
            Handle = 0;
        }
    }
}