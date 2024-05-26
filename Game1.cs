using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace gate
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        
        //debug variables
        public FpsCounter fps;

        //Game world (overseer/manager)
        World world;
        //Render target that the world is drawn to
        RenderTarget2D render_target;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            //set window title
            Window.Title = Constant.window_title;
            //pull current display resolution
            //set window size
            _graphics.PreferredBackBufferWidth = (int)Constant.window_width;
            _graphics.PreferredBackBufferHeight = (int)Constant.window_height;
            _graphics.ApplyChanges();

            //allow window_resizing
            Window.AllowUserResizing = true;
            //create listener for changing window size
            Window.ClientSizeChanged += new EventHandler<EventArgs>(Window_ClientSizeChanged);

            //initialize render target
            Console.WriteLine(GraphicsDevice.PresentationParameters.BackBufferWidth);
            Console.WriteLine(GraphicsDevice.PresentationParameters.BackBufferHeight);
            render_target = new RenderTarget2D(
                GraphicsDevice,
                GraphicsDevice.PresentationParameters.BackBufferWidth,
                GraphicsDevice.PresentationParameters.BackBufferHeight,
                false,
                GraphicsDevice.PresentationParameters.BackBufferFormat,
                DepthFormat.Depth24
            );

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            Constant.tree4_tex = Content.Load<Texture2D>("sprites/tree4");

            //initialize world after all content is loaded
            world = new World(this, _graphics, Content.RootDirectory, Content);
            world.resize_viewport(_graphics);

            //debug fps initialization
            fps = new FpsCounter(this, Constant.arial_small, Vector2.Zero);
            fps.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            //always be able to exit the game (for now)
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            //handle updates around the game window and render target

            world.Update(gameTime);

            fps.Update(gameTime);

            base.Update(gameTime);
        }

        void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            //update the world view to match with the window
            Constant.window_width = _graphics.GraphicsDevice.Viewport.Width;
            Constant.window_height = _graphics.GraphicsDevice.Viewport.Height;
            _graphics.PreferredBackBufferWidth = (int)Constant.window_width;
            _graphics.PreferredBackBufferHeight = (int)Constant.window_height;
            _graphics.ApplyChanges();
            //set render target to a new Render Target with the new params
            render_target = new RenderTarget2D(
                GraphicsDevice,
                GraphicsDevice.PresentationParameters.BackBufferWidth,
                GraphicsDevice.PresentationParameters.BackBufferHeight,
                false,
                GraphicsDevice.PresentationParameters.BackBufferFormat,
                DepthFormat.Depth24
            );
            //update the world camera viewport to match the window
            world.resize_viewport(_graphics);
            Constant.update_ui_positions_on_screen_size_change();
        }

        //function to draw world to render target
        protected void draw_scene_to_texture(SpriteBatch spriteBatch, RenderTarget2D render_target, World world) {
            // Set the render target
            GraphicsDevice.SetRenderTarget(render_target);

            GraphicsDevice.DepthStencilState = new DepthStencilState() { DepthBufferEnable = true };

            // Draw the scene
            GraphicsDevice.Clear(Color.CornflowerBlue);
            //draw world
            world.Draw(spriteBatch);

            // Drop the render target
            GraphicsDevice.SetRenderTarget(null);
        }

        protected override void Draw(GameTime gameTime)
        {
            draw_scene_to_texture(_spriteBatch, render_target, world);

            GraphicsDevice.Clear(Color.Black);

            //draw world
            //world.Draw(_spriteBatch);
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);
            //draw the texture to the screen
            _spriteBatch.Draw(render_target, new Rectangle(0, 0, (int)Constant.window_width, (int)Constant.window_height), Color.White);
            _spriteBatch.End();

            _spriteBatch.Begin();
            fps.Draw(gameTime);
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
