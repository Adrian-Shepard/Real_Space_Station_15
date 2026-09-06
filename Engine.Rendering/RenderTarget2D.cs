#nullable disable

using Silk.NET.OpenGL;
using System;

namespace Engine.Rendering
{
    public class RenderTarget2D : IDisposable
    {
        public uint Framebuffer { get; private set; }
        public Texture2D Texture { get; private set; }
        private GL _gl;

        public RenderTarget2D(GL gl, int width, int height)
        {
            _gl = gl;

            // Создаём текстуру, в которую будем рендерить
            Texture = new Texture2D(gl, new byte[width * height * 4], width, height);

            // Создаём FBO
            Framebuffer = _gl.GenFramebuffer();
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, Framebuffer);
            _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, Texture.Handle, 0);

            // Проверка
            if (_gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
            {
                throw new Exception("Framebuffer is not complete!");
            }

            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Bind()
        {
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, Framebuffer);
        }

        public void Unbind()
        {
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Dispose()
        {
            if (Framebuffer != 0)
                _gl.DeleteFramebuffer(Framebuffer);
            Texture?.Dispose();
        }
    }
}