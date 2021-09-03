using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Microsoft.Win32;

namespace AccessViolationIssue
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow() => InitializeComponent();



		/// <summary>
		///		Creates a bitmap from the <see cref="MainWindow"/>
		///		content and saves it to disk.
		/// </summary>
		private void SaveImage()
		{
			SaveFileDialog fileDialog = new SaveFileDialog()
			{
				Filter = "PNG files|*.png",
				Title = "Save dependency diagram"
			};

			if (fileDialog.ShowDialog() ?? false)
				if (fileDialog.FileName is string filePath)
				{
					int resolution = 96;
					double scaling = resolution / 96f;
					int width = (int)(ContentPresenter.ActualWidth * scaling), height = (int)(ContentPresenter.ActualHeight * scaling);

					RenderTargetBitmap bitmap = new RenderTargetBitmap(width, height, resolution, resolution, PixelFormats.Pbgra32);

					bitmap.Render(ContentPresenter);

					using FileStream imageFile = new FileStream(filePath, FileMode.Create, FileAccess.Write);
					BitmapEncoder png = new PngBitmapEncoder();
					png.Frames.Add(BitmapFrame.Create(bitmap));

					try
					{
						png.Save(imageFile);
					}
					catch (ArgumentException ex)
					{
						MessageBoxResult choice = MessageBox.Show($@"{ex.Message}

Image dimensions: {width}*{height} pixels.

Do you want to save the diagram by splitting the image into several files?", "Diagram image could not be saved", MessageBoxButton.YesNo, MessageBoxImage.Question);

						imageFile.Close();
						File.Delete(filePath);

						if (choice == MessageBoxResult.Yes)
							SaveTiledImage(bitmap, filePath);
					}
				}
		}

		/// <summary>
		///		Splits the bitmap to be saved into tiles and
		///		saves each of the tiles separately, adding a
		///		sequence number to the tiles' file names.
		/// </summary>
		/// <param name="bitmap">
		///		Bitmap to be saved to disk.
		/// </param>
		/// <param name="filePath">
		///		Destination file path for the image to be saved.
		/// </param>
		private static void SaveTiledImage(RenderTargetBitmap bitmap, string filePath)
		{
			SystemException? exception = null;
			string path = Path.GetDirectoryName(filePath)!;
			string newDir = $"{Path.GetFileNameWithoutExtension(filePath)} ({DateTime.Now:yyyy-MM-dd_hh;mm;ss})";
			string filePattern = Path.GetFileNameWithoutExtension(filePath)! + "-{0:D}" + Path.GetExtension(filePath);

			try
			{
				if (Directory.Exists(path))
				{
					path = Path.Combine(path, newDir);
					Directory.CreateDirectory(path);

					filePath = Path.Combine(path, filePattern);

					int startSteps = (int)bitmap.Width / 40000 * 2;

					for (int steps = startSteps; steps < startSteps + 4; steps += 2)
					{
						exception = null;

						for (int idx = 0; idx < steps; ++idx)
							try
							{
								SavePartialImage(bitmap, filePath, idx, steps);
							}
							catch (SystemException ex)
							{
								exception = ex;
								break;
							}

						if (exception == null)
						{
							MessageBox.Show("Diagram image successfully saved using tiles.", "Diagram image saved");
							return;
						}
					}
				}
			}
			catch (SystemException ex)
			{
				exception = ex;
			}

			MessageBox.Show(exception!.Message, "Diagram image could not be saved", MessageBoxButton.OK, MessageBoxImage.Error);
		}

		/// <summary>
		///		Saves a section of the provided bitmap to disk.
		/// </summary>
		/// <param name="bitmap">
		///		Bitmap to be saved to disk.
		/// </param>
		/// <param name="filePath">
		///		<para>Destination file path for the image to be saved.</para>
		///		<para>The file path must contain a placeholder item
		///		("<c>{0}</c>").</para>
		/// </param>
		/// <param name="index">
		///		Section index of the bitmap part to save to disk.
		/// </param>
		/// <param name="steps">
		///		Number of sections the bitmap is split into.
		/// </param>
		[HandleProcessCorruptedStateExceptions]
		private static void SavePartialImage(RenderTargetBitmap bitmap, string filePath, int index, int steps)
		{
			int width = (int)bitmap.Width / steps;
			int left = width * index;

			CroppedBitmap crop = new CroppedBitmap(bitmap, new Int32Rect(left, 0, width + (index == steps - 1 ? 0 : 1), (int)bitmap.Height));

			using FileStream imageFile = new FileStream(string.Format(filePath, index + 1), FileMode.Create, FileAccess.Write);
			BitmapEncoder png = new PngBitmapEncoder();
			png.Frames.Add(BitmapFrame.Create(crop));

			try
			{
				png.Save(imageFile);
			}
			catch (ArgumentException)
			{
				imageFile.Close();
				File.Delete(filePath);

				throw;
			}
		}



		private void Save_Click(object sender, RoutedEventArgs e) => SaveImage();
	}
}
