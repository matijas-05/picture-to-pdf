﻿using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace PictureToPdf
{
	/// <summary>
	/// Interaction logic for ProgressWindow.xaml
	/// </summary>
	public partial class ProgressWindow : Window
	{
		private const int GWL_STYLE = -16;
		private const int WS_SYSMENU = 0x80000;
		[DllImport("user32.dll", SetLastError = true)]
		private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
		[DllImport("user32.dll")]
		private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		public ProgressWindow()
		{
			InitializeComponent();
			Loaded += ProgressWindow_Loaded;
		}
		void ProgressWindow_Loaded(object sender, RoutedEventArgs e)
		{
			var hwnd = new WindowInteropHelper(this).Handle;
			SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
		}
		void Cancel_Click(object sender, RoutedEventArgs e)
		{
			var owner = (MainWindow)Owner;
			owner.CancelConvert();
		}
	}
}
