using System;
using System.Drawing;
using System.Numerics;
using MintyCore.FontStashSharp;
using MintyCore.FontStashSharp.Interfaces;
using MintyCore.FontStashSharp.RichText;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Myra.Platform;
using MintyCore.Myra.Utility;
using MintyCore.UI;

namespace MintyCore.Myra.Graphics2D
{
    public enum TextureFiltering
    {
        Nearest,
        Linear
    }

    public partial class RenderContext : IDisposable
    {
        private readonly IMyraRenderer _renderer;
        private readonly FontStashRenderer? _fontStashRenderer;
        private readonly FontStashRenderer2? _fontStashRenderer2;

        private VertexPositionColorTexture _topLeft = new VertexPositionColorTexture(),
            _topRight = new VertexPositionColorTexture(),
            _bottomLeft = new VertexPositionColorTexture(),
            _bottomRight = new VertexPositionColorTexture();

        private bool _beginCalled;
        private Rectangle _scissor;
        private TextureFiltering _textureFiltering = TextureFiltering.Nearest;
        public Transform Transform;

        internal Rectangle DeviceScissor
        {
            get { return _renderer.Scissor; }

            set { _renderer.Scissor = value; }
        }


        public Rectangle Scissor
        {
            get { return _scissor; }

            set
            {
                _scissor = value;

                if (MyraEnvironment.DisableClipping)
                {
                    return;
                }


                DeviceScissor = value;
            }
        }

        public float Opacity { get; set; }

        public RenderContext()
        {
            _renderer = MyraEnvironment.Platform.Renderer;

            if (_renderer.RendererType == RendererType.Sprite)
            {
                _fontStashRenderer = new FontStashRenderer(_renderer);
                _fontStashRenderer2 = null;
            }
            else
            {
                _fontStashRenderer = null;
                _fontStashRenderer2 = new FontStashRenderer2(_renderer);
            }
        }

        /// <summary>
        /// Applies opacity
        /// </summary>
        /// <param name="opacity"></param>
        public void AddOpacity(float opacity)
        {
            Opacity *= opacity;
        }

        private void SetTextureFiltering(TextureFiltering value)
        {
            if (_textureFiltering == value)
            {
                return;
            }

            _textureFiltering = value;
            Flush();
        }

        /// <summary>
        /// Draws a texture
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="destinationRectangle"></param>
        /// <param name="sourceRectangle"></param>
        /// <param name="color"></param>
        /// <param name="rotation"></param>
        /// <param name="depth"></param>
        public void Draw(FontTextureWrapper texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, FSColor color,
            float rotation, float depth = 0.0f)
        {
            Vector2 sz;
            if (sourceRectangle is not null)
            {
                sz = new Vector2(sourceRectangle.Value.Width, sourceRectangle.Value.Height);
            }
            else
            {
                Point p;
                if (_fontStashRenderer is not null)
                {
                    p = _fontStashRenderer.TextureManager.GetTextureSize(texture);
                }
                else
                {
                    p = _fontStashRenderer2!.TextureManager.GetTextureSize(texture);
                }

                sz = new Vector2(p.X, p.Y);
            }

            var pos = new Vector2(destinationRectangle.X, destinationRectangle.Y);
            var scale = new Vector2(destinationRectangle.Width / sz.X, destinationRectangle.Height / sz.Y);
            Draw(texture, pos, sourceRectangle, color, rotation, scale, depth);
        }

        /// <summary>
        /// Draws a texture
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="destinationRectangle"></param>
        /// <param name="sourceRectangle"></param>
        /// <param name="color"></param>
        /// <param name="rotation"></param>
        public void Draw(FontTextureWrapper texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, FSColor color,
            float rotation) => Draw(texture, destinationRectangle, sourceRectangle, color, rotation, 0.0f);

        /// <summary>
        /// Draws a texture
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="destinationRectangle"></param>
        /// <param name="sourceRectangle"></param>
        /// <param name="color"></param>
        public void Draw(FontTextureWrapper texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, FSColor color) =>
            Draw(texture, destinationRectangle, sourceRectangle, color, 0);

        /// <summary>
        /// Draws a texture
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="destinationRectangle"></param>
        /// <param name="color"></param>
        public void Draw(FontTextureWrapper texture, Rectangle destinationRectangle, FSColor color) =>
            Draw(texture, destinationRectangle, null, color, 0);

        /// <summary>
        /// Draws a texture
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="position"></param>
        /// <param name="sourceRectangle"></param>
        /// <param name="color"></param>
        /// <param name="rotation"></param>
        /// <param name="depth"></param>
        public void Draw(FontTextureWrapper texture, Vector2 position, Rectangle? sourceRectangle, FSColor color, float rotation,
            Vector2 scale, float depth = 0.0f)
        {
            SetTextureFiltering(TextureFiltering.Nearest);
            color = CrossEngineStuff.MultiplyColor(color, Opacity);
            scale *= Transform.Scale;
            rotation += Transform.Rotation;


            if (_fontStashRenderer is not null)
            {
                position = Transform.Apply(position);
                _renderer.DrawSprite(texture, position, sourceRectangle, color, rotation, scale, depth);
            }
            else
            {
                Rectangle r;
                if (sourceRectangle is not null)
                {
                    r = sourceRectangle.Value;
                }
                else
                {
                    var textureSize = _fontStashRenderer2!.TextureManager.GetTextureSize(texture);
                    r = new Rectangle(0, 0, textureSize.X, textureSize.Y);
                }

                var size = new Vector2(scale.X * r.Width, scale.Y * r.Height);
                _renderer.DrawQuad(texture, color, position, ref Transform.Matrix, depth, size, r,
                    ref _topLeft, ref _topRight, ref _bottomLeft, ref _bottomRight);
            }
        }

