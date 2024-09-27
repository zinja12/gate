using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using gate.Core;

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
        //Canvas that controls the render target that the world is drawn to
        Canvas _canvas;

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

            _canvas = new Canvas(_graphics.GraphicsDevice, (int)Constant.window_width, (int)Constant.window_height, 1.0f);
            _canvas.set_destination_rectangle();

            //allow window_resizing
            Window.AllowUserResizing = true;
            //create listener for changing window size
            Window.ClientSizeChanged += new EventHandler<EventArgs>(Window_ClientSizeChanged);

            //initialize render target
            Console.WriteLine($"window_width:{GraphicsDevice.PresentationParameters.BackBufferWidth}");
            Console.WriteLine($"window_height:{GraphicsDevice.PresentationParameters.BackBufferHeight}");

            base.Initialize();
        }
        
        //TODO: Come up with a better name for this function
        public void set_pixel_shader_active(bool enabled) {
            if (enabled) {
                _canvas.add_postprocessing_effect(Constant.pixelate_effect);
            } else {
                //_canvas.set_postprocessing_effect(null);
                _canvas.clear_postprocessing_effects();
            }
        }

        public SpriteBatch get_spriteBatch() {
            return this._spriteBatch;
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            //initialize world after all content is loaded
            world = new World(this, _graphics, Content.RootDirectory, Content);
            world.resize_viewport(_graphics);

            _canvas.add_postprocessing_effect(Constant.pixelate_effect);

            //debug fps initialization
            fps = new FpsCounter(this, Constant.arial_small, Vector2.Zero);
            fps.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            //always be able to exit the game (for now)
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            //world update
            world.Update(gameTime);
            //fps update
            fps.Update(gameTime);

            base.Update(gameTime);
        }

        void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            //update the world view to match with the window
            Constant.window_width = _graphics.GraphicsDevice.Viewport.Width;
            Constant.window_height = _graphics.GraphicsDevice.Viewport.Height;
            Constant.update_ui_positions_on_screen_size_change();
            _canvas.set_destination_rectangle();
        }

        protected override void Draw(GameTime gameTime)
        {
            //activate canvas for render target to capture draw calls
            _canvas.Activate();

            GraphicsDevice.Clear(Color.CornflowerBlue);

            //draw world
            world.Draw(_spriteBatch);
            
            //actually draw render target in canvas to the screen
            _canvas.Draw(_spriteBatch);

            //draw fps counter
            _spriteBatch.Begin();
            fps.Draw(gameTime);
            _spriteBatch.End();
            //draw world debug and text overlays outside canvas
            world.DrawTextOverlays(_spriteBatch);

            base.Draw(gameTime);
        }
    }
}
