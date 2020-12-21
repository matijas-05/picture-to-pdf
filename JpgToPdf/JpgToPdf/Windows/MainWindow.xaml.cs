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
using System.Threading;

namespace PictureToPdf
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		string[] m_Pictures;
		string m_OutputFile;
		ProgressWindow m_ProgressWindow;
		BackgroundWorker m_Worker;
		bool m_Cancelled;

		const double FILE_SIZE_PER_PAGE = 3.450549450549451d;

		public MainWindow()
		{
			InitializeComponent();
		}

		void FilePicker_PicturePicked(string[] files)
		{
			m_Pictures = files;
			picturesInfo.Content = $"Wybrano {files.Length} obrazów {Path.GetExtension(files[0])}";
			sizeInfo.Content = $"Końcowy rozmiar pliku ok. {Math.Round(FILE_SIZE_PER_PAGE * m_Pictures.Length, 1)} MB";
			convertBtn.IsEnabled = CanConvert();
		}
		void FilePicker_OutputFilePicked(string[] file)
		{
			m_OutputFile = file[0];
			convertBtn.IsEnabled = CanConvert();
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
			using (var pdfWriter = new PdfWriter(m_OutputFile))
			{
				var pdfDoc = new PdfDocument(pdfWriter);
				using (var doc = new Document(pdfDoc))
				{
					for (int i = 0; i < m_Pictures.Length; i++)
					{
						if (m_Worker.CancellationPending)
						{
							m_Worker.ReportProgress(-1, ("Anulowanie...", "Może to zająć nawet kilka minut"));
							Dispatcher.BeginInvoke(new Action(() => m_ProgressWindow.cancelBtn.IsEnabled = false), DispatcherPriority.Background);

							doc.Close();
							pdfWriter.Dispose();

							m_Worker.ReportProgress(-2, ("Anulowano", ""));
							m_Cancelled = true;
							return;
						}

						ImageData imgData = ImageDataFactory.Create(m_Pictures[i]);
						Image img = new Image(imgData);
						doc.Add(img);

						m_Worker.ReportProgress(Convert.ToInt32((float)(i + 1) / m_Pictures.Length * 100f), ($"Konwertowanie {i + 1} z {m_Pictures.Length}:", Path.GetFileName(m_Pictures[i])));
					}
					Dispatcher.BeginInvoke(new Action(() => m_ProgressWindow.cancelBtn.IsEnabled = false), DispatcherPriority.Background);
					m_Worker.ReportProgress(100, ("Kończenie...", "Może to zająć nawet kilka minut"));
				}
			}
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
			CloseProgressWindow();

			if (m_Cancelled)
				return;

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
		public void CancelConvert()
		{
			m_Worker.CancelAsync();
		}

		void CloseProgressWindow()
		{
			Dispatcher.BeginInvoke(new Action(() =>
			{
				m_ProgressWindow.Close();
			}), DispatcherPriority.Background);
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