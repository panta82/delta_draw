using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Reflection;

namespace Pantas.DeltaDraw.Shaders.Grayscale
{
	public class DDGrayscaleEffect : ShaderEffect
	{	
		private readonly static PixelShader _pixelShader = new PixelShader()
		{
			UriSource = new Uri(@"pack://application:,,,/GrayscaleShaderEffect;component/GrayscaleShader.ps")
		};

		public DDGrayscaleEffect()
		{
			PixelShader = _pixelShader;
			this.DdxUvDdyUvRegisterIndex = 6;

			UpdateShaderValue(InputProperty);
		}

		public Brush Input
		{
			get { return (Brush)GetValue(InputProperty); }
			set { SetValue(InputProperty, value); }
		}

		public static readonly DependencyProperty InputProperty =
			ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(DDGrayscaleEffect), 0);

	}
}
