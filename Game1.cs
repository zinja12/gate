using System;
using System.Collections.Generic;
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
        ScreenCanvas screen_canvas;

        public Color clear_color { get; set; }

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
            Console.WriteLine($"window_width:{GraphicsDevice.PresentationParameters.BackBufferWidth}");
            Console.WriteLine($"window_height:{GraphicsDevice.PresentationParameters.BackBufferHeight}");

            base.Initialize();
        }
        
        //TODO: Come up with a better name for this function
        public void set_pixel_shader_active(bool enabled) {
            if (enabled) {
                //screen_canvas.add_postprocessing_effect(Constant.pixelate_effect);
                screen_canvas.add_postprocessing_effect(Constant.scanline2_effect);
            } else {
                screen_canvas.clear_postprocessing_effects();
            }
        }

        public SpriteBatch get_spriteBatch() {
            return this._spriteBatch;
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            screen_canvas = new ScreenCanvas(_graphics.GraphicsDevice, Constant.internal_resolution_width, Constant.internal_resolution_height, clear_color);

            //initialize world after all content is loaded
            world = new World(this, _graphics, Content.RootDirectory, Content);
            world.resize_viewport(_graphics);
            
            screen_canvas.set_world(world);
            
            world.update_textbox_scale();

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
            screen_canvas.reset(Constant.internal_resolution_width, Constant.internal_resolution_height, clear_color);
            world.update_textbox_scale();
            //reset window height value for scanline shader
            Constant.scanline2_effect.Parameters["screen_height"].SetValue(Constant.window_height);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(clear_color);

            //draw render target that game is rendered to screen canvas
            screen_canvas.Draw(_spriteBatch);

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
