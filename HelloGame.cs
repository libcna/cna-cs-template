using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace CnaDotnetTemplate;

public class HelloGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private BasicEffect _cubeEffect = null!;
    private Texture2D _logo = null!;
    private Texture2D _solid = null!;
    private Vector2 _position;
    private Vector2 _velocity;
    private string _rendererName = "Unknown";
    private float _animationSeconds;
    private bool _supports3D;
    private bool _supportsDepth;
    private readonly int? _frameLimit;
    private int _drawnFrames;

    private const float Maximum2DLogoScale = 1.08f;
    private const float RendererBannerSeconds = 5.0f;
    private const float AnimationSpeed = 2.0f;
    private static readonly VertexPositionTexture[] CubeVertices = CreateLogoCubeVertices();

    public HelloGame(int? frameLimit = null)
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _frameLimit = frameLimit;
        Window.Title = "CNA.NET template - HelloGame";
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnClientSizeChanged;
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _logo = LoadRawTexture("logo.png");

        _solid = new Texture2D(GraphicsDevice, 1, 1);
        _solid.SetData(new[] { Color.White });

        EngineDiagnostics.Capabilities capabilities = EngineDiagnostics.Inspect(GraphicsDevice);
        _rendererName = capabilities.RendererName;
        _supports3D = capabilities.Supports3D;
        _supportsDepth = capabilities.SupportsDepth;
        Window.Title = $"CNA.NET template - HelloGame ({_rendererName})";

        if (_supports3D)
        {
            _cubeEffect = new BasicEffect(GraphicsDevice);
            _cubeEffect.TextureEnabled = true;
            _cubeEffect.Texture = _logo;
            _cubeEffect.LightingEnabled = false;
            _cubeEffect.VertexColorEnabled = false;
        }

        var viewport = GraphicsDevice.Viewport;
        _position = new Vector2(viewport.Width * 0.5f, viewport.Height * 0.5f);
        _velocity = new Vector2(104.0f, 74.0f);

        ReportRendererCapabilities();
    }

    private Texture2D LoadRawTexture(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, Content.RootDirectory, fileName);
        using Stream stream = File.OpenRead(path);
        return Texture2D.FromStream(GraphicsDevice, stream);
    }

    private void ReportRendererCapabilities()
    {
        Console.WriteLine($"CNA.NET template: renderer {_rendererName}");
        Console.WriteLine($"  3D pipeline     : {(_supports3D ? "yes" : "no (2D only)")}");
        Console.WriteLine($"  depth/stencil   : {(_supportsDepth ? "yes" : "no")}");
        Console.WriteLine($"  max texture size: {GetMaxTextureDimension()}");
    }

    private int GetMaxTextureDimension()
    {
        return GraphicsDevice.GraphicsProfile == GraphicsProfile.HiDef ? 4096 : 2048;
    }

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape) || GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
        {
            Exit();
            return;
        }

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _animationSeconds += deltaTime;

        MouseState mouse = Mouse.GetState();
        if (mouse.LeftButton == ButtonState.Pressed)
        {
            _position = new Vector2(mouse.X, mouse.Y);
        }

        if (!_supports3D)
        {
            float movementDelta = Math.Min(deltaTime, 0.1f);
            _position += _velocity * movementDelta;

            var viewport = GraphicsDevice.Viewport;
            float logoSize = Math.Max(_logo.Width, _logo.Height);
            float halfExtent = logoSize * 0.5f * Maximum2DLogoScale * 1.15f;

            float minX = Math.Min(halfExtent, viewport.Width * 0.5f);
            float minY = Math.Min(halfExtent, viewport.Height * 0.5f);
            float maxX = Math.Max(minX, viewport.Width - minX);
            float maxY = Math.Max(minY, viewport.Height - minY);

            if (_position.X < minX) { _position.X = minX; _velocity.X = Math.Abs(_velocity.X); }
            else if (_position.X > maxX) { _position.X = maxX; _velocity.X = -Math.Abs(_velocity.X); }

            if (_position.Y < minY) { _position.Y = minY; _velocity.Y = Math.Abs(_velocity.Y); }
            else if (_position.Y > maxY) { _position.Y = maxY; _velocity.Y = -Math.Abs(_velocity.Y); }
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        if (_supports3D && _supportsDepth)
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.CornflowerBlue, 1.0f, 0);
        else
            GraphicsDevice.Clear(Color.CornflowerBlue);

        if (_supports3D)
            Draw3DLogoCube();
        else
            Draw2DLogo();

        if (_animationSeconds < RendererBannerSeconds)
            DrawRendererBanner();

        base.Draw(gameTime);

        if (_frameLimit is int limit && ++_drawnFrames >= limit)
        {
            Console.WriteLine($"CNA.NET template: smoke test drew {_drawnFrames} frames; exiting");
            Exit();
        }
    }

    protected override void UnloadContent()
    {
        _cubeEffect?.Dispose();
        _solid?.Dispose();
        _logo?.Dispose();
        _spriteBatch?.Dispose();
        base.UnloadContent();
    }

    private void OnClientSizeChanged(object? sender, EventArgs args)
    {
        if (_logo is null)
        {
            return;
        }

        Viewport viewport = GraphicsDevice.Viewport;
        _position.X = Math.Clamp(_position.X, 0f, viewport.Width);
        _position.Y = Math.Clamp(_position.Y, 0f, viewport.Height);
    }

    private void Draw2DLogo()
    {
        float motionSeconds = _animationSeconds * AnimationSpeed;
        float scale = 0.96f + 0.12f * (float)Math.Sin(motionSeconds * 0.65f);
        float rotation = 0.11f * (float)Math.Sin(motionSeconds * 0.55f);
        Vector2 origin = new Vector2(_logo.Width * 0.5f, _logo.Height * 0.5f);

        _spriteBatch.Begin();
        _spriteBatch.Draw(_logo, _position, null, Color.White, rotation, origin, scale, SpriteEffects.None, 0f);
        _spriteBatch.End();
    }

    private void Draw3DLogoCube()
    {
        var viewport = GraphicsDevice.Viewport;
        float aspectRatio = (float)viewport.Width / Math.Max(1, viewport.Height);
        float motionSeconds = _animationSeconds * AnimationSpeed;
        float scale = 0.88f + 0.10f * (float)Math.Sin(motionSeconds * 0.48f);
        float moveX = 1.15f * (float)Math.Sin(motionSeconds * 0.24f);
        float moveY = 0.70f * (float)Math.Sin(motionSeconds * 0.19f + 1.1f);

        _cubeEffect.World = Matrix.CreateScale(scale) *
                           Matrix.CreateRotationX(motionSeconds * 0.22f) *
                           Matrix.CreateRotationY(motionSeconds * 0.31f) *
                           Matrix.CreateRotationZ(motionSeconds * 0.13f) *
                           Matrix.CreateTranslation(moveX, moveY, 0.0f);
        _cubeEffect.View = Matrix.CreateLookAt(new Vector3(0.0f, 0.0f, 6.0f), Vector3.Zero, Vector3.Up);
        _cubeEffect.Projection = Matrix.CreatePerspectiveFieldOfView(0.78539816339f, aspectRatio, 0.1f, 100.0f);

        GraphicsDevice.BlendState = BlendState.Opaque;
        GraphicsDevice.DepthStencilState = _supportsDepth ? DepthStencilState.Default : DepthStencilState.None;
        GraphicsDevice.RasterizerState = RasterizerState.CullNone;

        foreach (var pass in _cubeEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, CubeVertices, 0, CubeVertices.Length / 3);
        }
    }

    private void DrawRendererBanner()
    {
        var viewport = GraphicsDevice.Viewport;
        int viewportWidth = viewport.Width;
        int glyphColumns = Math.Max(1, _rendererName.Length * 6 - 1);
        int pixelSize = Math.Clamp((viewportWidth - 48) / glyphColumns, 1, 8);
        int textWidth = glyphColumns * pixelSize;
        int textHeight = 7 * pixelSize;
        int textX = (viewportWidth - textWidth) / 2;
        int textY = 28;
        int padding = 14;
        Color translucentWhite = new Color(255, 255, 255, 96);

        _spriteBatch.Begin();
        _spriteBatch.Draw(_solid, new Rectangle(textX - padding, textY - padding, textWidth + padding * 2, textHeight + padding * 2), translucentWhite);

        for (int i = 0; i < _rendererName.Length; i++)
        {
            byte[] rows = GetGlyphRows(_rendererName[i]);
            int charX = textX + i * 6 * pixelSize;

            for (int row = 0; row < 7; row++)
            {
                for (int col = 0; col < 5; col++)
                {
                    if ((rows[row] & (1 << (4 - col))) != 0)
                    {
                        _spriteBatch.Draw(_solid, new Rectangle(charX + col * pixelSize, textY + row * pixelSize, pixelSize, pixelSize), new Color(24, 36, 55, 255));
                    }
                }
            }
        }
        _spriteBatch.End();
    }

    private static byte[] GetGlyphRows(char c)
    {
        return c switch
        {
            'A' => new byte[] { 0x0e, 0x11, 0x11, 0x1f, 0x11, 0x11, 0x11 },
            'B' => new byte[] { 0x1e, 0x11, 0x11, 0x1e, 0x11, 0x11, 0x1e },
            'C' => new byte[] { 0x0e, 0x11, 0x10, 0x10, 0x10, 0x11, 0x0e },
            'D' => new byte[] { 0x1e, 0x11, 0x11, 0x11, 0x11, 0x11, 0x1e },
            'E' => new byte[] { 0x1f, 0x10, 0x10, 0x1e, 0x10, 0x10, 0x1f },
            'F' => new byte[] { 0x1f, 0x10, 0x10, 0x1e, 0x10, 0x10, 0x10 },
            'G' => new byte[] { 0x0e, 0x11, 0x10, 0x17, 0x11, 0x11, 0x0e },
            'H' => new byte[] { 0x11, 0x11, 0x11, 0x1f, 0x11, 0x11, 0x11 },
            'I' => new byte[] { 0x1f, 0x04, 0x04, 0x04, 0x04, 0x04, 0x1f },
            'J' => new byte[] { 0x07, 0x02, 0x02, 0x02, 0x12, 0x12, 0x0c },
            'K' => new byte[] { 0x11, 0x12, 0x14, 0x18, 0x14, 0x12, 0x11 },
            'L' => new byte[] { 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x1f },
            'M' => new byte[] { 0x11, 0x1b, 0x15, 0x15, 0x11, 0x11, 0x11 },
            'N' => new byte[] { 0x11, 0x19, 0x15, 0x13, 0x11, 0x11, 0x11 },
            'O' => new byte[] { 0x0e, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0e },
            'P' => new byte[] { 0x1e, 0x11, 0x11, 0x1e, 0x10, 0x10, 0x10 },
            'Q' => new byte[] { 0x0e, 0x11, 0x11, 0x11, 0x15, 0x12, 0x0d },
            'R' => new byte[] { 0x1e, 0x11, 0x11, 0x1e, 0x14, 0x12, 0x11 },
            'S' => new byte[] { 0x0f, 0x10, 0x10, 0x0e, 0x01, 0x01, 0x1e },
            'T' => new byte[] { 0x1f, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04 },
            'U' => new byte[] { 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0e },
            'V' => new byte[] { 0x11, 0x11, 0x11, 0x11, 0x11, 0x0a, 0x04 },
            'W' => new byte[] { 0x11, 0x11, 0x11, 0x15, 0x15, 0x15, 0x0a },
            'X' => new byte[] { 0x11, 0x11, 0x0a, 0x04, 0x0a, 0x11, 0x11 },
            'Y' => new byte[] { 0x11, 0x11, 0x0a, 0x04, 0x04, 0x04, 0x04 },
            'Z' => new byte[] { 0x1f, 0x01, 0x02, 0x04, 0x08, 0x10, 0x1f },
            '0' => new byte[] { 0x0e, 0x11, 0x13, 0x15, 0x19, 0x11, 0x0e },
            '1' => new byte[] { 0x04, 0x0c, 0x04, 0x04, 0x04, 0x04, 0x0e },
            '2' => new byte[] { 0x0e, 0x11, 0x01, 0x02, 0x04, 0x08, 0x1f },
            '3' => new byte[] { 0x1e, 0x01, 0x01, 0x0e, 0x01, 0x01, 0x1e },
            '4' => new byte[] { 0x02, 0x06, 0x0a, 0x12, 0x1f, 0x02, 0x02 },
            '5' => new byte[] { 0x1f, 0x10, 0x10, 0x1e, 0x01, 0x01, 0x1e },
            '6' => new byte[] { 0x0e, 0x10, 0x10, 0x1e, 0x11, 0x11, 0x0e },
            '7' => new byte[] { 0x1f, 0x01, 0x02, 0x04, 0x08, 0x08, 0x08 },
            '8' => new byte[] { 0x0e, 0x11, 0x11, 0x0e, 0x11, 0x11, 0x0e },
            '9' => new byte[] { 0x0e, 0x11, 0x11, 0x0f, 0x01, 0x01, 0x0e },
            '_' => new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1f },
            '-' => new byte[] { 0x00, 0x00, 0x00, 0x1f, 0x00, 0x00, 0x00 },
            ' ' => new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
            '(' => new byte[] { 0x02, 0x04, 0x04, 0x04, 0x04, 0x04, 0x02 },
            ')' => new byte[] { 0x08, 0x04, 0x04, 0x04, 0x04, 0x04, 0x08 },
            '.' => new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04 },
            _ => new byte[] { 0x0e, 0x11, 0x01, 0x02, 0x04, 0x00, 0x04 },
        };
    }

    private static VertexPositionTexture[] CreateLogoCubeVertices()
    {
        var vertices = new List<VertexPositionTexture>();

        void AddFace(Vector3 tl, Vector3 tr, Vector3 br, Vector3 bl)
        {
            vertices.Add(new VertexPositionTexture(tl, new Vector2(0, 0)));
            vertices.Add(new VertexPositionTexture(tr, new Vector2(1, 0)));
            vertices.Add(new VertexPositionTexture(br, new Vector2(1, 1)));
            vertices.Add(new VertexPositionTexture(tl, new Vector2(0, 0)));
            vertices.Add(new VertexPositionTexture(br, new Vector2(1, 1)));
            vertices.Add(new VertexPositionTexture(bl, new Vector2(0, 1)));
        }

        AddFace(new Vector3(-1, 1, 1), new Vector3(1, 1, 1), new Vector3(1, -1, 1), new Vector3(-1, -1, 1));
        AddFace(new Vector3(1, 1, -1), new Vector3(-1, 1, -1), new Vector3(-1, -1, -1), new Vector3(1, -1, -1));
        AddFace(new Vector3(1, 1, 1), new Vector3(1, 1, -1), new Vector3(1, -1, -1), new Vector3(1, -1, 1));
        AddFace(new Vector3(-1, 1, -1), new Vector3(-1, 1, 1), new Vector3(-1, -1, 1), new Vector3(-1, -1, -1));
        AddFace(new Vector3(-1, 1, -1), new Vector3(1, 1, -1), new Vector3(1, 1, 1), new Vector3(-1, 1, 1));
        AddFace(new Vector3(-1, -1, 1), new Vector3(1, -1, 1), new Vector3(1, -1, -1), new Vector3(-1, -1, -1));

        return vertices.ToArray();
    }
}
