using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Globalization;
using System.Windows.Threading;

namespace Pantas.DeltaDraw.NumericUpDown
{
	/// <summary>
	/// Interaction logic for NumericUpDown.xaml
	/// </summary>
	public partial class DDNumericUpDown : UserControl
	{
		public static int InitialMouseDownDelay = 300;
		public static int ConsequentMouseDownDelay = 50;
		
		private readonly DispatcherTimer _mouseDownTimer = new DispatcherTimer();		
		
		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
			"Value", typeof(double), typeof(DDNumericUpDown),
			new PropertyMetadata(0d, null, CoerceValue));

		public double Value
		{
			get { return (double)this.GetValue(ValueProperty); }
			set { this.SetValue(ValueProperty, value); }
		}

		private static object CoerceValue(DependencyObject d, object value)
		{
			double num = (double)value;
			double min = (double)d.GetValue(MinValueProperty);
			double max = (double)d.GetValue(MaxValueProperty);
			if (num < min)
				num = min;
			if (num > max)
				num = max;
			return num;
		}

		public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
			"MinValue", typeof(double), typeof(DDNumericUpDown),
			new PropertyMetadata(double.NegativeInfinity, null, CoerceMinValue));

		public double MinValue
		{
			get { return (double)this.GetValue(MinValueProperty); }
			set { this.SetValue(MinValueProperty, value); }
		}

		private static object CoerceMinValue(DependencyObject d, object value)
		{
			double num = (double)value;
			double max = (double)d.GetValue(MaxValueProperty);
			if (num > max)
				num = max;
			return num;
		}

		public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
			"MaxValue", typeof(double), typeof(DDNumericUpDown),
			new PropertyMetadata(double.PositiveInfinity, null, CoerceMaxValue));

		public double MaxValue
		{
			get { return (double)this.GetValue(MaxValueProperty); }
			set { this.SetValue(MaxValueProperty, value); }
		}

		private static object CoerceMaxValue(DependencyObject d, object value)
		{
			double num = (double)value;
			double min = (double)d.GetValue(MinValueProperty);
			if (num < min)
				num = min;
			return num;
		}

		public static readonly DependencyProperty DeltaProperty = DependencyProperty.Register(
			"Delta", typeof(double), typeof(DDNumericUpDown),
			new PropertyMetadata(1d));

		public double Delta
		{
			get { return (double)this.GetValue(DeltaProperty); }
			set { this.SetValue(DeltaProperty, value); }
		}

		public static readonly DependencyProperty CtrlMultiplierProperty = DependencyProperty.Register(
			"CtrlMultiplier", typeof(double), typeof(DDNumericUpDown),
			new PropertyMetadata(10d));

		public double CtrlMultiplier
		{
			get { return (double)this.GetValue(CtrlMultiplierProperty); }
			set { this.SetValue(CtrlMultiplierProperty, value); }
		}

		public DDNumericUpDown()
		{
			InitializeComponent();

			_mouseDownTimer.Interval = TimeSpan.FromMilliseconds(InitialMouseDownDelay);
			_mouseDownTimer.Tick += MouseDownTimer_Tick;
		}

		public void IncreaseNumber()
		{
			if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
				Value += Delta * CtrlMultiplier;
			else
				Value += Delta;
		}
		
		public void DecreaseNumber()
		{
			if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
				Value -= Delta * CtrlMultiplier;
			else
				Value -= Delta;
		}

		void MouseDownTimer_Tick(object sender, EventArgs e)
		{
			if (Mouse.LeftButton == MouseButtonState.Pressed)
			{
				if (Mouse.DirectlyOver == xamlUpButton)
				{
					IncreaseNumber();
					_mouseDownTimer.Interval = TimeSpan.FromMilliseconds(ConsequentMouseDownDelay);
					return;
				}
				else if (Mouse.DirectlyOver == xamlDownButton)
				{
					DecreaseNumber();
					_mouseDownTimer.Interval = TimeSpan.FromMilliseconds(ConsequentMouseDownDelay);
					return;
				}
			}
			_mouseDownTimer.Interval = TimeSpan.FromMilliseconds(InitialMouseDownDelay);
			_mouseDownTimer.Stop();
		}

		private void xamlTextBox_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (e.Delta > 0)
				IncreaseNumber();
			else
				DecreaseNumber();
		}

		private void xamlUpButton_Click(object sender, RoutedEventArgs e)
		{
			xamlTextBox.GetBindingExpression(TextBox.TextProperty).UpdateSource();
			IncreaseNumber();
			_mouseDownTimer.Start();
		}

		private void xamlDownButton_Click(object sender, RoutedEventArgs e)
		{
			xamlTextBox.GetBindingExpression(TextBox.TextProperty).UpdateSource();
			DecreaseNumber();
			_mouseDownTimer.Start();
		}

		private void xamlButton_GotFocus(object sender, RoutedEventArgs e)
		{
			xamlTextBox.Focus();
		}

		private void xamlTextBox_TargetUpdated(object sender, DataTransferEventArgs e)
		{
			xamlTextBox.SelectionStart = xamlTextBox.Text.Length;
		}

		private void xamlTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
				xamlTextBox.GetBindingExpression(TextBox.TextProperty).UpdateSource();
		}
	}

	[ValueConversion(typeof(String), typeof(double))]
	public class NumberConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value != null)
				return ((double)value).ToString("0.##", CultureInfo.InvariantCulture);
			else
				return "0";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is String))
				return 0d;

			string txt = value as string;

			StringBuilder numberPart = new StringBuilder();
			for (int i = 0; i < txt.Length; i++)
				if (((txt[i] >= '0') && (txt[i] <= '9'))
					|| ((i == 0) && (txt[i] == '-'))
					|| ((i > 0) && (i < txt.Length - 1) && (txt[i] == '.')))
					numberPart.Append(txt[i]);

			double num;
			if (double.TryParse(numberPart.ToString(), out num))
				return num;
			else
				return 0d;
		}
	}

}
