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
		public const bool Default_OutdoorThermometerFixed = false;
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
			set => Util.SetValue(ref _overrideVanillaOverlay, value, HeatMap.Instance.ResetAll);
		}
		private bool _showIndoorsOnly = Default_ShowIndoorsOnly;
		public bool ShowIndoorsOnly
		{
			get => _showIndoorsOnly;
			set => Util.SetValue(ref _showIndoorsOnly, value, HeatMap.Instance.ResetAll);
		}
		private int _overlayOpacity = Default_OverlayOpacity;
		public int OverlayOpacity
		{
			get => _overlayOpacity;
			set => Util.SetValue(ref _overlayOpacity, value, HeatMap.Instance.ResetAll);
		}
		public int UpdateDelay { get; set; } = Default_UpdateDelay;

		public bool ShowOutdoorThermometer { get; set; } = Default_ShowOutdoorThermometer;
		private int _outdoorThermometerOpacity = Default_OutdoorThermometerOpacity;
		public int OutdoorThermometerOpacity
		{
			get => _outdoorThermometerOpacity;
			set => Util.SetValue(ref _outdoorThermometerOpacity, value, HeatMap.Instance.ClearTemperatureTextureCache);
		}
		public bool OutdoorThermometerFixed { get; set; } = Default_OutdoorThermometerFixed;
		public float OutdoorThermometerRight { get; set; } = Default_OutdoorThermometerRight;
		public float OutdoorThermometerTop { get; set; } = Default_OutdoorThermometerTop;

		public bool ShowTemperatureOverRooms { get; set; } = Default_ShowTemperatureOverRooms;

		public ValueActionWrapper<float>[] GradientHue { get; set; } = Default_GradientHue.Select(v => new ValueActionWrapper<float>(v, HeatMap.Instance.ResetAll)).ToArray();

		public bool UseCustomRange { get; set; }
		private int _customRangeMin = Default_CustomRangeMin;
		public int CustomRangeMin
		{
			get => _customRangeMin;
			set => Util.SetValue(ref _customRangeMin, value, () =>
			{
				if (_customRangeMax <= value)
					_customRangeMax = value + 1;
				HeatMap.Instance.ResetAll();
			});
		}
		private int _customRangeMax = Default_CustomRangeMax;
		public int CustomRangeMax
		{
			get => _customRangeMax;
			set => Util.SetValue(ref _customRangeMax, value, () =>
			{
				if (_customRangeMin >= value)
					_customRangeMin = value - 1;
				HeatMap.Instance.ResetAll();
			});
		}
		private int _customRangeComfortMin = Default_CustomRangeComfortMin;
		public int CustomRangeComfortMin
		{
			get => _customRangeComfortMin;
			set => Util.SetValue(ref _customRangeComfortMin, value, HeatMap.Instance.ResetAll);
		}
		private int _customRangeComfortMax = Default_CustomRangeComfortMax;
		public int CustomRangeComfortMax
		{
			get => _customRangeComfortMax;
			set => Util.SetValue(ref _customRangeComfortMax, value, HeatMap.Instance.ResetAll);
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

		public static readonly float[] Default_GradientHue = new float[GradientSteps];
		#endregion

		#region CONSTRUCTORS
		static HeatMapSettings()
		{
			(MappedRange, MinComfortTemp, MaxComfortTemp) = HeatMapHelper.GetComfortTemperatureRanges();

			Default_CustomRangeMin = MappedRange.min;
			Default_CustomRangeMax = MappedRange.max;
			Default_CustomRangeComfortMin = MinComfortTemp;
			Default_CustomRangeComfortMax = MaxComfortTemp;

			// 0° = red, 60° = yellow, 120° = green, 180° = cyan, 240° = blue, 300° = magenta
			//  standard begins at blue (low) and ends at red (high)
			for (int i = 0; i < GradientSteps; i++)
				Default_GradientHue[i] = 240f - 60f * i;
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
					9999);

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
					100);
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
					nameof(OutdoorThermometerRight));
				OutdoorThermometerTop = ControlsBuilder.CreateNumeric(
					ref offsetY,
					width,
					"FALCHM.ThermometerTop".Translate(),
					"FALCHM.ThermometerTopDesc".Translate(new NamedArgument(Default_OutdoorThermometerTop, "default")),
					OutdoorThermometerTop,
					Default_OutdoorThermometerTop,
					nameof(OutdoorThermometerTop));

				ShowTemperatureOverRooms = ControlsBuilder.CreateCheckbox(
					ref offsetY,
					width,
					"FALCHM.ShowTemperatureOverRooms".Translate(),
					"FALCHM.ShowTemperatureOverRoomsDesc".Translate(new NamedArgument(Default_ShowTemperatureOverRooms, "default")),
					ShowTemperatureOverRooms,
					Default_ShowTemperatureOverRooms);

				for (int i = 0; i < GradientSteps; i++)
				{
					GradientHue[i].Value = ControlsBuilder.CreateNumeric(
						ref offsetY,
						width,
						"FALCHM.GradientHue".Translate(new NamedArgument(i, "index")),
						"FALCHM.GradientHueDesc".Translate(
							new NamedArgument(Default_GradientHue[i], "default"),
							new NamedArgument(MappedRange.min, "min"),
							new NamedArgument(MappedRange.max, "max"),
							new NamedArgument(MinComfortTemp, "comfortMin"),
							new NamedArgument(MaxComfortTemp, "comfortMax")),
						GradientHue[i].Value,
						Default_GradientHue[i],
						nameof(GradientHue) + "_" + i,
						0f,
						360f);
				}

				UseCustomRange = ControlsBuilder.CreateCheckbox(
					ref offsetY,
					width,
					"FALCHM.UseCustomeRange".Translate(),
					"FALCHM.UseCustomeRangeDesc".Translate(new NamedArgument(Default_UseCustomRange, "default")),
					UseCustomRange,
					Default_UseCustomRange);
				if (UseCustomRange)
				{
					var min = (int)GenTemperature.CelsiusTo(-273f, Prefs.TemperatureMode);
					var max = (int)GenTemperature.CelsiusTo(1000f, Prefs.TemperatureMode);
					CustomRangeMin = ControlsBuilder.CreateNumeric(
						ref offsetY,
						width,
						"FALCHM.CustomRangeMin".Translate(),
						$"{"FALCHM.CustomRangeMinDesc".Translate(new NamedArgument(MappedRange.min, "default"))} ({Prefs.TemperatureMode.ToStringHuman()})",
						CustomRangeMin,
						Default_CustomRangeMin,
						nameof(CustomRangeMin),
						min,
						max);
					CustomRangeMax = ControlsBuilder.CreateNumeric(
						ref offsetY,
						width,
						"FALCHM.CustomRangeMax".Translate(),
						$"{"FALCHM.CustomRangeMaxDesc".Translate(new NamedArgument(MappedRange.max, "default"))} ({Prefs.TemperatureMode.ToStringHuman()})",
						CustomRangeMax,
						Default_CustomRangeMax,
						nameof(CustomRangeMax),
						min,
						max);
					CustomRangeComfortMin = ControlsBuilder.CreateNumeric(
						ref offsetY,
						width,
						"FALCHM.CustomRangeComfortMin".Translate(),
						$"{"FALCHM.CustomRangeComfortMinDesc".Translate(new NamedArgument(MinComfortTemp, "default"))} ({Prefs.TemperatureMode.ToStringHuman()})",
						CustomRangeComfortMin,
						Default_CustomRangeComfortMin,
						nameof(CustomRangeComfortMin),
						min,
						max);
					CustomRangeComfortMax = ControlsBuilder.CreateNumeric(
						ref offsetY,
						width,
						"FALCHM.CustomRangeComfortMax".Translate(),
						$"{"FALCHM.CustomRangeComfortMaxDesc".Translate(new NamedArgument(MaxComfortTemp, "default"))} ({Prefs.TemperatureMode.ToStringHuman()})",
						CustomRangeComfortMax,
						Default_CustomRangeComfortMax,
						nameof(CustomRangeComfortMax),
						min,
						max);
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
				floatValue = GradientHue[i].Value;
				Scribe_Values.Look(ref floatValue, nameof(GradientHue) + "_" + i, Default_GradientHue[i]);
				GradientHue[i].Value = floatValue;
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
	}
}
