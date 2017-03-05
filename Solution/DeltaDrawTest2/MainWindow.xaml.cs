using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;
using Pantas.DeltaDraw.Core;

namespace Pantas.DeltaDraw.Test2
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		DDCanvas xamlCanvas = new DDCanvas();
		
		DDHandle draggedElement = null;
		protected DDHandle DraggedElement
		{
			get { return draggedElement; }
			set
			{
				draggedElement = value;
			}
		}

		public MainWindow()
		{	
			InitializeComponent();

			xamlScrollViewer.Content = xamlCanvas;

			xamlCanvas.MouseMove += new MouseEventHandler(xamlCanvas_MouseMove);
		}

		void xamlCanvas_MouseMove(object sender, MouseEventArgs e)
		{
			Point mosePos = e.GetPosition(xamlCanvas);

			/*foreach (UIElement el in xamlCanvas.Children)
				if (el is DDObject)
					if ((el as DDObject).StrokeContains(mosePos, 10))
					{
						mosePos = (el as DDObject).ClosestPoint(mosePos);
						break;
					}
			*/
			/*if ((e.LeftButton == MouseButtonState.Pressed)
				&& (e.MiddleButton == MouseButtonState.Released)
				&& (e.RightButton == MouseButtonState.Released)
				&& ((e.Source is DDHandle) || (DraggedElement != null)))
			{
				if ((DraggedElement == null) && (e.Source is DDHandle))
					DraggedElement = e.Source as DDHandle;
				DraggedElement.Position = mosePos;
			}
			else if ((e.LeftButton == MouseButtonState.Released)
				&& (DraggedElement != null))
				DraggedElement = null;*/
		}

		private void xamlButton1_Click(object sender, RoutedEventArgs e)
		{
			xamlCanvas.Children.Clear();
			
			DDPolygon p1;
			DDPolyline p2;
			
			/*DDRectangle r;

			r = new DDRectangle();

			r.Bounds = new Rect(200, 140, 300, 200);
			r.Stroke = Brushes.ForestGreen;
			r.StrokeThickness = 3;
			r.Fill = Brushes.Red;
			xamlCanvas.Children.Add(r);*/

			p1 = new DDPolygon();

			p1.Nodes.Add(new DDNodeWithHandles(xamlCanvas, new Point(100, 100)));
			p1.Nodes.Add(new DDNodeWithHandles(xamlCanvas, new Point(500, 100)));
			p1.Nodes.Add(new DDNodeWithHandles(xamlCanvas, new Point(300, 200)));

			p1.Stroke = Brushes.Black;
			p1.Fill = Brushes.Yellow;
			xamlCanvas.Children.Add(p1);

			p2 = new DDPolyline();

			p2.Nodes.Add(new DDNodeWithHandles(xamlCanvas, new Point(100, 250)));
			p2.Nodes.Add(new DDNodeWithHandles(xamlCanvas, new Point(500, 250)));
			p2.Nodes.Add(new DDNodeWithHandles(xamlCanvas, new Point(300, 350)));

			p2.Stroke = Brushes.Black;
			p2.Fill = Brushes.LimeGreen;
			xamlCanvas.Children.Add(p2);

			DDRectangle r1 = new DDRectangle();
			r1.Bounds = new Rect(100, 400, 400, 100);
			r1.Fill = Brushes.Red;
			r1.Stroke = Brushes.Black;
			xamlCanvas.Children.Add(r1);

			DDEllipse e1 = new DDEllipse();
			e1.Bounds = new Rect(100, 550, 400, 100);
			e1.Fill = Brushes.Blue;
			e1.Stroke = Brushes.Red;
			xamlCanvas.Children.Add(e1);

			r1.CornerRadiusX = 50;
			r1.CornerRadiusY = 100;
		}

		private void xamlButton2_Click(object sender, RoutedEventArgs e)
		{
			xamlCanvas.Children.Clear();
			
			Random rand = new Random();
			int pCount = rand.Next(30) + 10;
			DDPolygon[] polygons = new DDPolygon[pCount];
			for (int i = 0; i < pCount; i++)
			{
				polygons[i] = new DDPolygon();
				
				int nCount = rand.Next(10) + 5;
				Point pos = new Point(rand.Next(800), rand.Next(800));
				Size size = new Size(rand.Next(600) + 50, rand.Next(600) + 50);
				for (int j = 0; j < nCount; j++)
				{
					DDNodeWithHandles node = new DDNodeWithHandles(xamlCanvas)
					{
						Point = new Point(
							pos.X + rand.Next((int) size.Width),
							pos.Y + rand.Next((int) size.Height))
					};
					polygons[i].Nodes.Add(node);
				}

				polygons[i].Stroke = new SolidColorBrush(Color.FromArgb((byte)(200 + rand.Next(56)),
					(byte)rand.Next(256), (byte)rand.Next(256), (byte)rand.Next(256)));
				polygons[i].Fill = new SolidColorBrush(Color.FromArgb((byte)(200 + rand.Next(56)),
					(byte)rand.Next(256), (byte)rand.Next(256), (byte)rand.Next(256)));
				polygons[i].StrokeThickness = rand.Next(6);
				xamlCanvas.Children.Add(polygons[i]);
			}

			DDTranslateAndScaleFrame manipulationFrame = new DDTranslateAndScaleFrame(polygons);
			xamlCanvas.Children.Add(manipulationFrame);
		}

		private DDTranslateAndScaleFrame manipulationFrame = null;
		
		private void xamlButton3_Click(object sender, RoutedEventArgs e)
		{
			List<DDObject> objects = new List<DDObject>();
			foreach (UIElement el in xamlCanvas.Children)
				if (el is DDObject)
					objects.Add(el as DDObject);

			if (manipulationFrame == null)
			{
				manipulationFrame = new DDTranslateAndScaleFrame(objects.ToArray());
				xamlCanvas.Children.Add(manipulationFrame);
			}
			else
			{
				xamlCanvas.Children.Remove(manipulationFrame);
				manipulationFrame = null;
			}
		}

		private void xamlButton4_Click(object sender, RoutedEventArgs e)
		{
			List<DDObject> objects = new List<DDObject>();
			foreach (UIElement el in xamlCanvas.Children)
				if (el is DDObject)
					objects.Add(el as DDObject);
			
			foreach (DDObject o in objects)
				o.ManipulationState = (o.ManipulationState == DDManipulationState.Normal)
					? DDManipulationState.Edited
					: DDManipulationState.Normal;
		}

		private void xamlButton5_Click(object sender, RoutedEventArgs e)
		{
			foreach (UIElement el in xamlCanvas.Children)
			{
				string oldBounds = "";
				if (el is DDObject)
					oldBounds = "OLD: " + (el as DDObject).Bounds.ToString();
				el.RenderTransform = new TranslateTransform(50.5, 50.5);
				if (el is DDObject)
					(el as DDObject).ToolTip = oldBounds + "\nNEW: " +
						(el as DDObject).Bounds.ToString();
			}
		}
	}

	public class DDCanvas : Canvas
	{
		public DDCanvas()
		{
			this.Background = new SolidColorBrush(Color.FromRgb(230, 230, 250));
		}
		
		// Courtesy of http://illef.tistory.com/entry/Canvas-supports-ScrollViewer
		
		protected override Size MeasureOverride(Size constraint)
		{
			Size availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
			double maxHeight = 0;
			double maxWidth = 0;

			foreach (UIElement element in base.InternalChildren)
			{
				if (element != null)
				{
					element.Measure(availableSize);
					double left = Canvas.GetLeft(element);
					double top = Canvas.GetTop(element);
					left += element.DesiredSize.Width;
					top += element.DesiredSize.Height;

					maxWidth = maxWidth < left ? left : maxWidth;
					maxHeight = maxHeight < top ? top : maxHeight;
				}
			}

			return new Size { Height = maxHeight, Width = maxWidth };
		}
	}
}
