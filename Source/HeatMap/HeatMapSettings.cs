using RimWorld;
using RimWorld.Planet;
using SyControlsBuilder;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace HeatMap
{
	public class HeatMapSettings : ModSettings
	{
		#region CONSTANTS
		public const int GradientSteps = 5;

		public const bool Default_OverrideVanillaOverlay = true;
		public const bool Default_ShowIndoorsOnly = true;
		public const int Default_OverlayOpacity = 30;
		public const int Default_UpdateDelay = 100;

		public const bool Default_ShowOutdoorThermometer = true;
		public const int Default_OutdoorThermometerOpacity = 30;
		public const bool Default_OutdoorThermometerFixed = true;
		public const float Default_OutdoorThermometerRight = 8f + HeatMap.BoxSize;
		public const float Default_OutdoorThermometerTop = 8f;

		public const bool Default_ShowTemperatureOverRooms = true;

		public const bool Default_UseCustomRange = false;
		#endregion

		#region PROPERTIES
		private bool _overrideVanillaOverlay = Default_OverrideVanillaOverlay;
		public bool OverrideVanillaOverlay
		{
			get => _overrideVanillaOverlay;
			set => Util.SetValue(ref _overrideVanillaOverlay, value, v => HeatMap.Instance.ResetAll());
		}
		private bool _showIndoorsOnly = Default_ShowIndoorsOnly;
		public bool ShowIndoorsOnly
		{
			get => _showIndoorsOnly;
			set => Util.SetValue(ref _showIndoorsOnly, value, v => HeatMap.Instance.ResetAll());
		}
		private int _overlayOpacity = Default_OverlayOpacity;
		public int OverlayOpacity
		{
			get => _overlayOpacity;
			set => Util.SetValue(ref _overlayOpacity, value, v => HeatMap.Instance.ResetAll());
		}
		public int UpdateDelay { get; set; } = Default_UpdateDelay;

		public bool ShowOutdoorThermometer { get; set; } = Default_ShowOutdoorThermometer;
		private int _outdoorThermometerOpacity = Default_OutdoorThermometerOpacity;
		public int OutdoorThermometerOpacity
		{
			get => _outdoorThermometerOpacity;
			set => Util.SetValue(ref _outdoorThermometerOpacity, value, v => HeatMapHelper.WidgetTextures = null);
		}
		public bool OutdoorThermometerFixed { get; set; } = Default_OutdoorThermometerFixed;
		public float OutdoorThermometerRight { get; set; } = Default_OutdoorThermometerRight;
		public float OutdoorThermometerTop { get; set; } = Default_OutdoorThermometerTop;

		public bool ShowTemperatureOverRooms { get; set; } = Default_ShowTemperatureOverRooms;

		public ValueSetting<float>[] GradientHue { get; } = new ValueSetting<float>[GradientSteps];

		public bool UseCustomRange { get; set; }
		private int _customRangeMin = Default_CustomRangeMin;
		public int CustomRangeMin
		{
			get => _customRangeMin;
			set => Util.SetValue(ref _customRangeMin, value, v =>
			{
				if (_customRangeMax <= v)
					_customRangeMax = v + 1;
				HeatMap.Instance.ResetAll();
			});
		}
		private int _customRangeMax = Default_CustomRangeMax;
		public int CustomRangeMax
		{
			get => _customRangeMax;
			set => Util.SetValue(ref _customRangeMax, value, v =>
			{
				if (_customRangeMin >= v)
					_customRangeMin = v - 1;
				HeatMap.Instance.ResetAll();
			});
		}
		private int _customRangeComfortMin = Default_CustomRangeComfortMin;
		public int CustomRangeComfortMin
		{
			get => _customRangeComfortMin;
			set => Util.SetValue(ref _customRangeComfortMin, value, v =>
			{
				if (_customRangeComfortMax <= v)
					_customRangeComfortMax = v + 1;
				HeatMap.Instance.ResetAll();
			});
		}
		private int _customRangeComfortMax = Default_CustomRangeComfortMax;
		public int CustomRangeComfortMax
		{
			get => _customRangeComfortMax;
			set => Util.SetValue(ref _customRangeComfortMax, value, v =>
			{
				if (_customRangeComfortMin >= v)
					_customRangeComfortMin = v - 1;
				HeatMap.Instance.ResetAll();
			});
		}
		#endregion

		#region FIELDS
		public static readonly IntRange MappedRange;
		public static readonly int MinComfortTemp;
		public static readonly int MaxComfortTemp;

		public static readonly int Default_CustomRangeMin;
		public static readonly int Default_CustomRangeMax;
		public static readonly int Default_CustomRangeComfortMin;
		public static readonly int Default_CustomRangeComfortMax;
		#endregion

		#region CONSTRUCTORS
		static HeatMapSettings()
		{
			(MappedRange, MinComfortTemp, MaxComfortTemp) = HeatMapHelper.GetComfortTemperatureRanges();

			Default_CustomRangeMin = MappedRange.min;
			Default_CustomRangeMax = MappedRange.max;
			Default_CustomRangeComfortMin = MinComfortTemp;
			Default_CustomRangeComfortMax = MaxComfortTemp;
		}

		public HeatMapSettings()
		{
			// 0° = red, 60° = yellow, 120° = green, 180° = cyan, 240° = blue, 300° = magenta
			//  standard begins at blue (low) and ends at red (high)
			for (int i = 0; i < GradientSteps; i++)
			{
				var defaultHue = 240f - 60f * i;
				GradientHue[i] = new ValueSetting<float>(nameof(GradientHue) + "_" + i, "", "", defaultHue, defaultHue, v => HeatMap.Instance.ResetAll());
			}
		}
		#endregion

		#region PUBLIC METHODS
		public void DoSettingsWindowContents(Rect inRect)
		{
			var offsetY = 0.0f;
			var width = ControlsBuilder.Begin(inRect);
			try
			{
				OverrideVanillaOverlay = ControlsBuilder.CreateCheckbox(
					ref offsetY,
					width,
					"FALCHM.OverrideVanillaOverlay".Translate(),
					"FALCHM.OverrideVanillaOverlayDesc".Translate(new NamedArgument(Default_OverrideVanillaOverlay, "default")),
					OverrideVanillaOverlay,
					Default_OverrideVanillaOverlay);
				ShowIndoorsOnly = ControlsBuilder.CreateCheckbox(
					ref offsetY,
					width,
					"FALCHM.ShowIndoorsOnly".Translate(),
					"FALCHM.ShowIndoorsOnlyDesc".Translate(new NamedArgument(Default_ShowIndoorsOnly, "default")),
					ShowIndoorsOnly,
					Default_ShowIndoorsOnly);
				OverlayOpacity = ControlsBuilder.CreateNumeric(
					ref offsetY,
					width,
					"FALCHM.OverlayOpacity".Translate(),
					"FALCHM.OverlayOpacityDesc".Translate(new NamedArgument(Default_OverlayOpacity, "default")),
					OverlayOpacity,
					Default_OverlayOpacity,
					nameof(OverlayOpacity),
					0,
					100,
					unit: "%");
				UpdateDelay = ControlsBuilder.CreateNumeric(
					ref offsetY,
					width,
					"FALCHM.UpdateDelay".Translate(),
					"FALCHM.UpdateDelayDesc".Translate(new NamedArgument(Default_UpdateDelay, "default")),
					UpdateDelay,
					Default_UpdateDelay,
					nameof(UpdateDelay),
					1,
					10000,
					unit: "ticks");
				ShowTemperatureOverRooms = ControlsBuilder.CreateCheckbox(
					ref offsetY,
					width,
					"FALCHM.ShowTemperatureOverRooms".Translate(),
					"FALCHM.ShowTemperatureOverRoomsDesc".Translate(new NamedArgument(Default_ShowTemperatureOverRooms, "default")),
					ShowTemperatureOverRooms,
					Default_ShowTemperatureOverRooms);

				offsetY += ControlsBuilder.SettingsRowMargin;

				ShowOutdoorThermometer = ControlsBuilder.CreateCheckbox(
					ref offsetY,
					width,
					"FALCHM.ShowOutDoorThermometer".Translate(),
					"FALCHM.ShowOutDoorThermometerDesc".Translate(new NamedArgument(Default_ShowOutdoorThermometer, "default")),
					ShowOutdoorThermometer,
					Default_ShowOutdoorThermometer);
				OutdoorThermometerOpacity = ControlsBuilder.CreateNumeric(
					ref offsetY,
					width,
					"FALCHM.ThermometerOpacity".Translate(),
					"FALCHM.ThermometerOpacityDesc".Translate(new NamedArgument(Default_OutdoorThermometerOpacity, "default")),
					OutdoorThermometerOpacity,
					Default_OutdoorThermometerOpacity,
					nameof(OutdoorThermometerOpacity),
					1,
					100,
					unit: "%");
				OutdoorThermometerFixed = ControlsBuilder.CreateCheckbox(
					ref offsetY,
					width,
					"FALCHM.ThermometerFixed".Translate(),
					"FALCHM.ThermometerFixedDesc".Translate(new NamedArgument(Default_OutdoorThermometerFixed, "default")),
					OutdoorThermometerFixed,
					Default_OutdoorThermometerFixed);
				OutdoorThermometerRight = ControlsBuilder.CreateNumeric(
					ref offsetY,
					width,
					"FALCHM.ThermometerRight".Translate(),
					"FALCHM.ThermometerRightDesc".Translate(new NamedArgument(Default_OutdoorThermometerRight, "default")),
					OutdoorThermometerRight,
					Default_OutdoorThermometerRight,
					nameof(OutdoorThermometerRight),
					unit: "px");
				OutdoorThermometerTop = ControlsBuilder.CreateNumeric(
					ref offsetY,
					width,
					"FALCHM.ThermometerTop".Translate(),
					"FALCHM.ThermometerTopDesc".Translate(new NamedArgument(Default_OutdoorThermometerTop, "default")),
					OutdoorThermometerTop,
					Default_OutdoorThermometerTop,
					nameof(OutdoorThermometerTop),
					unit: "px");

				offsetY += ControlsBuilder.SettingsRowMargin;

				ControlsBuilder.CreateMultiNumeric(
					ref offsetY,
					width,
					"FALCHM.GradientHue".Translate(),
					"FALCHM.GradientHueDesc".Translate(
						new NamedArgument(string.Join(", ", GradientHue.Select(v => v.DefaultValue)), "default"),
						new NamedArgument(MappedRange.min, "min"),
						new NamedArgument(MappedRange.max, "max"),
						new NamedArgument(MinComfortTemp, "comfortMin"),
						new NamedArgument(MaxComfortTemp, "comfortMax")),
					GradientHue,
					nameof(GradientHue),
					0f,
					360f,
					unit: "°");

				offsetY += ControlsBuilder.SettingsRowMargin;

				UseCustomRange = ControlsBuilder.CreateCheckbox(
					ref offsetY,
					width,
					"FALCHM.UseCustomeRange".Translate(),
					"FALCHM.UseCustomeRangeDesc".Translate(new NamedArgument(Default_UseCustomRange, "default")),
					UseCustomRange,
					Default_UseCustomRange);
				if (UseCustomRange)
				{
					var min = -273;
					var max = 1000;
					CustomRangeMin = ControlsBuilder.CreateNumeric(
						ref offsetY,
						width,
						"FALCHM.CustomRangeMin".Translate(),
						$"{"FALCHM.CustomRangeMinDesc".Translate(new NamedArgument(CelsiusTo(MappedRange.min), "default"))} ({Prefs.TemperatureMode.ToStringHuman()})",
						CustomRangeMin,
						Default_CustomRangeMin,
						nameof(CustomRangeMin),
						min,
						max,
						unit: displayTemp(CustomRangeMin));
					CustomRangeMax = ControlsBuilder.CreateNumeric(
						ref offsetY,
						width,
						"FALCHM.CustomRangeMax".Translate(),
						$"{"FALCHM.CustomRangeMaxDesc".Translate(new NamedArgument(CelsiusTo(MappedRange.max), "default"))} ({Prefs.TemperatureMode.ToStringHuman()})",
						CustomRangeMax,
						Default_CustomRangeMax,
						nameof(CustomRangeMax),
						min,
						max,
						unit: displayTemp(CustomRangeMax));
					CustomRangeComfortMin = ControlsBuilder.CreateNumeric(
						ref offsetY,
						width,
						"FALCHM.CustomRangeComfortMin".Translate(),
						$"{"FALCHM.CustomRangeComfortMinDesc".Translate(new NamedArgument(CelsiusTo(MinComfortTemp), "default"))} ({Prefs.TemperatureMode.ToStringHuman()})",
						CustomRangeComfortMin,
						Default_CustomRangeComfortMin,
						nameof(CustomRangeComfortMin),
						min,
						max,
						unit: displayTemp(CustomRangeComfortMin));
					CustomRangeComfortMax = ControlsBuilder.CreateNumeric(
						ref offsetY,
						width,
						"FALCHM.CustomRangeComfortMax".Translate(),
						$"{"FALCHM.CustomRangeComfortMaxDesc".Translate(new NamedArgument(CelsiusTo(MaxComfortTemp), "default"))} ({Prefs.TemperatureMode.ToStringHuman()})",
						CustomRangeComfortMax,
						Default_CustomRangeComfortMax,
						nameof(CustomRangeComfortMax),
						min,
						max,
						unit: displayTemp(CustomRangeComfortMax));

					string displayTemp(int v) =>
						$"{CelsiusTo(v)} {GetTemperatureUnitText()}";
				}
			}
			finally
			{
				ControlsBuilder.End(offsetY);
			}
		}

		public Color GetGradientColor(int index)
		{
			if (index >= 0 && index < GradientHue.Length)
				return Color.HSVToRGB(GradientHue[index].Value / 360f, 1f, 1f);
			return Color.black;
		}
		#endregion

		#region OVERRIDES
		public override void ExposeData()
		{
			base.ExposeData();

			bool boolValue;
			float floatValue;
			int intValue;

			boolValue = OverrideVanillaOverlay;
			Scribe_Values.Look(ref boolValue, nameof(OverrideVanillaOverlay), Default_OverrideVanillaOverlay);
			OverrideVanillaOverlay = boolValue;

			boolValue = ShowIndoorsOnly;
			Scribe_Values.Look(ref boolValue, nameof(ShowIndoorsOnly), Default_ShowIndoorsOnly);
			ShowIndoorsOnly = boolValue;

			intValue = OverlayOpacity;
			Scribe_Values.Look(ref intValue, nameof(OverlayOpacity), Default_OverlayOpacity);
			OverlayOpacity = intValue;

			intValue = UpdateDelay;
			Scribe_Values.Look(ref intValue, nameof(UpdateDelay), Default_UpdateDelay);
			UpdateDelay = intValue;

			boolValue = ShowOutdoorThermometer;
			Scribe_Values.Look(ref boolValue, nameof(ShowOutdoorThermometer), Default_ShowOutdoorThermometer);
			ShowOutdoorThermometer = boolValue;

			intValue = OutdoorThermometerOpacity;
			Scribe_Values.Look(ref intValue, nameof(OutdoorThermometerOpacity), Default_OutdoorThermometerOpacity);
			OutdoorThermometerOpacity = intValue;

			boolValue = OutdoorThermometerFixed;
			Scribe_Values.Look(ref boolValue, nameof(OutdoorThermometerFixed), Default_OutdoorThermometerFixed);
			OutdoorThermometerFixed = boolValue;

			floatValue = OutdoorThermometerRight;
			Scribe_Values.Look(ref floatValue, nameof(OutdoorThermometerRight), Default_OutdoorThermometerRight);
			OutdoorThermometerRight = floatValue;

			floatValue = OutdoorThermometerTop;
			Scribe_Values.Look(ref floatValue, nameof(OutdoorThermometerTop), Default_OutdoorThermometerTop);
			OutdoorThermometerTop = floatValue;

			boolValue = ShowTemperatureOverRooms;
			Scribe_Values.Look(ref boolValue, nameof(ShowTemperatureOverRooms), Default_ShowTemperatureOverRooms);
			ShowTemperatureOverRooms = boolValue;

			for (int i = 0; i < GradientSteps; i++)
			{
				var hue = GradientHue[i];
				floatValue = hue.Value;
				Scribe_Values.Look(ref floatValue, hue.Name, hue.DefaultValue);
				hue.Value = floatValue;
			}

			boolValue = UseCustomRange;
			Scribe_Values.Look(ref boolValue, nameof(UseCustomRange), Default_UseCustomRange);
			UseCustomRange = boolValue;

			intValue = CustomRangeMin;
			Scribe_Values.Look(ref intValue, nameof(CustomRangeMin), Default_CustomRangeMin);
			CustomRangeMin = intValue;

			intValue = CustomRangeMax;
			Scribe_Values.Look(ref intValue, nameof(CustomRangeMax), Default_CustomRangeMax);
			CustomRangeMax = intValue;

			intValue = CustomRangeComfortMin;
			Scribe_Values.Look(ref intValue, nameof(CustomRangeComfortMin), Default_CustomRangeComfortMin);
			CustomRangeComfortMin = intValue;

			intValue = CustomRangeComfortMax;
			Scribe_Values.Look(ref intValue, nameof(CustomRangeComfortMax), Default_CustomRangeComfortMax);
			CustomRangeComfortMax = intValue;
		}
		#endregion

		#region PRIVATE METHODS
		private static string GetTemperatureUnitText() =>
			GetTemperatureUnitText(Prefs.TemperatureMode);
		private static string GetTemperatureUnitText(TemperatureDisplayMode mode)
		{
			switch (mode)
			{
				case TemperatureDisplayMode.Celsius:
					return "°C";
				case TemperatureDisplayMode.Fahrenheit:
					return "°F";
				case TemperatureDisplayMode.Kelvin:
					return "K";
			}
			return "";
		}
		private static float CelsiusTo(float temp) =>
			GenTemperature.CelsiusTo(temp, Prefs.TemperatureMode);
		private static float ToCelsius(float temp) =>
			ToCelsius(temp, Prefs.TemperatureMode);
		private static float ToCelsius(float temp, TemperatureDisplayMode oldMode)
		{
			switch (oldMode)
			{
				case TemperatureDisplayMode.Celsius:
					return temp;
				case TemperatureDisplayMode.Kelvin:
					return temp - 273;
				case TemperatureDisplayMode.Fahrenheit:
					return (temp - 32) / 1.8f;
				default:
					throw new InvalidOperationException();
			}
		}
		#endregion
	}
}