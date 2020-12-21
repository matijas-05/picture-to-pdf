using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Pdf;
using iText.IO.Image;

using WPFCustomMessageBox;

using System.IO;
using System.Windows;
using System.Diagnostics;
using System.ComponentModel;

namespace PictureToPdf
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		string[] m_Pictures;
		string m_OutputFile;

		public MainWindow()
		{
			InitializeComponent();
		}

		void FilePicker_PicturePicked(string[] files)
		{
			m_Pictures = files;
			picturesInfo.Content = $"Wybrano {files.Length} obrazów {Path.GetExtension(files[0])}";
			convertBtn.IsEnabled = CanConvert();
		}
		void FilePicker_OutputFilePicked(string[] file)
		{
			m_OutputFile = file[0];
			convertBtn.IsEnabled = CanConvert();
		}

		void ConvertBtn_Click(object sender, RoutedEventArgs e)
		{
			var worker = new BackgroundWorker();
			worker.DoWork += Worker_DoWork;
			worker.ProgressChanged += Worker_ProgressChanged;
			worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
			worker.WorkerReportsProgress = true;
			worker.RunWorkerAsync();
		}

		void Worker_DoWork(object sender, DoWorkEventArgs e)
		{
			var worker = (BackgroundWorker)sender;
			worker.ReportProgress(0);

			// Convert to pdf
			using (var pdfWriter = new PdfWriter(m_OutputFile))
			{
				using (var pdfDoc = new PdfDocument(pdfWriter))
				{
					using (var doc = new Document(pdfDoc))
					{
						for (int i = 0; i < m_Pictures.Length; i++)
						{
							ImageData imgData = ImageDataFactory.Create(m_Pictures[i]);
							Image img = new Image(imgData);
							doc.Add(img);

							worker.ReportProgress(i+1);
						}
					}
				}
			}
		}
		void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{

		}
		void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			// Show dialog after converting
			var result = CustomMessageBox.ShowYesNoCancel("Zakończono konwertowanie", "Informacja", "Otwórz plik .pdf", "Otwórz folder zawierający", "Zamknij", MessageBoxImage.Information);

			if (result == MessageBoxResult.Yes)
			{
				Process.Start(m_OutputFile);
			}
			else if (result == MessageBoxResult.No)
			{
				Process explorer = new Process();

				explorer.StartInfo.FileName = "explorer.exe";
				explorer.StartInfo.Arguments = $"/select,\"{m_OutputFile}\"";

				explorer.Start();
			}
		}

		bool CanConvert()
		{
			if (m_Pictures == null || m_Pictures.Length == 0 || string.IsNullOrEmpty(m_OutputFile))
				return false;

			foreach (var file in m_Pictures)
			{
				if (!File.Exists(file))
					return false;
			}

			if (!Directory.Exists(Directory.GetParent(m_OutputFile).FullName))
				return false;

			return true;
		}
	}
}