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
using System.ComponentModel;

namespace Pantas.DeltaDraw.ColorPicker
{
	/// <summary>
	/// Interaction logic for UserControl1.xaml
	/// </summary>
	public partial class DDColorPicker : UserControl
	{
		private readonly Color[] _baseColors = { Colors.Red, Colors.Yellow, Colors.Lime, Colors.Aqua, Colors.Blue, Colors.Fuchsia, Colors.Red };
		protected bool PickedColorChangeLock = false;

		#region PickedColor Property
		public static readonly DependencyProperty PickedColorProperty = DependencyProperty.Register(
			"PickedColor", typeof(Color), typeof(DDColorPicker),
			new PropertyMetadata(Colors.Transparent, PickedColorPropertyChangedCallback));

		static void PickedColorPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			DDColorPicker realSender = d as DDColorPicker;
			if (!realSender.PickedColorChangeLock)
			{
				realSender.PickedColorChangeLock = true;
				realSender.SetFromColor((Color)e.NewValue);
				realSender.PickedColorChangeLock = false;
			}
			realSender.OnPickedColorChanged();
		}

		public Color PickedColor
		{
			get { return (Color)GetValue(PickedColorProperty); }
			set { SetValue(PickedColorProperty, value); }
		}
		#endregion

		#region InnerPadding Property
		public static DependencyProperty InnerPaddingProperty = DependencyProperty.Register(
			"InnerPadding", typeof(double), typeof(DDColorPicker),
			new PropertyMetadata(5d));

		public double InnerPadding
		{
			get { return (double)this.GetValue(InnerPaddingProperty); }
			set { this.SetValue(InnerPaddingProperty, value); }
		}
		#endregion
		
		#region ShowAlphaScale Property
		public static DependencyProperty ShowAlphaScaleProperty = DependencyProperty.Register(
			"ShowAlphaScale", typeof(bool), typeof(DDColorPicker),
			new PropertyMetadata(true, ShowAlphaScalePropertyChangedCallback));