        /// <summary>
        /// Draws a texture
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="pos"></param>
        /// <param name="color"></param>
        /// <param name="scale"></param>
        /// <param name="rotation"></param>
        public void Draw(FontTextureWrapper texture, Vector2 pos, FSColor color, Vector2 scale, float rotation = 0.0f) =>
            Draw(texture, pos, null, color, rotation, scale);

        /// <summary>
        /// Draws a texture
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="position"></param>
        /// <param name="sourceRectangle"></param>
        /// <param name="color"></param>
        /// <param name="rotation"></param>
        public void Draw(FontTextureWrapper texture, Vector2 position, Rectangle? sourceRectangle, FSColor color,
            float rotation) =>
            Draw(texture, position, sourceRectangle, color, rotation, Vector2.One);

        /// <summary>
        /// Draws a texture
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="position"></param>
        /// <param name="sourceRectangle"></param>
        /// <param name="color"></param>
        public void Draw(FontTextureWrapper texture, Vector2 position, Rectangle? sourceRectangle, FSColor color) =>
            Draw(texture, position, sourceRectangle, color, 0, Vector2.One);

        /// <summary>
        /// Draws a texture
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="position"></param>
        /// <param name="color"></param>
        public void Draw(FontTextureWrapper texture, Vector2 position, FSColor color) =>
            Draw(texture, position, null, color, 0, Vector2.One);

        private void SetTextTextureFiltering()
        {
            if (!MyraEnvironment.SmoothText)
            {
                SetTextureFiltering(TextureFiltering.Nearest);
            }
            else
            {
                SetTextureFiltering(TextureFiltering.Linear);
            }
        }

        /// <summary>
        /// Draws a text
        /// </summary>
        /// <param name="text">The text which will be drawn.</param>
        /// <param name="position">The drawing location on screen.</param>
        /// <param name="color">A color mask.</param>
        /// <param name="rotation">A rotation of this text in radians.</param>
        /// <param name="scale">A scaling of this text.</param>
        /// <param name="layerDepth">A depth of the layer of this string.</param>
        public void DrawString(SpriteFontBase font, string text, Vector2 position, FSColor color, Vector2 scale,
            float rotation, float layerDepth = 0.0f)
        {
            SetTextTextureFiltering();
            color = CrossEngineStuff.MultiplyColor(color, Opacity);
            position = Transform.Apply(position);

            scale *= Transform.Scale;
            rotation += Transform.Rotation;


            if (_fontStashRenderer is not null)
            {
                font.DrawText(_fontStashRenderer, text, position, color, rotation, Vector2.Zero, scale);
            }
            else
            {
                font.DrawText(_fontStashRenderer2!, text, position, color, rotation, Vector2.Zero, scale);
            }
        }

        public void DrawString(SpriteFontBase font, string text, Vector2 position, FSColor color, Vector2 scale,
            float layerDepth = 0.0f) =>
            DrawString(font, text, position, color, scale, 0, layerDepth);

        /// <summary>
        /// Draws a text
        /// </summary>
        /// <param name="text">The text which will be drawn.</param>
        /// <param name="position">The drawing location on screen.</param>
        /// <param name="color">A color mask.</param>
        /// <param name="layerDepth">A depth of the layer of this string.</param>
        public void DrawString(SpriteFontBase font, string text, Vector2 position, FSColor color,
            float layerDepth = 0.0f) =>
            DrawString(font, text, position, color, Vector2.One, 0, layerDepth);

        /// <summary>
        /// Draws a rich text
        /// </summary>
        /// <param name="richText">The text which will be drawn.</param>
        /// <param name="position">The drawing location on screen.</param>
        /// <param name="color">A color mask.</param>
        /// <param name="sourceScale">A scaling of this text.</param>
        /// <param name="rotation">A rotation of this text in radians.</param>
        /// <param name="layerDepth">A depth of the layer of this string.</param>
        public void DrawRichText(RichTextLayout richText, Vector2 position, FSColor color,
            Vector2? sourceScale = null, float rotation = 0, float layerDepth = 0.0f,
            TextHorizontalAlignment horizontalAlignment = TextHorizontalAlignment.Left)
        {
            SetTextTextureFiltering();
            color = CrossEngineStuff.MultiplyColor(color, Opacity);
            position = Transform.Apply(position);

            var scale = sourceScale ?? Vector2.One;

            scale *= Transform.Scale;
            rotation += Transform.Rotation;


            if (_fontStashRenderer is not null)
            {
                richText.Draw(_fontStashRenderer, position, color, rotation, Vector2.Zero, scale, layerDepth,
                    horizontalAlignment);
            }
            else
            {
                richText.Draw(_fontStashRenderer2!, position, color, rotation, Vector2.Zero, scale, layerDepth,
                    horizontalAlignment);
            }
        }

        public void Begin()
        {
            _renderer.Begin(_textureFiltering);

            _beginCalled = true;
        }

        public void End()
        {
            _renderer.End();
            _beginCalled = false;
        }

        public void Flush()
        {
            if (!_beginCalled)
            {
                return;
            }

            End();
            Begin();
        }

        private void ReleaseUnmanagedResources()
        {
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~RenderContext()
        {
            ReleaseUnmanagedResources();
        }
    }
}