using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Pdf;

using System.IO;
using System.Windows;

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
