using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Reflection;
using System.ComponentModel;

namespace Pantas.DeltaDraw.BrushPicker
{
	/// <summary>
	/// Interaction logic for UserControl1.xaml
	/// </summary>
	public partial class DDBrushPicker : UserControl, INotifyPropertyChanged
	{	
		protected bool ExternalLock = false;
		
		#region PickedBrush Property
		public static DependencyProperty PickedBrushProperty = DependencyProperty.Register(
			"PickedBrush", typeof(Brush), typeof(DDBrushPicker),
			new PropertyMetadata(null, PickedBrushPropertyChangedCallback));

		private static void PickedBrushPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs args)
		{
			DDBrushPicker sender = (DDBrushPicker)d;
			Brush newValue = (Brush)args.NewValue;
			
			if (sender.ExternalLock)
				return;

			sender.ExternalLock = true;

			if (newValue == null)
			{
				sender.SelectedTab = 0;
			}
			else if (newValue is SolidColorBrush)
			{
				if (newValue != sender.SolidColorBrush)
				{
					sender.SolidColorBrush = (newValue as SolidColorBrush).Clone();
				}
				sender.SelectedTab = 1;
			}
			else if (newValue is LinearGradientBrush)
			{
				if (newValue != sender.LinearGradientBrush)
				{
					sender.LinearGradientBrush = (newValue as LinearGradientBrush).Clone();
				}
				sender.SelectedTab = 2;
			}
			else if (newValue is RadialGradientBrush)
			{
				if (newValue != sender.RadialGradientBrush)
				{
					sender.RadialGradientBrush = (newValue as RadialGradientBrush).Clone();
				}
				sender.SelectedTab = 3;
			}

			sender.ExternalLock = false;

			sender.UpdatePickedBrush();
			
		}
	
		public Brush PickedBrush
		{
			get { return (Brush)this.GetValue(PickedBrushProperty); }
			set { this.SetValue(PickedBrushProperty, value); }
		}
		#endregion

		#region SolidColorBrush Property
		internal static DependencyProperty SolidColorBrushProperty = DependencyProperty.Register(
			"SolidColorBrush", typeof(SolidColorBrush), typeof(DDBrushPicker),
			new PropertyMetadata(new SolidColorBrush(Colors.White), AnyTabBrushPropertyChangedCallback));

		internal SolidColorBrush SolidColorBrush
		{
			get { return (SolidColorBrush)this.GetValue(SolidColorBrushProperty); }
			set { this.SetValue(SolidColorBrushProperty, value); }
		}
		#endregion

		#region LinearGradientBrush Property
		internal static DependencyProperty LinearGradientBrushProperty = DependencyProperty.Register(
			"LinearGradientBrush", typeof(LinearGradientBrush), typeof(DDBrushPicker),
			new PropertyMetadata(new LinearGradientBrush(Colors.White, Colors.Black, 0), AnyTabBrushPropertyChangedCallback));

		internal LinearGradientBrush LinearGradientBrush
		{
			get { return (LinearGradientBrush)this.GetValue(LinearGradientBrushProperty); }
			set { this.SetValue(LinearGradientBrushProperty, value); }
		}
		#endregion

		#region RadialGradientBrush Property
		internal static DependencyProperty RadialGradientBrushProperty = DependencyProperty.Register(
			"RadialGradientBrush", typeof(RadialGradientBrush), typeof(DDBrushPicker),
			new PropertyMetadata(new RadialGradientBrush(Colors.White, Colors.Black), AnyTabBrushPropertyChangedCallback));

		internal RadialGradientBrush RadialGradientBrush
		{
			get { return (RadialGradientBrush)this.GetValue(RadialGradientBrushProperty); }
			set { this.SetValue(RadialGradientBrushProperty, value); }
		}
		#endregion

