using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Markup;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Collections;
using Pantas.DeltaDraw.Core;

namespace Pantas.DeltaDraw.Test1
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		//List<DDNodeHandle> handles = new List<DDNodeHandle>();

		DDHandleBase draggedElement = null;

		DDPolygon myTest;

		//DDManipulationFrame manipulationFrame = null;

		public MainWindow()
		{	
			InitializeComponent();

			myTest = new DDPolygon();

			myTest.Nodes.Add(new DDNodeWithHandles(new Point(130, 132)));
			myTest.Nodes.Add(new DDNodeWithHandles(new Point(167, 111)));
			myTest.Nodes.Add(new DDNodeWithHandles(new Point(217, 156)));
			myTest.Nodes.Add(new DDNodeWithHandles(new Point(154, 235)));

			myTest.Stroke = Brushes.Black;
			myTest.Fill = Brushes.Yellow;
			xamlCanvas.Children.Add(myTest);

			xamlCanvas.MouseMove += new MouseEventHandler(xamlCanvas_MouseMove);

			//manipulationFrame = new DDManipulationFrame(myTest);
			//xamlCanvas.Children.Add(manipulationFrame);

		}

		void xamlCanvas_MouseMove(object sender, MouseEventArgs e)
		{
			if ((e.LeftButton == MouseButtonState.Pressed)
				&& (e.MiddleButton == MouseButtonState.Released)
				&& (e.RightButton == MouseButtonState.Released)
				&& ((e.Source is DDHandleBase) || (draggedElement is DDHandleBase)))
			{
				if ((draggedElement == null) && (e.Source is DDHandleBase))
					draggedElement = e.Source as DDHandleBase;
				draggedElement.Position = e.GetPosition(xamlCanvas);
			}
			else if ((e.LeftButton == MouseButtonState.Released)
				&& (draggedElement != null))
				draggedElement = null;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			myTest.Width = 400;
		}
	}
}
