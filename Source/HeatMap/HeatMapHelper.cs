using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;

namespace HeatMap
{
	[StaticConstructorOnStartup]
	public static class HeatMapHelper
	{
		internal static IntRange MappedTemperatureRange {  get; private set; }
		internal static Color[] MappedColors { get; private set; }
		internal static Texture2D[] WidgetTextures { get; set; }

		public static void RegenerateColorMap()
		{
			if (HeatMap.Settings == null)
				return;
			if (HeatMap.Settings.UseCustomRange)
				CreateCustomMap();
			else
				CreateComfortMap();
		}

		private static void CreateCustomMap()
		{
			var minComfortTemp = HeatMap.Settings.CustomRangeComfortMin;
			var maxComfortTemp = HeatMap.Settings.CustomRangeComfortMax;

			MappedTemperatureRange = new IntRange(HeatMap.Settings.CustomRangeMin, HeatMap.Settings.CustomRangeMax);
			MappedColors = CreateColorGradient(MappedTemperatureRange, minComfortTemp, maxComfortTemp);
			WidgetTextures = null;
		}

		private static void CreateComfortMap()
		{
			(var mappedRange, var minComfortTemp, var maxComfortTemp) = GetComfortTemperatureRanges();

			MappedTemperatureRange = mappedRange;
			MappedColors = CreateColorGradient(MappedTemperatureRange, minComfortTemp, maxComfortTemp);
			WidgetTextures = null;
		}

		internal static void CreateWidgetTextures()
		{
			// stuff to get pixels from display border texture (it's readonly, no idea how to do it otherwise)
			var displayBorder = Resources.DisplayBoder;
			var temporary = RenderTexture.GetTemporary(displayBorder.width, displayBorder.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
			Graphics.Blit(displayBorder, temporary);

			var previous = RenderTexture.active;
			RenderTexture.active = temporary;

			var foreground = new Texture2D(displayBorder.width, displayBorder.height);
			foreground.ReadPixels(new Rect(0, 0, temporary.width, temporary.height), 0, 0);
			foreground.Apply();

			RenderTexture.active = previous;
			RenderTexture.ReleaseTemporary(temporary);

			// create textures per temperature
			var textures = new Texture2D[MappedColors.Length];
			var opacity = HeatMap.Settings.OutdoorThermometerOpacity * 0.01f;
			for (int i = 0; i < textures.Length; i++)
			{
				var color = MappedColors[i];
				color.a = opacity;

				var texture = new Texture2D(foreground.width, foreground.height);
				for (int x = 0; x < texture.width; x++)
				{
					for (int y = 0; y < texture.height; y++)
					{
						var pixel = foreground.GetPixel(x, y);
						texture.SetPixel(x, y, Color.Lerp(color, pixel, pixel.a));
					}
				}
				texture.Apply();
				textures[i] = texture;
			}
			WidgetTextures = textures;
		}

		private static Color[] CreateColorGradient(IntRange range, int minComfortTemp, int maxComfortTemp)
		{
			var mappedColorCount = MappedTemperatureRange.max - MappedTemperatureRange.min;

			var gradientColors = new List<Tuple<Color, float>>();
			for (int i = 0; i < HeatMapSettings.GradientSteps; i++)
				gradientColors.Add(new Tuple<Color, float>(HeatMap.Settings.GetGradientColor(i), 1f));

			var gradientSteps = new List<float>();
			for (int i = 0, t = range.min; i < mappedColorCount; i++, t++)
				gradientSteps.Add((t >= minComfortTemp - 1 && t <= maxComfortTemp) ? 4f : 1f);

			return ColorHelper.Gradient(gradientColors, gradientSteps).ToArray();
		}

		public static (IntRange mappedRange, int comfortTempMin, int comfortTempMax) GetComfortTemperatureRanges()
		{
			var human = ThingDefOf.Human;
			var minComfortTemp = (int)human.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin) + 3;
			var maxComfortTemp = (int)human.GetStatValueAbstract(StatDefOf.ComfyTemperatureMax) - 3;

			var comfortDoubleRange = (maxComfortTemp - minComfortTemp) * 2;
			var mappedTemperatureRange = new IntRange(minComfortTemp - comfortDoubleRange, maxComfortTemp + comfortDoubleRange);

			return (mappedTemperatureRange, minComfortTemp, maxComfortTemp);
		}

		public static int GetIndexForTemperature(float temperature)
		{
			var colorMapIndex = (int)temperature - MappedTemperatureRange.min;
			if (colorMapIndex < 0)
				colorMapIndex = 0;
			else if (colorMapIndex >= MappedColors.Length)
				colorMapIndex = MappedColors.Length - 1;
			return colorMapIndex;
		}
		public static Color GetColorForTemperature(float temperature)
		{
			return MappedColors[GetIndexForTemperature(temperature)];
		}
	}
}
