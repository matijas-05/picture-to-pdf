using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Pdf;
using iText.IO.Image;

using WPFCustomMessageBox;

using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;

namespace PictureToPdf
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		string[] m_Pictures;
		string m_OutputFolder;
		ProgressWindow m_ProgressWindow;
		BackgroundWorker m_Worker;
		bool m_Cancelled;

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
		void FilePicker_OutputFolderPicked(string[] file)
		{
			m_OutputFolder = file[0];
			convertBtn.IsEnabled = CanConvert();
		}
		bool CanConvert()
		{
			if (m_Pictures == null || m_Pictures.Length == 0 || string.IsNullOrEmpty(m_OutputFolder))
				return false;

			foreach (var file in m_Pictures)
			{
				if (!File.Exists(file))
					return false;
			}

			if (!Directory.Exists(m_OutputFolder))
				return false;

			return true;
		}

		void ConvertBtn_Click(object sender, RoutedEventArgs e)
		{
			m_Worker = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
			m_Worker.DoWork += Worker_DoWork;
			m_Worker.ProgressChanged += Worker_ProgressChanged;
			m_Worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
			m_Worker.RunWorkerAsync();
		}
		void Worker_DoWork(object sender, DoWorkEventArgs e)
		{
			Dispatcher.BeginInvoke(new Action(() =>
			{
				m_ProgressWindow = new ProgressWindow();
				m_ProgressWindow.Owner = this;
				m_ProgressWindow.ShowDialog();
			}), DispatcherPriority.Background);

			m_Cancelled = false;
			m_Worker.ReportProgress(0, ("Rozpoczynanie...", ""));

			// Convert to pdf
			for (int i = 0; i < m_Pictures.Length; i++)
			{
				using (var pdfWriter = new PdfWriter($"{m_OutputFolder}\\{Path.GetFileNameWithoutExtension(m_Pictures[i])}.pdf"))
				{
					var pdfDoc = new PdfDocument(pdfWriter);
					using (var doc = new Document(pdfDoc))
					{
						// Cancelling state
						if (m_Worker.CancellationPending)
						{
							m_Worker.ReportProgress(-1, ("Anulowanie...", ""));
							Dispatcher.BeginInvoke(new Action(() => m_ProgressWindow.cancelBtn.IsEnabled = false), DispatcherPriority.Background);

							if (doc.GetPdfDocument().GetNumberOfPages() > 0) doc.Close();
							pdfWriter.Dispose();

							m_Worker.ReportProgress(-2, ("Anulowano", ""));
							m_Cancelled = true;
							return;
						}

						ImageData imgData = ImageDataFactory.Create(m_Pictures[i]);
						Image img = new Image(imgData);

						// Rotate image if height is bigger than width (portrait)
						if(img.GetImageWidth() > img.GetImageHeight()) img.SetRotationAngle(-1.57079633d);
						img.SetAutoScale(true);
						doc.Add(img);

						m_Worker.ReportProgress(Convert.ToInt32((float)(i + 1) / m_Pictures.Length * 100f), ($"Konwertowanie {i + 1} z {m_Pictures.Length}:", Path.GetFileName(m_Pictures[i])));
					}
				}
			}

			// Ending state
			Dispatcher.BeginInvoke(new Action(() => m_ProgressWindow.cancelBtn.IsEnabled = false), DispatcherPriority.Background);
			m_Worker.ReportProgress(100, ("Usuwanie z pamięci tymczasowej...", ""));
		}
		void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			Dispatcher.BeginInvoke(new Action(() =>
			{
				var progress = ((string, string))e.UserState;

				if (e.ProgressPercentage == 100 || e.ProgressPercentage == -1) m_ProgressWindow.progressBar.IsIndeterminate = true;

				m_ProgressWindow.progressBar.Value = e.ProgressPercentage;
				m_ProgressWindow.progressInfo1.Content = progress.Item1;
				m_ProgressWindow.progressInfo2.Content = progress.Item2;
			}), DispatcherPriority.Background);
		}
		void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			// Close progress window
			Dispatcher.BeginInvoke(new Action(() =>
			{
				m_ProgressWindow.Close();
			}), DispatcherPriority.Background);

			if (m_Cancelled)
				return;

			// Show dialog after converting
			var result = CustomMessageBox.ShowOKCancel("Zakończono konwertowanie", "Informacja", "Otwórz folder", "Zamknij", MessageBoxImage.Information);

			if (result == MessageBoxResult.OK)
			{
				Process.Start(m_OutputFolder);
			}
		}
		public void CancelConvert()
		{
			m_Worker.CancelAsync();
		}

	}
}