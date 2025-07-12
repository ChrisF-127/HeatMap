using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
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
		private bool _draggingThermometer = false;
		private float _dragThermometerRight = 0f;
		private float _dragThermometerTop = 0f;

		private readonly Dictionary<int, Texture2D> _temperatureTextureCache = new Dictionary<int, Texture2D>();
		#endregion

		#region CONSTRUCTORS
		public HeatMap(ModContentPack content) : base(content)
		{
			Instance = this;

			LongEventHandler.ExecuteWhenFinished(Initialize);
		}
		#endregion

		#region PUBLIC METHODS
		public void OnGUI()
		{
			if (Current.ProgramState != ProgramState.Playing
				|| Find.CurrentMap == null
				|| WorldRendererUtility.WorldRendered)
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
			{
				if (WorldRendererUtility.WorldRendered)
					return;

				Find.PlaySettings.showTemperatureOverlay = !Find.PlaySettings.showTemperatureOverlay;
			}
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
				if (typeof(LearningReadout).GetField("windowRect", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(Find.Tutor.learningReadout) is Rect helpRect
					&& helpRect.Overlaps(outRect) == true)
					outRect.x = helpRect.x - BoxSize - 5f;
			}

			if (!Settings.OutdoorThermometerFixed && Event.current.isMouse)
			{
				switch (Event.current.type)
				{
					case EventType.MouseDown:
						if (Mouse.IsOver(outRect) && Event.current.modifiers == EventModifiers.Shift)
						{
							Event.current.Use();

							_dragThermometerRight = Settings.OutdoorThermometerRight;
							_dragThermometerTop = Settings.OutdoorThermometerTop;

							_draggingThermometer = true;
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
						if (_draggingThermometer)
						{
							Event.current.Use();

							Settings.OutdoorThermometerRight = _dragThermometerRight;
							Settings.OutdoorThermometerTop = _dragThermometerTop;

							_draggingThermometer = false;
						}
						break;
				}
			}

			var temperature = Find.CurrentMap.mapTemperature.OutdoorTemp;
			var textureIndex = HeatMapHelper.GetIndexForTemperature(temperature);
			if (!_temperatureTextureCache.ContainsKey(textureIndex))
			{
				var backColor = HeatMapHelper.GetColorForTemperature(temperature);
				backColor.a = Settings.OutdoorThermometerOpacity / 100f;
				_temperatureTextureCache[textureIndex] = SolidColorMaterials.NewSolidColorTexture(backColor);
			}
			GUI.DrawTexture(outRect, _temperatureTextureCache[textureIndex]);
			GUI.DrawTexture(outRect, Resources.DisplayBoder);

			var temperatureForDisplay = temperature.ToStringTemperature("F0");
			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.MiddleCenter;
			GUI.color = Color.white;
			Widgets.Label(outRect, temperatureForDisplay);

			if (Widgets.ButtonInvisible(outRect))
				Find.PlaySettings.showTemperatureOverlay = !Find.PlaySettings.showTemperatureOverlay;

			TooltipHandler.TipRegion(outRect, "FALCHM.ThermometerTooltip".Translate());

			Text.Anchor = TextAnchor.UpperLeft;
		}

		public void ResetAll()
		{
			HeatMapHelper.RegenerateColorMap();
			TemperatureDisplayer.Reset();
			ClearTemperatureTextureCache();

			Find.CurrentMap?.mapTemperature?.Drawer?.SetDirty();
		}

		public void ClearTemperatureTextureCache() => 
			_temperatureTextureCache.Clear();
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
		}
		#endregion
	}
}