		private static void ShowAlphaScalePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs args)
		{
			DDColorPicker sender = (DDColorPicker)d;
			bool newValue = (bool)args.NewValue;
			bool oldValue = (bool)args.OldValue;

			if (sender.xamlMainGrid.ColumnDefinitions.Count < 5)
				return;

			if (newValue)
			{
				sender.xamlMainGrid.ColumnDefinitions[3].MaxWidth = double.PositiveInfinity;
				sender.xamlMainGrid.ColumnDefinitions[4].MaxWidth = double.PositiveInfinity;
			}
			else
			{
				sender.xamlMainGrid.ColumnDefinitions[3].MaxWidth = 0;
				sender.xamlMainGrid.ColumnDefinitions[4].MaxWidth = 0;
			}
		}

		public bool ShowAlphaScale
		{
			get { return (bool)this.GetValue(ShowAlphaScaleProperty); }
			set { this.SetValue(ShowAlphaScaleProperty, value); }
		}
		#endregion

		double _pickedBaseValue;
		protected double PickedBaseValue
		{
			get { return _pickedBaseValue; }
			set
			{
				_pickedBaseValue = value;
				PickBaseColor(_pickedBaseValue);
			}
		}

		double _pickedAlphaValue;
		protected double PickedAlphaValue
		{
			get { return _pickedAlphaValue; }
			set
			{
				_pickedAlphaValue = value;
				PickAlphaValue(_pickedAlphaValue);
			}
		}
		
		Point _pickedDetailPoint;
		protected Point PickedDetailPoint
		{
			get { return _pickedDetailPoint; }
			set
			{
				_pickedDetailPoint = value;
				PickDetailColor(_pickedDetailPoint);
			}
		}

		public DDColorPicker()
		{
			InitializeComponent();
			
			PickedColorChangeLock = true;
			PickedBaseValue = 0;
			PickedAlphaValue = 0;
			PickedDetailPoint = new Point(0,0);
			PickedColorChangeLock = false;
		}

		protected Color PickedPointToBaseColor(double pickedY)
		{
			if (xamlBaseColors.ActualHeight == 0)
				return new Color();

			double ratioStep = 1D / ((double)_baseColors.Length - 1D);
			double ratio = pickedY / xamlBaseColors.ActualHeight;
			int index = (int)(ratio * (_baseColors.Length - 1));
			double localRatio = (ratio - index * ratioStep) / ratioStep;
			Color color1 = _baseColors[index];
			Color color2 = index < _baseColors.Length - 1 ? _baseColors[index + 1] : color1;

			return Color.FromArgb(
				(byte)(color1.A * (1 - localRatio) + color2.A * localRatio),
				(byte)(color1.R * (1 - localRatio) + color2.R * localRatio),
				(byte)(color1.G * (1 - localRatio) + color2.G * localRatio),
				(byte)(color1.B * (1 - localRatio) + color2.B * localRatio));
		}

		protected byte PickedPointToAlpha(double pickedY)
		{
			if (xamlAlphaScale.ActualHeight == 0)
				return 255;

			return (byte)(255d - (Math.Max(pickedY - 1, 0) * 255d) / (xamlAlphaScale.ActualHeight - 2));
		}

		protected Color PickedPointToDetailColor(Point pickedPoint)
		{
			// ratio: 1 = base color, 0 = black (y-axis) or white (x-axis)
			
			Point ratio = new Point(pickedPoint.X / xamlDetailColor.ActualWidth,
				1 - pickedPoint.Y / xamlDetailColor.ActualHeight);
			Color baseColor = (xamlDetailColor.Background as SolidColorBrush).Color;
			return Color.FromArgb(
				PickedColor.A,
				(byte)((255 * (1 - ratio.X) + baseColor.R * ratio.X) * ratio.Y),
				(byte)((255 * (1 - ratio.X) + baseColor.G * ratio.X) * ratio.Y),
				(byte)((255 * (1 - ratio.X) + baseColor.B * ratio.X) * ratio.Y));
		}

		protected void SetFromColor(Color color)
		{
			if ((DesignerProperties.GetIsInDesignMode(this)) || (!IsMeasureValid))
				return;

			/*
			 * In this control, base colors are such colors where one component is set to 0 
			 * and another to 255 (third one can be anything from 0 to 255). This construct
			 * is useful because these are the colors represented on the xamlBaseColors scale.
			 * 
			 * This function uses an assumption that highest component of inputed color
			 * corresponds with the highest one of base color (255) and the lowest one with 0.			 
			 * 
			 * Therefore, we must must sort indexes of colors in ascending order.
			 * We need to know which value in bytes is the highest and which the lowest, 
			 * without disrupting the order of bytes (which we'll need later). 
			 * After this code, bytes[indexes[0]] will be the lowest byte and bytes[indexes[2]] the highest.
			 */
			
			byte[] bytes = new byte[] { color.R, color.G, color.B };
			byte[] indexes = new byte[] { 0, 1, 2 };
			for (int i = 0; i < 2; i++)
				for (int j = i + 1; j < 3; j++)
					if (bytes[indexes[i]] > bytes[indexes[j]])
					{
						byte temp = indexes[i];
						indexes[i] = indexes[j];
						indexes[j] = temp;
					}

			/*
			 * Now we can find base color of the inputed color, as well as x and y,
			 * which are distances of the picked color from the base color
			 * (in the upper right corner of the detail picker).
			 * 
			 * Note that in this case, y is oriented from DOWN UP (y == 0 is at the bottom of control).
			 * This is opposite of normal screen coordinates, but matches the logic that x & y are
			 * parameters applied to the base color. Therefore, x,y == [1,1] would be the base color,
			 * lower x's add whiteness and lower y's darkness. [0,1] is white, and [1,0] and [0,0] are black.
			 */


			double x;
			if ((double)bytes[indexes[0]] == 0)
				x = 1;
			else 
				x = 1D - (double)bytes[indexes[0]] / (double)bytes[indexes[2]];
			double y = (double)bytes[indexes[2]] / 255D;

			bytes[indexes[0]] = 0;
			bytes[indexes[1]] = (byte)(((double)bytes[indexes[1]] - 255D * y * (1D - x)) / (x * y));
			bytes[indexes[2]] = 255;

			Color baseColor = Color.FromRgb(bytes[0], bytes[1], bytes[2]);

			/*
			 * Now that we have baseColor, we need to find its position on xamlBaseColors,
			 * so we could position the selector.
			 */

			double pickedY = 0;
			double ratioStep = 1D / ((double)_baseColors.Length - 1D);
			for (int i = 0; i < _baseColors.Length - 1; i++)
			{			
				if (((_baseColors[i].R & _baseColors[i + 1].R) <= baseColor.R)
					&& ((_baseColors[i].R | _baseColors[i + 1].R) >= baseColor.R)
					&& ((_baseColors[i].G & _baseColors[i + 1].G) <= baseColor.G)
					&& ((_baseColors[i].G | _baseColors[i + 1].G) >= baseColor.G)
					&& ((_baseColors[i].B & _baseColors[i + 1].B) <= baseColor.B)
					&& ((_baseColors[i].B | _baseColors[i + 1].B) >= baseColor.B))
				{
					double localDiff = Math.Abs(_baseColors[i].R - baseColor.R)
						+ Math.Abs(_baseColors[i].G - baseColor.G)
						+ Math.Abs(_baseColors[i].B - baseColor.B);
					double localRatio = (localDiff * ratioStep) / 255D;
					double totalRatio = localRatio + ratioStep * i;
					pickedY = totalRatio * xamlBaseColors.ActualHeight;
					break;
				}
			}

			// *NEW*
			// Find position on alpha picker scale according to color's alpha value

			double pickedAlphaY = xamlAlphaScale.ActualHeight - ((double)color.A * xamlAlphaScale.ActualHeight) / 255d;

			// Apply the findings.
			// There's a little bit of overhead here, but IMO this is logically the soundest approach.

			PickedBaseValue = pickedY;
			PickedAlphaValue = pickedAlphaY;
			PickedDetailPoint = new Point(
				x * xamlDetailColor.ActualWidth,
				(1 - y) * xamlDetailColor.ActualHeight);
		}

		private void PickBaseColor(double pickedY)
		{
			xamlDetailColor.Background = new SolidColorBrush(PickedPointToBaseColor(pickedY));

			xamlBaseSelector.Margin = new Thickness(
				xamlBaseSelector.Margin.Left,
				pickedY - xamlBaseSelector.Height / 2 + xamlBaseColorsBorder.Margin.Top + 2,
				xamlBaseSelector.Margin.Right,
				xamlBaseSelector.Margin.Bottom);

			PickDetailColor(PickedDetailPoint);
		}

		private void PickAlphaValue(double pickedY)
		{
			if (!PickedColorChangeLock)
			{
				PickedColorChangeLock = true;
				PickedColor = Color.FromArgb(PickedPointToAlpha(pickedY), PickedColor.R, PickedColor.G, PickedColor.B);
				PickedColorChangeLock = false;
			}
			xamlAlphaSelector.Margin = new Thickness(
				xamlAlphaSelector.Margin.Left,
				pickedY - xamlAlphaSelector.Height / 2 + xamlAlphaScaleBorder.Margin.Top + 2,
				xamlAlphaSelector.Margin.Right,
				xamlAlphaSelector.Margin.Bottom);
		}

		private void PickDetailColor(Point pickedPoint)
		{
			if (!PickedColorChangeLock)
			{
				PickedColorChangeLock = true;
				PickedColor = PickedPointToDetailColor(pickedPoint);
				PickedColorChangeLock = false;
			}

			xamlDetailSelector.Background = new SolidColorBrush(PickedColor);
			
			try
			{
				xamlDetailSelector.Margin = new Thickness(
					xamlDetailColorBorder.Margin.Left + pickedPoint.X - xamlDetailSelector.Width / 2,
					xamlDetailColorBorder.Margin.Top + pickedPoint.Y - xamlDetailSelector.Height / 2,
					0, 0);
			}
			catch (ArgumentException)
			{
				// silent fail
			}
		}

		private void xamlBaseColors_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				PickedBaseValue = e.GetPosition(sender as Rectangle).Y;
				OnColorPicked();
			}
		}

		private void xamlBaseColors_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				PickedBaseValue = e.GetPosition(sender as Rectangle).Y;
				OnColorPicked();
			}
		}

		private void xamlAlphaScale_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				PickedAlphaValue = e.GetPosition(sender as Rectangle).Y;
				OnColorPicked();
			}
		}

		private void xamlAlphaScale_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				PickedAlphaValue = e.GetPosition(sender as Rectangle).Y;
				OnColorPicked();
			}
		}

		private void xamlDetailColor_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				PickedDetailPoint = e.GetPosition(sender as Control);
				OnColorPicked();
			}
		}

		private void xamlDetailColor_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				PickedDetailPoint = e.GetPosition(sender as Control);
				OnColorPicked();
			}
		}

		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);

			this.SetFromColor(PickedColor);
		}

		protected virtual void OnColorPicked()
		{
			if (ColorPicked != null)
				ColorPicked(this, PickedColor);
		}

		protected virtual void OnPickedColorChanged()
		{
			if (PickedColorChanged != null)
				PickedColorChanged(this, PickedColor);
		}

		public event ColorPickerDelegate ColorPicked;
		public event ColorPickerDelegate PickedColorChanged;
	}

	public delegate void ColorPickerDelegate(object sender, Color refColor);

	/// <summary>
	/// Control that obeys ClipToBounds==False in EVERY panel control, not just Canvas
	/// Source: http://drwpf.com/blog/2007/12/28/cliptoboundsmaybe/
	/// </summary>
	public class DDNonClippingControl : Control
	{
		protected override Geometry GetLayoutClip(Size layoutSlotSize)
		{
			return ClipToBounds ? base.GetLayoutClip(layoutSlotSize) : null;
		}
	}


	/// <summary>
	/// Class that converts a normal 4 channel color into a 3 channel color (with alpha set to 255)
	/// </summary>
	[ValueConversion(typeof(Color), typeof(Color))]
	public class DDColorToPureColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is Color)
			{
				Color c = (Color)value;
				return Color.FromArgb(255, c.R, c.G, c.B);
			}
			else
				return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