		private static void AnyTabBrushPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs args)
		{
			DDBrushPicker sender = (DDBrushPicker)d;
			sender.BindBrushes();
			sender.UpdatePickedBrush();
		}

		#region LinearStart Property
		public static DependencyProperty LinearStartProperty = DependencyProperty.Register(
			"LinearStart", typeof(DDDependencyPoint), typeof(DDBrushPicker),
			new PropertyMetadata(null));
	
		public DDDependencyPoint LinearStart
		{
			get { return (DDDependencyPoint)this.GetValue(LinearStartProperty); }
			set { this.SetValue(LinearStartProperty, value); }
		}
		#endregion

		#region LinearEnd Property
		public static DependencyProperty LinearEndProperty = DependencyProperty.Register(
			"LinearEnd", typeof(DDDependencyPoint), typeof(DDBrushPicker),
			new PropertyMetadata(null));
	
		public DDDependencyPoint LinearEnd
		{
			get { return (DDDependencyPoint)this.GetValue(LinearEndProperty); }
			set { this.SetValue(LinearEndProperty, value); }
		}
		#endregion

		#region RadialOrigin Property
		public static DependencyProperty RadialOriginProperty = DependencyProperty.Register(
			"RadialOrigin", typeof(DDDependencyPoint), typeof(DDBrushPicker),
			new PropertyMetadata(null));
	
		public DDDependencyPoint RadialOrigin
		{
			get { return (DDDependencyPoint)this.GetValue(RadialOriginProperty); }
			set { this.SetValue(RadialOriginProperty, value); }
		}
		#endregion

		#region RadialCenter Property
		public static DependencyProperty RadialCenterProperty = DependencyProperty.Register(
			"RadialCenter", typeof(DDDependencyPoint), typeof(DDBrushPicker),
			new PropertyMetadata(null));
	
		public DDDependencyPoint RadialCenter
		{
			get { return (DDDependencyPoint)this.GetValue(RadialCenterProperty); }
			set { this.SetValue(RadialCenterProperty, value); }
		}
		#endregion

		public List<DDColorComboItem> DefaultColors
		{
			get { return DDColorComboItem.SystemColors; }
		}

		public int SelectedTab
		{
			get { return xamlTabControl.SelectedIndex; }
			set
			{
				if (value != xamlTabControl.SelectedIndex)
				{
					xamlTabControl.SelectedIndex = value;
				}
			}
		}

		public DDBrushPicker()
		{
			InitializeComponent();

			xamlSolidColorCombo.ItemsSource = DefaultColors;
			xamlSolidColorCombo.Text = "";

			LinearStart = new DDDependencyPoint();
			LinearEnd = new DDDependencyPoint();
			RadialOrigin = new DDDependencyPoint();
			RadialCenter = new DDDependencyPoint();

			SolidColorBrush = new SolidColorBrush(Colors.Transparent);
			LinearGradientBrush = new LinearGradientBrush(Colors.White, Colors.Black, 0);
			RadialGradientBrush = new RadialGradientBrush(Colors.White, Colors.Black);
			
			//BindBrushes();
		}

		protected void BindBrushes()
		{	
			if ((LinearGradientBrush != null) && (!LinearGradientBrush.IsFrozen))
			{
				BindingOperations.SetBinding(LinearStart, DDDependencyPoint.PointProperty,
					new Binding("StartPoint") { Source = LinearGradientBrush, Mode = BindingMode.TwoWay });
				BindingOperations.SetBinding(LinearEnd, DDDependencyPoint.PointProperty,
					new Binding("EndPoint") { Source = LinearGradientBrush, Mode = BindingMode.TwoWay });
			}
			
			if ((RadialGradientBrush != null) && (!RadialGradientBrush.IsFrozen))
			{
				BindingOperations.SetBinding(RadialOrigin, DDDependencyPoint.PointProperty,
					new Binding("GradientOrigin") { Source = RadialGradientBrush, Mode = BindingMode.TwoWay });
				BindingOperations.SetBinding(RadialCenter, DDDependencyPoint.PointProperty,
					new Binding("Center") { Source = RadialGradientBrush, Mode = BindingMode.TwoWay });
			}
		}

		protected void UpdatePickedBrush()
		{
			switch(SelectedTab)
			{
				case 0:
					PickedBrush = null;
					break;
				case 1:
					PickedBrush = this.SolidColorBrush;
					break;
				case 2:
					PickedBrush = this.LinearGradientBrush;
					break;
				case 3:
					PickedBrush = this.RadialGradientBrush;
					break;
			}
		}

		protected void RecreateBrushes()
		{
			this.SolidColorBrush = this.SolidColorBrush.Clone();
			this.LinearGradientBrush = this.LinearGradientBrush.Clone();
			this.RadialGradientBrush = this.RadialGradientBrush.Clone();
			
			BindBrushes();
		}
		
		private void xamlTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.OriginalSource != xamlTabControl)
				return;

			if (!ExternalLock)
			{
				RecreateBrushes();
				UpdatePickedBrush();
			}
		}

		#region INotifyPropertyChanged Members

		protected void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		private void RemoveStopButton_Click(object sender, RoutedEventArgs e)
		{
			GradientStop stop = (sender as Button).Tag as GradientStop;
			if (PickedBrush == this.LinearGradientBrush)
			{
				this.LinearGradientBrush.GradientStops.Remove(stop);
				xamlLinearGradientStops.SelectedIndex = -1;
				xamlLinearGradientStops.Items.Refresh();
			}
			else if (PickedBrush == this.RadialGradientBrush)
			{
				this.RadialGradientBrush.GradientStops.Remove(stop);
				xamlRadialGradientStops.SelectedIndex = -1;
				xamlRadialGradientStops.Items.Refresh();
			}
		}

		private void AddStopButton_Click(object sender, RoutedEventArgs e)
		{
			if (PickedBrush == this.LinearGradientBrush)
			{
				this.LinearGradientBrush.GradientStops.Add(new GradientStop(Colors.Black, 1));
				xamlLinearGradientStops.SelectedIndex = -1;
				xamlLinearGradientStops.Items.Refresh();
			}
			else if (PickedBrush == this.RadialGradientBrush)
			{
				this.RadialGradientBrush.GradientStops.Add(new GradientStop(Colors.Black, 1));
				xamlRadialGradientStops.SelectedIndex = -1;
				xamlRadialGradientStops.Items.Refresh();
			}
		}

		// An uglyish hack to prevent click-through effect when selecting list-box items
		// TODO: A better solution?

		private void ClickThroughGuardedElement_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			(sender as FrameworkElement).Tag = 1;
		}

		private void ClickThroughGuardedElement_Loaded(object sender, RoutedEventArgs e)
		{
			(sender as FrameworkElement).Tag = 0;
		}

		private void ClickThroughGuardedElement_PreviewMouseMove(object sender, MouseEventArgs e)
		{
			if (((sender as FrameworkElement).Tag == null) || ((int)(sender as FrameworkElement).Tag == 0))
				e.Handled = true;
		}
	}

	// Standard converter to test bindings
	// Not used ATM

	public class DDTestConverter : IValueConverter
	{
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			Console.WriteLine("     Convert: " + value.ToString());
			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			Console.WriteLine("Convert back: " + value.ToString());
			return value;
		}

		#endregion
	}

	[ValueConversion(typeof(Color), typeof(SolidColorBrush))]
	public class DDColorToSolidColorBrushConverter : IValueConverter
	{
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			SolidColorBrush ret;
			if (value is Color)
				ret = new SolidColorBrush((Color)value);
			else 
				ret = new SolidColorBrush();
			ret.Freeze();
			return ret;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is SolidColorBrush)
				return ((SolidColorBrush)value).Color;
			else
				return Colors.Transparent;
		}

		#endregion
	}

	public struct DDColorComboItem
	{
		public static List<DDColorComboItem> SystemColors
		{
			get
			{
				List<DDColorComboItem> list = new List<DDColorComboItem>();
				foreach (PropertyInfo pi in typeof(Colors).GetProperties(BindingFlags.Public | BindingFlags.Static))
					list.Add(new DDColorComboItem(pi.Name, (Color)ColorConverter.ConvertFromString(pi.Name)));
				return list;
			}
		}

		private Color _color;
		public Color Color
		{
			get { return _color; }
			set { _color = value; }
		}
		
		private string _name;
		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}

		public DDColorComboItem(string name, Color color)
		{
			_name = name;
			_color = color;
		}

		public override string ToString()
		{
			return Name;
		}

		public static string GetSystemName(Color color)
		{
			List<DDColorComboItem> list = SystemColors;
			foreach (DDColorComboItem item in list)
				if (item.Color == color)
					return item.Name;
			return color.ToString();
		}
	}

	public class DDDependencyPoint : DependencyObject
	{
		protected bool InternalUpdate = false;
		
		#region Point Property
		public static DependencyProperty PointProperty = DependencyProperty.Register(
			"Point", typeof(Point), typeof(DDDependencyPoint),
			new PropertyMetadata(new Point(), PointPropertyChangedCallback));

		private static void PointPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs args)
		{
			DDDependencyPoint sender = (DDDependencyPoint)d;
			Point newValue = (Point)args.NewValue;

			if (sender.InternalUpdate)
				return;

			sender.InternalUpdate = true;
			sender.X = newValue.X;
			sender.Y = newValue.Y;
			sender.InternalUpdate = false;
		}

		public Point Point
		{
			get { return (Point)this.GetValue(PointProperty); }
			set { this.SetValue(PointProperty, value); }
		}
		#endregion

		#region X Property
		public static DependencyProperty XProperty = DependencyProperty.Register(
			"X", typeof(double), typeof(DDDependencyPoint),
			new PropertyMetadata(0d, CoordPropertyChangedCallback));
	
		public double X
		{
			get { return (double)this.GetValue(XProperty); }
			set { this.SetValue(XProperty, value); }
		}
		#endregion

		#region Y Property
		public static DependencyProperty YProperty = DependencyProperty.Register(
			"Y", typeof(double), typeof(DDDependencyPoint),
			new PropertyMetadata(0d, CoordPropertyChangedCallback));
	
		public double Y
		{
			get { return (double)this.GetValue(YProperty); }
			set { this.SetValue(YProperty, value); }
		}
		#endregion

		private static void CoordPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs args)
		{
			DDDependencyPoint sender = (DDDependencyPoint)d;

			if (sender.InternalUpdate)
				return;

			sender.InternalUpdate = true;
			sender.Point = new Point(sender.X, sender.Y);
			sender.InternalUpdate = false;
		}
	}

	[ValueConversion(typeof(Color), typeof(string))]
	public class DDColorToStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is Color)
				return DDColorComboItem.GetSystemName((Color)value);
			else
				return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is string)
			{
				try
				{
					return ColorConverter.ConvertFromString(value as string);
				}
				catch (FormatException)
				{
					return null;
				}
			}
			else
				return null;
		}
	}

	[ValueConversion(typeof(double), typeof(Point))]
	public class DDTwoDoublesToPointConverter : IMultiValueConverter
	{
		#region IMultiValueConverter Members

		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if ((values != null) && (values.Length == 2))
				return new Point((double)values[0], (double)values[1]);
			else
				return new Point();
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is Point)
				return new object[] { ((Point)value).X, ((Point)value).Y };
			else
				return new object[] { 0, 0 };
		}

		#endregion
	}

	[ValueConversion(typeof(double), typeof(Point))]
	public class DDXToPointConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if ((value is double) && (parameter is double))
				return new Point((double)value, (double)parameter);
			else
				return new Point();
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is Point)
				return ((Point)value).X;
			else
				return 0;
		}
	}

	[ValueConversion(typeof(double), typeof(Point))]
	public class DDYToPointConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if ((value is double) && (parameter is double))
				return new Point((double)parameter, (double)value);
			else
				return new Point();
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is Point)
				return ((Point)value).Y;
			else
				return 0;
		}
	}
}
