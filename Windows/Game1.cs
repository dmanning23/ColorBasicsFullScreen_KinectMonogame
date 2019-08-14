using Microsoft.Kinect;
using ResolutionBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;

namespace ColorBasicsFullScreen_KinectMonogame
{
	/// <summary>
	/// This is the main type for your game
	/// </summary>
	public class Game1 : Game
	{
		GraphicsDeviceManager graphics;
		SpriteBatch spriteBatch;

		/// <summary>
		/// Active Kinect sensor
		/// </summary>
		private KinectSensor sensor;

		/// <summary>
		/// Intermediate storage for the depth data converted to color
		/// </summary>
		private byte[] colorPixels;

		/// <summary>
		/// the texture to write to
		/// </summary>
		Texture2D pixels;

		/// <summary>
		/// temp buffer to hold convert kinect data to color objects
		/// </summary>
		Color[] pixelData_clear;

		/// <summary>
		/// The horizontal size of the texture we want to display
		/// </summary>
		private const int ScreenX = 1024;

		/// <summary>
		/// the vertical size of the texture we want to display
		/// </summary>
		private const int ScreenY = 768;

		public Game1()
		{
			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
		}

		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize()
		{
#if __IOS__
			var resolution = new ResolutionComponent(this, graphics, new Point(1280, 720), new Point(1280, 720), true, false);
#else
			var resolution = new ResolutionComponent(this, graphics, new Point(1280, 720), new Point(1280, 720), false, false);
#endif

			base.Initialize();
		}

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent()
		{
			// Create a new SpriteBatch, which can be used to draw textures.
			spriteBatch = new SpriteBatch(GraphicsDevice);

			//Create the texture that will be displayed on screen
			pixels = new Texture2D(graphics.GraphicsDevice,
							ScreenX,
							ScreenY, false, SurfaceFormat.Color);

			//Create the temp buffer that will be used to store Kinect data
			pixelData_clear = new Color[ScreenX * ScreenY];

			//Iniitalize the temp data to black (this is probably unecessary, but if no kinect sensor it's better to be sure)
			for (int i = 0; i < pixelData_clear.Length; ++i)
			{
				pixelData_clear[i] = Color.Black;
			}

			// Look through all sensors and start the first connected one.
			// This requires that a Kinect is connected at the time of app startup.
			// To make your app robust against plug/unplug, 
			// it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
			foreach (var potentialSensor in KinectSensor.KinectSensors)
			{
				if (potentialSensor.Status == KinectStatus.Connected)
				{
					this.sensor = potentialSensor;
					break;
				}
			}

			if (null != this.sensor)
			{
				// Turn on the color stream to receive color frames
				this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

				// Allocate space to put the color pixels we'll create
				this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];

				// Add an event handler to be called whenever there is new color frame data
				this.sensor.ColorFrameReady += this.SensorColorFrameReady;

				// Start the sensor!
				try
				{
					this.sensor.Start();
				}
				catch (IOException)
				{
					this.sensor = null;
				}
			}

			//if (null == this.sensor)
			//{
			//	this.statusBarText.Text = Properties.Resources.NoKinectReady;
			//}
		}

		/// <summary>
		/// UnloadContent will be called once per game and is the place to unload
		/// all content.
		/// </summary>
		protected override void UnloadContent()
		{
			if (null != this.sensor)
			{
				this.sensor.Stop();
			}
		}

		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update(GameTime gameTime)
		{
			if ((GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed) ||
			Keyboard.GetState().IsKeyDown(Keys.Escape))
			{
				this.Exit();
			}

			base.Update(gameTime);
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.CornflowerBlue);

			//copy the temp buffer to the 2d texture
			pixels.SetData<Color>(pixelData_clear);

			//calculate proper viewport according to aspect ratio
			Resolution.ResetViewport();

			//setup the spritebatch object for rendering
			spriteBatch.Begin(SpriteSortMode.Immediate,
				BlendState.AlphaBlend,
				null, null, null, null,
				Resolution.TransformationMatrix());

			//Render the texture to the screen
			spriteBatch.Draw(pixels, new Vector2(0, 0), null, Color.White);

			spriteBatch.End();

			base.Draw(gameTime);
		}

		/// <summary>
		/// Event handler for Kinect sensor's ColorFrameReady event
		/// </summary>
		/// <param name="sender">object sending the event</param>
		/// <param name="e">event arguments</param>
		private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
		{
			using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
			{
				if (colorFrame != null)
				{
					// Copy the pixel data from the image to a temporary array
					colorFrame.CopyPixelDataTo(this.colorPixels);

					//get the width of the image
					int imageWidth = colorFrame.Width;

					//get the height of the image
					int imageHeight = colorFrame.Height;

					// Convert the depth to RGB
					for (int pixelIndex = 0; pixelIndex < pixelData_clear.Length; pixelIndex++)
					{
						//get the pixel column
						int x = pixelIndex % ScreenX;

						//get the pixel row
						int y = pixelIndex / ScreenX;

						//convert the image x to cell x
						int x2 = (x * imageWidth) / ScreenX;

						//convert the image y to cell y
						int y2 = (y * imageHeight) / ScreenY;

						//get the index of the cell
						int cellIndex = ((y2 * imageWidth) + x2) * 4;

						//Create a new color
						pixelData_clear[pixelIndex] = new Color(colorPixels[cellIndex + 2], colorPixels[cellIndex + 1], colorPixels[cellIndex + 0]);
					}
				}
			}
		}
	}
}
