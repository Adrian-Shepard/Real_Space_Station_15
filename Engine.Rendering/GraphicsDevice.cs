#nullable disable

using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;

namespace Engine.Rendering
{
    public class GraphicsDevice : IDisposable
    {
        public IWindow GameWindow { get; private set; }   // переименовано, чтобы не конфликтовать с типом Window
        public GL Gl { get; private set; }

        public GraphicsDevice(int width, int height, string title)
        {
            var options = WindowOptions.Default with
            {
                Size = new Vector2D<int>(width, height),
                Title = title,
                API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.ForwardCompatible, new APIVersion(4, 6))
            };

            GameWindow = Silk.NET.Windowing.Window.Create(options); // явное обращение к статическому методу класса Window
            GameWindow.Load += OnLoadInternal;
            GameWindow.Update += OnUpdateInternal;
            GameWindow.Render += OnRenderInternal;
            GameWindow.Resize += OnResizeInternal;
            GameWindow.Closing += OnClosingInternal;
        }

        protected virtual void OnLoad() { }
        protected virtual void OnUpdate(double deltaTime) { }
        protected virtual void OnRender(double deltaTime) { }
        protected virtual void OnResize(Vector2D<int> size) { }
        protected virtual void OnClosing() { }

        private void OnLoadInternal()
        {
            Gl = GameWindow.CreateOpenGL();
            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            OnLoad();
        }

        private void OnUpdateInternal(double deltaTime) => OnUpdate(deltaTime);
        private void OnRenderInternal(double deltaTime) => OnRender(deltaTime);
        private void OnResizeInternal(Vector2D<int> size) => OnResize(size);
        private void OnClosingInternal() => OnClosing();

        public void Run() => GameWindow.Run();

        public void Dispose()
        {
            Gl?.Dispose();
            GameWindow?.Dispose();
        }
    }
}