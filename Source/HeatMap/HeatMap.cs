using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;

namespace HeatMap
{
    public class HeatMap : Mod
	{
		#region CONSTANTS
		public const float BoxSize = 62f;
		#endregion

		#region PROPERTIES
		public static HeatMap Instance { get; private set; }
		public static HeatMapSettings Settings { get; private set; }

		public RoomTemperatureDisplayer TemperatureDisplayer { get; } = new RoomTemperatureDisplayer();
		#endregion

		#region FIELDS
		private bool _clickingThermometer = false;
		private bool _draggingThermometer = false;
		private float _dragThermometerRight = 0f;
		private float _dragThermometerTop = 0f;
		private string _thermometerTooltip;

		private static FieldInfo _learningReadout_windowRect;
		#endregion

		#region CONSTRUCTORS
		static HeatMap()
		{
			var harmony = new Harmony("falc.heatmap");
			harmony.PatchAll();
		}
		public HeatMap(ModContentPack content) : base(content)
		{
			_learningReadout_windowRect = AccessTools.Field(typeof(LearningReadout), "windowRect");
			if (_learningReadout_windowRect == null)
				Log.Error("LearningReadout.windowRect not found");

			Instance = this;

			LongEventHandler.ExecuteWhenFinished(Initialize);
		}
		#endregion

		#region PUBLIC METHODS
		public void OnGUI()
		{
			if (Current.ProgramState != ProgramState.Playing
				|| Find.CurrentMap == null
				|| !WorldRendererUtility.DrawingMap)
				return;

			UpdateOutdoorThermometer();

			if (Find.PlaySettings.showTemperatureOverlay && Settings.ShowTemperatureOverRooms)
			{
				TemperatureDisplayer.Update(Settings.UpdateDelay);
				TemperatureDisplayer.OnGUI();
			}

			if (Event.current.type != EventType.KeyDown || Event.current.keyCode == KeyCode.None)
				return;

			if (HeatMapKeyBingings.ToggleHeatMap.JustPressed)
				Find.PlaySettings.showTemperatureOverlay = !Find.PlaySettings.showTemperatureOverlay;
		}

		public void WorldLoaded()
		{
			ResetAll();
		}

		public void UpdateOutdoorThermometer()
		{
			if (!Settings.ShowOutdoorThermometer)
				return;

			var right = Mathf.Clamp(_draggingThermometer ? _dragThermometerRight : Settings.OutdoorThermometerRight, BoxSize, UI.screenWidth);
			var top = Mathf.Clamp(_draggingThermometer ? _dragThermometerTop : Settings.OutdoorThermometerTop, 0, UI.screenHeight - BoxSize);
			var outRect = new Rect(UI.screenWidth - right, top, BoxSize, BoxSize);
			if (TutorSystem.AdaptiveTrainingEnabled && Find.PlaySettings.showLearningHelper)
			{
				if (_learningReadout_windowRect?.GetValue(Find.Tutor.learningReadout) is Rect helpRect
					&& helpRect.Overlaps(outRect) == true)
					outRect.x = helpRect.x - BoxSize - 5f;
			}

			if (Event.current.isMouse)
			{
				switch (Event.current.type)
				{
					case EventType.MouseDown:
						if (Mouse.IsOver(outRect))
						{
							Event.current.Use();

							if (!Settings.OutdoorThermometerFixed && Event.current.modifiers == EventModifiers.Shift)
							{
								_dragThermometerRight = Settings.OutdoorThermometerRight;
								_dragThermometerTop = Settings.OutdoorThermometerTop;

								_draggingThermometer = true;
							}
							else
							{
								_clickingThermometer = true;
							}
						}
						break;
					case EventType.MouseDrag:
						if (_draggingThermometer)
						{
							Event.current.Use();

							_dragThermometerRight -= Event.current.delta.x;
							_dragThermometerRight = Mathf.Clamp(_dragThermometerRight, BoxSize, UI.screenWidth);
							_dragThermometerTop += Event.current.delta.y;
							_dragThermometerTop = Mathf.Clamp(_dragThermometerTop, 0, UI.screenHeight - BoxSize);
							//outRect = new Rect(outRect.x - _dragThermometerRight, outRect.y + _dragThermometerTop, _boxSize, _boxSize); // repositioning is processed on next update
						}
						break;
					case EventType.MouseUp:
						if (Mouse.IsOver(outRect))
						{
							if (_draggingThermometer)
							{
								Event.current.Use();

								Settings.OutdoorThermometerRight = _dragThermometerRight;
								Settings.OutdoorThermometerTop = _dragThermometerTop;

								_draggingThermometer = false;
							}
							else if (_clickingThermometer)
							{
								Event.current.Use();

								Find.PlaySettings.showTemperatureOverlay = !Find.PlaySettings.showTemperatureOverlay;
							}
						}
						_clickingThermometer = false;
						break;
				}
			}

			if (HeatMapHelper.WidgetTextures == null)
				HeatMapHelper.CreateWidgetTextures();

			var temperature = Find.CurrentMap.mapTemperature.OutdoorTemp;
			var textureIndex = HeatMapHelper.GetIndexForTemperature(temperature);
			GUI.DrawTexture(outRect, HeatMapHelper.WidgetTextures[textureIndex]);

			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.MiddleCenter;
			GUI.color = Color.white;
			Widgets.Label(outRect, temperature.ToStringTemperature("F0"));

			TooltipHandler.TipRegion(outRect, _thermometerTooltip);

			Text.Anchor = TextAnchor.UpperLeft;
		}

		public void ResetAll()
		{
			HeatMapHelper.RegenerateColorMap();
			TemperatureDisplayer.Reset();

			Find.CurrentMap?.mapTemperature?.Drawer?.SetDirty();
		}
		#endregion

		#region OVERRIDES
		public override string SettingsCategory() =>
			"Heat Map";

		public override void DoSettingsWindowContents(Rect inRect)
		{
			base.DoSettingsWindowContents(inRect);

			Settings.DoSettingsWindowContents(inRect);
		}
		#endregion

		#region PRIVATE METHODS
		private void Initialize()
		{
			Settings = GetSettings<HeatMapSettings>();

			_thermometerTooltip = "FALCHM.ThermometerTooltip".Translate();
		}
		#endregion
	}
}
