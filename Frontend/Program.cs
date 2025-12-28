using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Maths;
using System.Runtime.InteropServices;

namespace Frontend
{
    public static class Program
    {
        // --- 1. Window & GL Resources ---
        private static IWindow _window;
        private static GL _gl;
        private static uint _vao, _vbo, _ebo;
        private static uint _program;
        private static uint _texture;

        // --- 2. Emulation Resources ---
        // This simulates your PPU.ScreenBuffer. 
        // 256 pixels * 240 lines
        private static uint[] _screenBuffer = new uint[256 * 240];

        // --- [NES INTEGRATION] 2. Your Emulator Instance ---
        // private static Bus _nes;
        // private static Cartridge _cart;


        public static void Main(string[] args)
        {
            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(256 * 3, 240 * 3); // Scale 3x
            options.Title = "NES Emulator";
            options.VSync = true; // Use VSync for now to prevent CPU melting

            // --- [NES INTEGRATION] 3. Initialize System ---
            // _cart = new Cartridge("super_mario_bros.nes");
            // if (!_cart.bImageValid) { Console.WriteLine("Invalid ROM"); return; }
            // 
            // _nes = new Bus();
            // _nes.InsertCartridge(_cart);
            // _nes.Reset();

            _window = Window.Create(options);
            _window.Load += OnLoad;
            _window.Render += OnRender;
            _window.Update += OnUpdate;
            _window.Closing += OnClose;

            _window.Run();
        }

        private static unsafe void OnLoad()
        {
            // Initialize OpenGL
            _gl = _window.CreateOpenGL();

            // --- A. Fill Buffer with "Static" (Test Data) ---
            // This proves your rendering pipeline works before you connect the PPU
            // --- TEST DATA INIT (Delete this later) ---
            Random rnd = new Random();
            for (int i = 0; i < _screenBuffer.Length; i++)
            {
                // Format: 0xAABBGGRR (Alpha must be FF)
                byte r = (byte)rnd.Next(255);
                byte g = (byte)rnd.Next(255);
                byte b = (byte)rnd.Next(255);
                _screenBuffer[i] = (uint)(0xFF000000 | (b << 16) | (g << 8) | r);
            }

            // --- B. Create GPU Texture ---
            _texture = _gl.GenTexture();
            _gl.BindTexture(TextureTarget.Texture2D, _texture);

            // Scaling filter: Nearest Neighbor (Keeps pixels sharp/blocky)
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);

            // Allocate memory on GPU (256x240, RGBA)
            _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, 256, 240, 0, PixelFormat.Bgra, PixelType.UnsignedByte, null);

            // --- C. Create Quad (The Screen) ---
            // x, y, z, u, v
            float[] vertices =
            {
                 1.0f,  1.0f, 0.0f, 1.0f, 0.0f, // Top Right
                 1.0f, -1.0f, 0.0f, 1.0f, 1.0f, // Bottom Right
                -1.0f, -1.0f, 0.0f, 0.0f, 1.0f, // Bottom Left
                -1.0f,  1.0f, 0.0f, 0.0f, 0.0f  // Top Left
            };

            uint[] indices = { 0, 1, 3, 1, 2, 3 };

            _vao = _gl.GenVertexArray();
            _gl.BindVertexArray(_vao);

            _vbo = _gl.GenBuffer();
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            fixed (float* buf = vertices)
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)),
                buf, BufferUsageARB.StaticDraw);
            
            _ebo = _gl.GenBuffer();
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
            fixed (uint* buf = indices)
                _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)),
                buf, BufferUsageARB.StaticDraw);
            
            // Attribute 0: Position (3 floats)
            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)0);
            _gl.EnableVertexAttribArray(0);

            // Attribute 1: Texture Coords (2 floats)
            _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)(3 * sizeof(float)));
            _gl.EnableVertexAttribArray(1);

            // --- D. Compile Shaders ---
            // (Function definition at bottom)
            CreateShaderProgram();

            // --- E. Add Inputs
            IInputContext input = _window.CreateInput();

            for (int i = 0; i < input.Keyboards.Count; i++)
                input.Keyboards[i].KeyDown += KeyDown;
        }

        private static void OnUpdate(double deltaTime)
        {
            // --- [NES INTEGRATION] 5. The Clock Loop ---

            // OPTION A: Current Test Code (Updates random pixels)
            _screenBuffer[new Random().Next(0, _screenBuffer.Length)] = 0xFFFFFFFF;

            // OPTION B: Real Emulation (Uncomment this later)
            // while (!_nes.ppu.frameComplete)
            // {
            //     _nes.Clock();
            // }
            // _nes.ppu.frameComplete = false;
        }

        private static unsafe void OnRender(double deltaTime)
        {
            _gl.Clear(ClearBufferMask.ColorBufferBit);

            // 1. Upload the Pixel Buffer to the GPU Texture
            _gl.BindTexture(TextureTarget.Texture2D, _texture);
            
            // --- [NES INTEGRATION] 4. Switch Buffer Pointer ---
            
            // OPTION A: Current Test Code
            fixed (void* data = _screenBuffer)
            {
                _gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, 256, 240, PixelFormat.Bgra, PixelType.UnsignedByte, data);
            }

            // OPTION B: Real PPU (Uncomment this later)
            // fixed (void* data = _nes.ppu.ScreenBuffer)
            // {
            //     _gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, 256, 240, PixelFormat.Bgra, PixelType.UnsignedByte, data);
            // }

            // 2. Draw the Quad
            _gl.UseProgram(_program);
            _gl.BindVertexArray(_vao);
            _gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, (void*)0);
        }

        private static void OnClose()
        {
            // Cleanup
            _gl.DeleteBuffer(_vbo);
            _gl.DeleteBuffer(_ebo);
            _gl.DeleteVertexArray(_vao);
            _gl.DeleteTexture(_texture);
            _gl.DeleteProgram(_program);
        }

        // --- Boilerplate Shader Code ---
        private static void CreateShaderProgram()
        {
            string vertexSource = @"#version 330 core
            layout (location = 0) in vec3 aPos;
            layout (location = 1) in vec2 aTexCoord;
            out vec2 TexCoord;
            void main()
            {
                gl_Position = vec4(aPos, 1.0);
                TexCoord = aTexCoord;
            }";

            string fragmentSource = @"#version 330 core
            out vec4 FragColor;
            in vec2 TexCoord;
            uniform sampler2D ourTexture;
            void main()
            {
                FragColor = texture(ourTexture, TexCoord);
            }";

            uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
            _gl.ShaderSource(vertexShader, vertexSource);
            _gl.CompileShader(vertexShader);
            CheckShader(vertexShader);

            uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
            _gl.ShaderSource(fragmentShader, fragmentSource);
            _gl.CompileShader(fragmentShader);
            CheckShader(fragmentShader);

            _program = _gl.CreateProgram();
            _gl.AttachShader(_program, vertexShader);
            _gl.AttachShader(_program, fragmentShader);
            _gl.LinkProgram(_program);
            
            // Clean up individual shaders (linked into program now)
            _gl.DeleteShader(vertexShader);
            _gl.DeleteShader(fragmentShader);
        }

        private static void CheckShader(uint shader)
        {
            string infoLog = _gl.GetShaderInfoLog(shader);
            if (!string.IsNullOrWhiteSpace(infoLog))
            {
                Console.WriteLine($"Shader Error: {infoLog}");
            }
        }

        private static void KeyDown(IKeyboard keyboard, Key key, int keyCode)
        {
            if (key == Key.Escape)
                _window.Close();
        }

        
    }
}