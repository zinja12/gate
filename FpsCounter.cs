using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace gate
{
    /// <summary>
    /// A game component that counts FPS and UPS, also gives other useful performance information.
    /// </summary>
    public class FpsCounter : Microsoft.Xna.Framework.DrawableGameComponent
    {
        #region Private Fields
        private const int refreshesPerSec = 4;  // how many times do we calculate FPS & UPS every second
        private readonly TimeSpan RefreshTime = TimeSpan.FromMilliseconds(1000 / refreshesPerSec);
        private TimeSpan elapsedTime = TimeSpan.Zero;
        private static int fps = 0, ups = 0;
        private int frameCounter = 0, updateCounter = 0;
        private SpriteBatch spriteBatch;
        private SpriteFont font;
        private Vector2 position;
        private static Process process = Process.GetCurrentProcess();
        private static List<string> messages = new List<string>();
        private static List<double> messageTimers = new List<double>();
        private StringBuilder outputSb = new StringBuilder();
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the current FPS.
        /// </summary>
        public static int FPS
        {
            get { return fps; }
        }

        /// <summary>
        /// Gets the current UPS.
        /// </summary>
        public static int UPS
        {
            get { return ups; }
        }

        /// <summary>
        /// Gets the total allocated memory, in bytes.
        /// </summary>
        public static long MemAllocated
        {
            get { return process.PrivateMemorySize64; }
        }
        #endregion

        public FpsCounter(Game game, SpriteFont font, Vector2 pos)
            : base(game)
        {
            this.spriteBatch = new SpriteBatch(game.GraphicsDevice);
            this.font = font;
            this.position = pos;
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
        }

        #region Public Methods

        /// <summary>
        /// Displays a message on screen for debugging purposes.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="milliseconds"></param>
        public static void ShowMessage(string msg, int milliseconds)
        {
            messages.Add(msg);
            messageTimers.Add(milliseconds);
        }

        #endregion

        #region Update and Draw

        /// <summary>
        /// Allows performace monitor to calculate update rate.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            elapsedTime += gameTime.ElapsedGameTime;

            updateCounter++;

            if (elapsedTime > RefreshTime)
            {
                elapsedTime -= RefreshTime;
                fps = frameCounter * refreshesPerSec;
                ups = updateCounter * refreshesPerSec;
                frameCounter = 0;
                updateCounter = 0;
            }

            // Update message timers
            for (int i = 0; i < messageTimers.Count; i++)
            {
                messageTimers[i] -= gameTime.ElapsedGameTime.TotalMilliseconds;
                if (messageTimers[i] <= 0)
                {
                    messages.RemoveAt(i);   // remove timed out messages
                    messageTimers.RemoveAt(i);
                }
            }

            outputSb.Clear();
            outputSb.Append(fps.ToString() + " ");
            outputSb.Append("(" + (MemAllocated / 1000000).ToString() + " MB)" + Environment.NewLine);
            foreach (string msg in messages)
            {
                outputSb.Append(msg + Environment.NewLine);
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// Allows performance monitor to calculate draw rate.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Draw(GameTime gameTime)
        {
            frameCounter++; // increment frame counter

            spriteBatch.Begin();
            spriteBatch.DrawString(font, outputSb.ToString(), position + new Vector2(1, 1), Color.Black); // shadow
            spriteBatch.DrawString(font, outputSb.ToString(), position, Color.Fuchsia);
            spriteBatch.End();
        }

        #endregion
    }
}