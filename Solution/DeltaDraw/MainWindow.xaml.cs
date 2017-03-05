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
using Pantas.DeltaDraw.Core;
using Pantas.DeltaDraw.NumericUpDown;
using Pantas.DeltaDraw.BrushPicker;
using System.Windows.Controls.Primitives;
using Microsoft.Win32;
using System.IO;


namespace Pantas.DeltaDraw.Application
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public static readonly RoutedCommand CopyAsXamlCommand = new RoutedUICommand(
			"Copy as _XAML", "CopyAsXamlCommand", typeof(MainWindow));
		
		public static readonly RoutedCommand AboutCommand = new RoutedUICommand(
			"_About", "AboutCommand", typeof(MainWindow));

		static MainWindow()
		{
			CopyAsXamlCommand.InputGestures.Add(new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift));
		}

		protected DDToolManager ToolManager = null;
		protected DDUndoManager<string> UndoManager = new DDUndoManager<string>();

		protected string UndoBuffer { get; set; }

		protected Size DrawingAreaMargin = new Size(150, 150);

		protected double[] ZoomScale = new double[] { 0.1, 0.2, 0.3, 0.4, 0.5, 1, 1.5, 2, 3, 4, 5, 7, 10 };
		
		const int NormalZoomPos = 5;
		int _zoomScalePos = NormalZoomPos;
		protected int ZoomScalePos
		{
			get { return _zoomScalePos; }
			set
			{
				if (value < 0) value = 0;
				if (value > ZoomScale.Length - 1) value = ZoomScale.Length - 1;
				_zoomScalePos = value;
				UpdateZoom();
			}
		}
		
		protected double ZoomLevel
		{
			get  { return ZoomScale[ZoomScalePos]; }
		}

		private DDObject _boundFillAndStrokeObject = null;
		private DDNodeObject _convertTarget = null;
		private DDTranslateAndScaleFrame _activeTSFrame = null;
		private DDTool _lastConnectedPickTool = null;
		private DDTool _lastConnectedRectangleTool = null;
		private DDTool _lastConnectedEllipseTool = null;
		private DDTool _lastConnectedPenTool = null;

		private string _openFilePath = null;
		protected string OpenFilePath
		{
			get { return _openFilePath; }
			set
			{
				_openFilePath = value;
				UpdateTitle();
			}
		}

		private bool _documentChanged = false;
		protected bool DocumentChanged
		{
			get { return _documentChanged; }
			set
			{
				if (value != _documentChanged)
				{
					_documentChanged = value;
					UpdateTitle();
				}
				xamlDrawingCanvas.IsChanged = value;
				DocumentChangedSinceLastUndo = value;
			}
		}

		protected bool DocumentChangedSinceLastUndo { get; set; }

		protected OpenFileDialog OpenDialog = new OpenFileDialog();
		protected SaveFileDialog SaveDialog = new SaveFileDialog();

		public MainWindow()
		{
			InitializeComponent();
			
			ToolManager = new DDToolManager(xamlDrawingArea, xamlDrawingCanvas, xamlScrollViewer);
			CheckExpanders();

			OpenDialog.DefaultExt = ".dd";
			OpenDialog.Filter = "DeltaDraw image (*.dd)|*.dd|All files (*.*)|*.*";
			SaveDialog.DefaultExt = ".dd";
			SaveDialog.Filter = "DeltaDraw image (*.dd)|*.dd|All files (*.*)|*.*";

			RestUndoManager();

			EventManager.RegisterClassHandler(typeof(Control), UIElement.MouseEnterEvent, new MouseEventHandler(AnyControl_MouseEnter));
			EventManager.RegisterClassHandler(typeof(Control), UIElement.MouseLeaveEvent, new MouseEventHandler(AnyControl_MouseLeave));
		}

		private void ScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
		{
			Point p = e.GetPosition(xamlDrawingCanvas);
			xamlStatusMouseXY.Text = string.Format("{0:0.#}x{1:0.#}", p.X, p.Y);
		}
		
		protected void DeactivateToolButtons()
		{
			DeactivateTool();
			foreach (UIElement e in xamlButtonPanel.Children)
				if (e is RadioButton)
					(e as RadioButton).IsChecked = false;
			CheckExpanders();
		}
		
		protected void DeactivateTool()
		{
			if (ToolManager.ActiveTool != null)
			{
				ToolManager.ActiveTool.Dispose();
				ToolManager.ActiveTool = null;
			}
			TryAddUndoStep();
		}

		private void xamlToolButton_Click(object sender, RoutedEventArgs e)
		{
			RadioButton btn = (sender as RadioButton);
			Type type = btn.Tag as Type;
			if ((ToolManager.ActiveTool != null) && (type == ToolManager.ActiveTool.GetType()))
			{
				btn.IsChecked = false;
				DeactivateTool();
			}
			else
			{
				DeactivateTool();
				ToolManager.ActiveTool = Activator.CreateInstance(type) as DDTool;
			}
			CheckExpanders();
		}

		private void ConvertButton_Click(object sender, RoutedEventArgs e)
		{
			if (!(ToolManager.ActiveTool is DDPickTool))
				return;
			DDNodeObject replacementObject = null;
			if (_convertTarget is DDPolygon)
				replacementObject = (_convertTarget as DDPolygon).CloneToPolyline();
			else if (_convertTarget is DDPolyline)
				replacementObject = (_convertTarget as DDPolyline).CloneToPolygon();
			if (replacementObject != null)
			{
				xamlDrawingCanvas.Children.Remove(_convertTarget);
				_convertTarget = null;
				xamlDrawingCanvas.Children.Add(replacementObject);
				(ToolManager.ActiveTool as DDPickTool).SelectObjects(xamlDrawingCanvas, replacementObject);
			}
		}

		protected void CheckExpanders()
		{
			if (ToolManager.ActiveTool is DDPickTool)
			{
				xamlExpanderPickTool.Visibility = Visibility.Visible;
				DDPickTool pickTool = (ToolManager.ActiveTool as DDPickTool);
				xamlAllowPartialSelection.SetBinding(ToggleButton.IsCheckedProperty,
					new Binding("AllowIntersectionHits") { Source = pickTool, Mode = BindingMode.TwoWay });
	
				if (pickTool != _lastConnectedPickTool)
				{
					pickTool.FrameChanged += TSFrameChangedHandler;
					pickTool.ObjectsAdded += AnyTool_UndoRelevantChange;
					pickTool.ObjectsDeleted += AnyTool_UndoRelevantChange;
					_lastConnectedPickTool = pickTool;
				}
				TSFrameChangedHandler(null);
			}
			else
			{
				xamlExpanderPickTool.Visibility = Visibility.Collapsed;
				xamlExpanderSelectionLayout.Visibility = Visibility.Collapsed;

				BindingOperations.ClearBinding(xamlAllowPartialSelection, ToggleButton.IsCheckedProperty);
			}

			if (ToolManager.ActiveTool is DDRectangleTool)
			{
				xamlExpanderFill.Visibility = Visibility.Visible;
				xamlExpanderStroke.Visibility = Visibility.Visible;
				xamlExpanderRectangle.Visibility = Visibility.Visible;
				BindFillAndStrokeObject(DDRectangleTool.PrototypeRectangle);

				if (_lastConnectedRectangleTool != ToolManager.ActiveTool)
				{
					(ToolManager.ActiveTool as DDRectangleTool).ObjectCreated += AnyTool_UndoRelevantChange;
					_lastConnectedRectangleTool = ToolManager.ActiveTool;
				}
			}
			else
				xamlExpanderRectangle.Visibility = Visibility.Collapsed;

			if (ToolManager.ActiveTool is DDEllipseTool)
			{
				xamlExpanderFill.Visibility = Visibility.Visible;
				xamlExpanderStroke.Visibility = Visibility.Visible;
				BindFillAndStrokeObject(DDEllipseTool.PrototypeEllipse);

				if (_lastConnectedEllipseTool != ToolManager.ActiveTool)
				{
					(ToolManager.ActiveTool as DDEllipseTool).ObjectCreated += AnyTool_UndoRelevantChange;
					_lastConnectedEllipseTool = ToolManager.ActiveTool;
				}
			}

			if (ToolManager.ActiveTool is DDPenTool)
			{
				xamlExpanderFill.Visibility = Visibility.Visible;
				xamlExpanderStroke.Visibility = Visibility.Visible;
				xamlExpanderPenTool.Visibility = Visibility.Visible;

				DDPenTool penTool = ToolManager.ActiveTool as DDPenTool;
				xamlExpanderPolygonButton.SetBinding(UIElement.IsEnabledProperty,
					new Binding("IsDrawing") { Source = penTool, Converter = new DDBooleanInversionConverter() });
				xamlExpanderPolylineButton.SetBinding(UIElement.IsEnabledProperty,
					new Binding("IsDrawing") { Source = penTool, Converter = new DDBooleanInversionConverter() });
				xamlExpanderPolygonButton.SetBinding(ToggleButton.IsCheckedProperty,
					new Binding("CreateClosed") { Source = penTool });

				if (_lastConnectedPenTool != ToolManager.ActiveTool)
				{
					(ToolManager.ActiveTool as DDPenTool).ObjectCreated += AnyTool_UndoRelevantChange;
					_lastConnectedPenTool = ToolManager.ActiveTool;
				}

				BindFillAndStrokeObject(DDPenTool.PrototypeNodeObject);
			}
			else
			{
				xamlExpanderPenTool.Visibility = Visibility.Collapsed;

				BindingOperations.ClearBinding(xamlExpanderPolygonButton, UIElement.IsEnabledProperty);
				BindingOperations.ClearBinding(xamlExpanderPolylineButton, UIElement.IsEnabledProperty);
				BindingOperations.ClearBinding(xamlExpanderPolygonButton, ToggleButton.IsCheckedProperty);
			}

			xamlExpanderPolygon.Visibility = Visibility.Collapsed;
			xamlExpanderPolyline.Visibility = Visibility.Collapsed;

			if ((ToolManager.ActiveTool == null) || (ToolManager.ActiveTool is DDEditTool))
			{
				xamlExpanderFill.Visibility = Visibility.Collapsed;
				xamlExpanderStroke.Visibility = Visibility.Collapsed;
				BindFillAndStrokeObject(null);
			}

			if (ToolManager.ActiveTool == null)
			{
				xamlExpanderCanvasSettings.Visibility = Visibility.Visible;
			}
			else
				xamlExpanderCanvasSettings.Visibility = Visibility.Collapsed;
		}

		void AnyTool_UndoRelevantChange(object sender, EventArgs e)
		{
			TryAddUndoStep();
		}

		protected void TSFrameChangedHandler(DDTranslateAndScaleFrame refFrame)
		{
			if (refFrame == null)
			{
				xamlExpanderSelectionLayout.Visibility = Visibility.Collapsed;
				BindingOperations.ClearAllBindings(xamlLayoutX);
				BindingOperations.ClearAllBindings(xamlLayoutY);
				BindingOperations.ClearAllBindings(xamlLayoutWidth);
				BindingOperations.ClearAllBindings(xamlLayoutHeight);
			}
			else
			{
				xamlExpanderSelectionLayout.Visibility = Visibility.Visible;
				xamlLayoutX.SetBinding(DDNumericUpDown.ValueProperty, new Binding("X")
					{ Source = refFrame, Mode = BindingMode.TwoWay });
				xamlLayoutY.SetBinding(DDNumericUpDown.ValueProperty, new Binding("Y")
					{ Source = refFrame, Mode = BindingMode.TwoWay });
				xamlLayoutWidth.SetBinding(DDNumericUpDown.ValueProperty, new Binding("Width")
					{ Source = refFrame, Mode = BindingMode.TwoWay });
				xamlLayoutHeight.SetBinding(DDNumericUpDown.ValueProperty, new Binding("Height")
					{ Source = refFrame, Mode = BindingMode.TwoWay });
			}

			if ((refFrame != null) && (refFrame.Targets.Count == 1))
			{
				if (!(refFrame.Targets[0] is DDPolyline))
					xamlExpanderFill.Visibility = Visibility.Visible;
				xamlExpanderStroke.Visibility = Visibility.Visible;
				if (refFrame.Targets[0] is DDRectangle)
					xamlExpanderRectangle.Visibility = Visibility.Visible;
				if (refFrame.Targets[0] is DDPolygon)
				{
					_convertTarget = refFrame.Targets[0] as DDNodeObject;
					xamlExpanderPolygon.Visibility = Visibility.Visible;
				}
				if (refFrame.Targets[0] is DDPolyline)
				{
					_convertTarget = refFrame.Targets[0] as DDNodeObject;
					xamlExpanderPolyline.Visibility = Visibility.Visible;
				}
				BindFillAndStrokeObject(refFrame.Targets[0]);
			}
			else
			{
				_convertTarget = null;
				xamlExpanderFill.Visibility = Visibility.Collapsed;
				xamlExpanderStroke.Visibility = Visibility.Collapsed;
				xamlExpanderRectangle.Visibility = Visibility.Collapsed;
				xamlExpanderPolygon.Visibility = Visibility.Collapsed;
				xamlExpanderPolyline.Visibility = Visibility.Collapsed;
				BindFillAndStrokeObject(null);
			}

			_activeTSFrame = refFrame;
			TryAddUndoStep();
		}

		protected void BindFillAndStrokeObject(DDObject target)
		{
			if (_boundFillAndStrokeObject == target)
				return;
			
			if (_boundFillAndStrokeObject != null)
			{
				BindingOperations.ClearBinding(xamlExpanderFillPicker, DDBrushPicker.PickedBrushProperty);
				BindingOperations.ClearBinding(xamlExpanderStrokePicker, DDBrushPicker.PickedBrushProperty);
				BindingOperations.ClearBinding(xamlExpanderStrokeThickness, DDNumericUpDown.ValueProperty);
				BindingOperations.ClearBinding(xamlRectExpanderRadiusX, DDNumericUpDown.ValueProperty);
				BindingOperations.ClearBinding(xamlRectExpanderRadiusY, DDNumericUpDown.ValueProperty);
				_boundFillAndStrokeObject = null;
			}

			if (target != null)
			{
				xamlExpanderFillPicker.SetBinding(DDBrushPicker.PickedBrushProperty,
					new Binding("Fill") { Source = target, Mode = BindingMode.TwoWay });
				xamlExpanderStrokePicker.SetBinding(DDBrushPicker.PickedBrushProperty,
					new Binding("Stroke") { Source = target, Mode = BindingMode.TwoWay });
				xamlExpanderStrokeThickness.SetBinding(DDNumericUpDown.ValueProperty,
					new Binding("StrokeThickness") { Source = target, Mode = BindingMode.TwoWay });
				if (target is DDRectangle)
				{
					xamlRectExpanderRadiusX.SetBinding(DDNumericUpDown.ValueProperty,
						new Binding("CornerRadiusX") { Source = target, Mode = BindingMode.TwoWay });
					xamlRectExpanderRadiusY.SetBinding(DDNumericUpDown.ValueProperty,
						new Binding("CornerRadiusY") { Source = target, Mode = BindingMode.TwoWay });
				}
			}

			_boundFillAndStrokeObject = target;
		}

		protected void UpdateZoom()
		{
			xamlDrawingViewbox.Width = xamlDrawingArea.Width * ZoomLevel;
			xamlDrawingViewbox.Height = xamlDrawingArea.Height * ZoomLevel;
			xamlScrollViewer.UpdateLayout();
			xamlStatusZoom.Text = "x" + ZoomLevel;
		}

		protected void CenterScrollAreaOnRelativePoint(Point percentXY)
		{
			if (xamlScrollViewer.ExtentWidth > xamlScrollViewer.ViewportWidth)
				xamlScrollViewer.ScrollToHorizontalOffset(percentXY.X * xamlScrollViewer.ExtentWidth - xamlScrollViewer.ViewportWidth / 2);
			if (xamlScrollViewer.ExtentHeight > xamlScrollViewer.ViewportHeight)
				xamlScrollViewer.ScrollToVerticalOffset(percentXY.Y * xamlScrollViewer.ExtentHeight - xamlScrollViewer.ViewportHeight / 2);
		}

		private void xamlDrawingCanvasBorder_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			xamlDrawingArea.Width = e.NewSize.Width + DrawingAreaMargin.Width * 2;
			xamlDrawingArea.Height = e.NewSize.Height + DrawingAreaMargin.Height * 2;
			UpdateZoom();
		}

		private void xamlDrawingArea_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			Point absoluteXY = e.GetPosition(xamlDrawingViewbox);
			Point percentXY = new Point(absoluteXY.X / xamlDrawingViewbox.Width,
				absoluteXY.Y / xamlDrawingViewbox.Height);
			if (e.Delta > 0)
				ZoomScalePos++;
			else if (e.Delta < 0)
				ZoomScalePos--;
			CenterScrollAreaOnRelativePoint(percentXY);
			e.Handled = true;
		}

		private void ScrollViewerCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			if ((e.Command == ApplicationCommands.Delete) ||
				(e.Command == ApplicationCommands.Copy) ||
				(e.Command == ApplicationCommands.Cut) ||
				(e.Command == CopyAsXamlCommand))
			e.CanExecute = (_activeTSFrame != null);

			if (e.Command == ApplicationCommands.Paste)
				e.CanExecute = Clipboard.ContainsData(DDPickTool.DataFormat);
		}

		private void ScrollViewerCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (e.Command == ApplicationCommands.Delete)
			{
				if (ToolManager.ActiveTool is DDPickTool)
					(ToolManager.ActiveTool as DDPickTool).DeleteSelection();
			}
			if (e.Command == ApplicationCommands.Cut)
			{
				if (ToolManager.ActiveTool is DDPickTool)
					(ToolManager.ActiveTool as DDPickTool).CutToClipboard();
			}
			if (e.Command == ApplicationCommands.Copy)
			{
				if (ToolManager.ActiveTool is DDPickTool)
					(ToolManager.ActiveTool as DDPickTool).CopyToClipboard();
			}
			if (e.Command == ApplicationCommands.Paste)
			{
				if (ToolManager.ActiveTool is DDPickTool)
					(ToolManager.ActiveTool as DDPickTool).PasteFromClipboard(xamlDrawingCanvas);
			}
			if (e.Command == CopyAsXamlCommand)
			{
				if (ToolManager.ActiveTool is DDPickTool)
				{
					Canvas carrierCanvas = xamlDrawingCanvas.CloneToCLR(true);
					if (xamlCanvasBrushPicker.PickedBrush != null)
						carrierCanvas.Background = xamlCanvasBrushPicker.PickedBrush.Clone();
					else
						carrierCanvas.Background = null;
					(ToolManager.ActiveTool as DDPickTool).CopyToClipboardAsXaml(carrierCanvas);
				}
			}
		}

		private void WindowCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			if ((e.Command == ApplicationCommands.New) ||
				(e.Command == ApplicationCommands.Open) ||
				(e.Command == ApplicationCommands.Close) ||
				(e.Command == AboutCommand))
				e.CanExecute = true;
			if ((e.Command == ApplicationCommands.Save) ||
				(e.Command == ApplicationCommands.SaveAs))
				e.CanExecute = DocumentChanged;
			if (e.Command == ApplicationCommands.Undo)
				e.CanExecute = UndoManager.CanUndo();
			if (e.Command == ApplicationCommands.Redo)
				e.CanExecute = UndoManager.CanRedo();
		}

		private void WindowCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (e.Command == ApplicationCommands.New) New();
			else if (e.Command == ApplicationCommands.Open) Open();
			else if (e.Command == ApplicationCommands.Save) Save();
			else if (e.Command == ApplicationCommands.SaveAs) SaveAs();
			else if (e.Command == ApplicationCommands.Close) Close();
			else if (e.Command == AboutCommand) ShowAbout();
			else if (e.Command == ApplicationCommands.Undo) Undo();
			else if (e.Command == ApplicationCommands.Redo) Redo();
		}

		protected void ClearCanvas()
		{
			DeactivateToolButtons();
			xamlDrawingCanvas.Children.Clear();
			DocumentChanged = false;
		}

		protected bool New()
		{
			if (xamlDrawingCanvas.IsChanged)
				if (!OfferSave())
					return false;

			ClearCanvas();
			//xamlCanvasBrushPicker.PickedBrush = new SolidColorBrush(Colors.White);
			//xamlDrawingCanvasBackground.Background = new SolidColorBrush(Colors.White);
			xamlDrawingCanvasBorder.Width = 200;
			xamlDrawingCanvasBorder.Height = 150;
			ZoomScalePos = NormalZoomPos;
			OpenFilePath = null;
			DocumentChanged = false;
			UpdateTitle();
			RestUndoManager();
			return true;
		}

		protected bool OfferSave()
		{
			MessageBoxResult res = MessageBox.Show("Do you wish to save changes?", "Save changes",
				MessageBoxButton.YesNoCancel);
			switch (res)
			{
				case MessageBoxResult.Yes:
					return Save();
				case MessageBoxResult.No:
					return true;
				case MessageBoxResult.Cancel:
					return false;
			}
			return false;
		}

		protected bool Save()
		{
			bool res = false;
			if (string.IsNullOrEmpty(OpenFilePath))
				return SaveAs();
			else
				if (SaveToFile(OpenFilePath))
				{
					DocumentChanged = false;
					res = true;
				}
			UpdateTitle();
			RestUndoManager();
			return res;
		}

		protected bool SaveAs()
		{
			bool res = false;
			if (!string.IsNullOrEmpty(OpenFilePath))
				SaveDialog.FileName = OpenFilePath;
			if (SaveDialog.ShowDialog() == true)
			{
				OpenFilePath = SaveDialog.FileName;
				if (SaveToFile(OpenFilePath))
				{
					DocumentChanged = false;
					res = true;
				}
			}
			UpdateTitle();
			RestUndoManager();
			return res;
		}

		protected string SaveToString()
		{
			Canvas canvas = xamlDrawingCanvas.CloneToCLR(false);
			//canvas.Background = xamlCanvasBrushPicker.PickedBrush;
			return DDTools.WriteToXaml(canvas, false);
		}

		protected bool SaveToFile(string filePath)
		{
			string xaml = SaveToString();
			try
			{
				TextWriter writer = new StreamWriter(filePath);
				writer.Write(xaml);
				writer.Close();
				return true;
			}
			catch (Exception e)
			{
				MessageBox.Show("Failed to write to file.\nMessage: " + e.Message, "File error");
			}
			return false;
		}

		protected bool Open()
		{
			if (xamlDrawingCanvas.IsChanged)
				if (!OfferSave())
					return false;			
			
			bool res = false;
			OpenDialog.FileName = SaveDialog.FileName;
			if (OpenDialog.ShowDialog() == true)
			{
				ClearCanvas();
				ZoomScalePos = NormalZoomPos;
				OpenFilePath = null;

				if (OpenFromFile(OpenDialog.FileName))
				{
					OpenFilePath = OpenDialog.FileName;
					res = true;
				}

				DocumentChanged = false;
			}
			UpdateTitle();
			RestUndoManager();
			return res;
		}

		protected bool SetFromString(string xaml)
		{
			try
			{
				object o = DDTools.ReadFromXaml(xaml);
				if (o is Canvas)
				{
					//xamlDrawingCanvasBackground.Background = (o as Canvas).Background.Clone();
					(o as Canvas).ClearValue(Panel.BackgroundProperty);
					xamlDrawingCanvas.SetFromCanvas(o as Canvas);
					return true;
				}
			}
			catch (Exception)
			{
				return false;
			}
			return false;
		}

		protected bool OpenFromFile(string filePath)
		{
			if (!File.Exists(filePath))
				return false;
			
			string xaml;
			try
			{
				TextReader reader = new StreamReader(filePath);
				xaml = reader.ReadToEnd();
				reader.Close();
			}
			catch (Exception e)
			{
				MessageBox.Show("Failed to read file.\nMessage: " + e.Message, "File error");
				return false;
			}
			return SetFromString(xaml);
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			if (xamlDrawingCanvas.IsChanged)
				if (!OfferSave())
					e.Cancel = true;
			
			base.OnClosing(e);
		}

		protected void ShowAbout()
		{
			MessageBox.Show("DeltaDraw\n(c)2011, Ivan Pantic\nNRT 224/05");
		}
		
		protected void RestUndoManager()
		{
			UndoManager.Clear();
			UndoBuffer = SaveToString();
		}

		protected void TryAddUndoStep()
		{
			if (DocumentChangedSinceLastUndo)
			{
				string state = SaveToString();
				if (string.IsNullOrEmpty(UndoBuffer))
					UndoBuffer = state;
				if (state == UndoBuffer)
					return;
				UndoManager.AddStep(UndoBuffer);
				UndoBuffer = state;
				DocumentChangedSinceLastUndo = false;
			}
		}

		protected void Undo()
		{
			if ((!UndoManager.CanUndo()) || (string.IsNullOrEmpty(UndoBuffer)))
				return;
			
			TryAddUndoStep();
			string state = SaveToString();
			string undoState = UndoManager.Undo(state);
			ClearCanvas();
			SetFromString(undoState);
			UndoBuffer = undoState;
			DocumentChangedSinceLastUndo = false;
		}

		protected void Redo()
		{
			if ((!UndoManager.CanRedo()) || (string.IsNullOrEmpty(UndoBuffer)))
				return;

			string state = SaveToString();
			string redoState = UndoManager.Redo(state);
			ClearCanvas();
			SetFromString(redoState);
			UndoBuffer = redoState;
			DocumentChangedSinceLastUndo = false;
		}

		protected void UpdateTitle()
		{
			this.Title = "DeltaDraw";
			if (!String.IsNullOrEmpty(OpenFilePath))
				Title += " - " + OpenFilePath;
			if (DocumentChanged)
				Title += "*";
			xamlStatusUnsavedItem.Visibility = (DocumentChanged ? Visibility.Visible : Visibility.Collapsed);
		}
		
		private void xamlDrawingCanvas_RelevantPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			DocumentChanged = xamlDrawingCanvas.IsChanged;
		}

		private void AnyControl_MouseEnter(object sender, MouseEventArgs e)
		{
			if (sender is Control)
				if ((sender as Control).ToolTip != null)
				xamlStatusInfo.Text = (sender as Control).ToolTip.ToString();
		}

		private void AnyControl_MouseLeave(object sender, MouseEventArgs e)
		{
			if (sender is Control)
				if ((sender as Control).ToolTip != null)
				xamlStatusInfo.Text = "";
		}
	}

	[ValueConversion(typeof(bool), typeof(bool))]
	public class DDBooleanInversionConverter : IValueConverter
	{
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is bool)
				return !((bool)value);
			else
				return false;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is bool)
				return !((bool)value);
			else
				return false;
		}

		#endregion
	}
}
