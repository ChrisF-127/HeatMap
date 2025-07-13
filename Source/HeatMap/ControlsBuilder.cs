using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace SyControlsBuilder
{
	public static class ControlsBuilder
	{
		#region DELEGATE
		public delegate string ConvertDelegate<T>(T value);
		#endregion

		#region CONSTANTS
		public const float SettingsRowHeight = 32f;
		public const float SettingsRowMargin = SettingsRowHeight * 0.25f;

		private const float SettingsScrollbarWidth = 16f;
		#endregion

		#region FIELDS
		private static readonly Dictionary<string, object> ValueBuffers = new Dictionary<string, object>();

		private static readonly Color ModifiedColor = Color.cyan;

		private static GameFont OriTextFont;
		private static TextAnchor OriTextAnchor;
		private static Color OriColor;

		private static float SettingsViewHeight = 0f;
		private static Vector2 SettingsScrollPosition = new Vector2();
		#endregion

		#region PUBLIC METHODS
		public static float Begin(Rect inRect)
		{
			OriTextFont = Text.Font;
			OriTextAnchor = Text.Anchor;
			OriColor = GUI.color;

			Text.Anchor = TextAnchor.MiddleLeft;

			var viewWidth = inRect.width - SettingsScrollbarWidth;

			GUI.BeginGroup(inRect);
			Widgets.BeginScrollView(
				new Rect(0, 0, inRect.width, inRect.height),
				ref SettingsScrollPosition,
				new Rect(0, 0, viewWidth, SettingsViewHeight));

			return viewWidth;
		}
		public static void End(float offsetY)
		{
			Widgets.EndScrollView();
			GUI.EndGroup();

			Text.Font = OriTextFont;
			Text.Anchor = OriTextAnchor;
			GUI.color = OriColor;

			SettingsViewHeight = offsetY + SettingsRowHeight;
		}

		public static void CreateText(
			ref float offsetY,
			float viewWidth,
			string text, 
			Color color, 
			TextAnchor textAnchor = TextAnchor.MiddleLeft, 
			GameFont font = GameFont.Small)
		{
			// remember previous style
			var prevColor = GUI.color;
			var prevTextAnchor = Text.Anchor;
			var prevFont = Text.Font;

			// set desired style
			GUI.color = color;
			Text.Anchor = textAnchor;
			Text.Font = font;

			// draw text
			Widgets.Label(new Rect(2, offsetY, viewWidth - 4, SettingsRowHeight), text);

			// reset to previous style
			Text.Font = prevFont;
			Text.Anchor = prevTextAnchor;
			GUI.color = prevColor;

			offsetY += SettingsRowHeight;
		}

		public static T CreateNumeric<T>(
			ref float offsetY,
			float viewWidth,
			string label,
			string tooltip,
			T value,
			T defaultValue,
			string valueBufferKey,
			float min = 0f,
			float max = 1e+9f,
			ConvertDelegate<T> additionalText = null,
			string unit = null)
			where T : struct, IComparable
		{
			var isModified = !value.Equals(defaultValue);
			var controlWidth = GetControlWidth(viewWidth);

			// Label
			if (isModified)
				GUI.color = ModifiedColor;
			Widgets.Label(new Rect(0, offsetY, controlWidth - 8, SettingsRowHeight), label);
			GUI.color = OriColor;

			// Setting
			var textFieldRect = new Rect(controlWidth + 2, offsetY + 6, controlWidth - 4, SettingsRowHeight - 12);
			var valueBuffer = GetValueBuffer(valueBufferKey, value); // required for typing decimal points etc.
			Widgets.TextFieldNumeric(textFieldRect, ref value, ref valueBuffer.Buffer, min, max);

			// Tooltip
			if (!string.IsNullOrWhiteSpace(tooltip))
				DrawTooltip(textFieldRect, tooltip);

			// Unit
			DrawTextFieldUnit(textFieldRect, unit);

			// Additional Text
			if (additionalText != null)
			{
				var additionalTextRect = textFieldRect;
				additionalTextRect.x += textFieldRect.width + 8;
				additionalTextRect.width -= 8;
				Widgets.Label(additionalTextRect, additionalText(value));
			}

			// Reset button
			if (isModified && DrawResetButton(offsetY, viewWidth, defaultValue.ToString()))
			{
				value = defaultValue;
				ValueBuffers.Remove(valueBufferKey);
			}

			offsetY += SettingsRowHeight;
			return value;
		}

		public static bool CreateCheckbox(
			ref float offsetY,
			float viewWidth,
			string label,
			string tooltip,
			bool value,
			bool defaultValue,
			string text = null)
		{
			var isModified = value != defaultValue;
			var controlWidth = GetControlWidth(viewWidth);

			// Label
			if (isModified)
				GUI.color = ModifiedColor;
			Widgets.Label(new Rect(0, offsetY, controlWidth, SettingsRowHeight), label);
			GUI.color = OriColor;

			// Setting
			var checkboxSize = SettingsRowHeight - 8;
			Widgets.Checkbox(controlWidth, offsetY + (SettingsRowHeight - checkboxSize) / 2, ref value, checkboxSize);
			DrawTooltip(new Rect(controlWidth, offsetY, checkboxSize, checkboxSize), tooltip);

			// Text
			if (text != null)
				Widgets.Label(new Rect(controlWidth + checkboxSize + 4, offsetY + 4, controlWidth - checkboxSize - 6, SettingsRowHeight - 8), text ?? "");

			// Reset button
			if (isModified && DrawResetButton(offsetY, viewWidth, defaultValue.ToString()))
				value = defaultValue;

			offsetY += SettingsRowHeight;
			return value;
		}

		public static T CreateDropdown<T>(
			ref float offsetY,
			float viewWidth,
			string label,
			string tooltip,
			TargetWrapper<T> valueWrapper,
			T defaultValue,
			IEnumerable<T> list,
			Func<T, string> itemToString)
			where T : struct, IComparable
		{
			var isModified = valueWrapper?.Value.Equals(defaultValue) != true;
			var controlWidth = GetControlWidth(viewWidth);

			// Label
			if (isModified)
				GUI.color = ModifiedColor;
			Widgets.Label(new Rect(0, offsetY, controlWidth - 8, SettingsRowHeight), label);
			GUI.color = OriColor;

			// Menu Generator
			IEnumerable<Widgets.DropdownMenuElement<T>> menuGenerator(TargetWrapper<T> vWrapper)
			{
				foreach (var item in list)
				{
					yield return new Widgets.DropdownMenuElement<T>
					{
						option = new FloatMenuOption(itemToString(item), () => vWrapper.Value = item),
						payload = item,
					};
				}
			}

			// Dropdown
			var rect = new Rect(controlWidth + 2, offsetY + 2, controlWidth - 4, SettingsRowHeight - 4);
			Widgets.Dropdown(
				rect,
				valueWrapper,
				null,
				menuGenerator,
				itemToString(valueWrapper.Value));
			DrawTooltip(rect, tooltip);

			// Reset
			if (isModified && DrawResetButton(offsetY, viewWidth, itemToString(defaultValue)))
				valueWrapper.Value = defaultValue;

			offsetY += SettingsRowHeight;
			return valueWrapper.Value;
		}


		public static bool DrawResetButton(float offsetY, float viewWidth, string tooltip)
		{
			var buttonRect = new Rect(viewWidth / 3 * 2 + 4, offsetY + 2, SettingsRowHeight * 2 - 4, SettingsRowHeight - 4);
			DrawTooltip(buttonRect, "Reset to " + tooltip);
			return Widgets.ButtonText(buttonRect, "Reset");
		}
		public static void DrawTooltip(Rect rect, string tooltip)
		{
			if (Mouse.IsOver(rect))
			{
				ActiveTip activeTip = new ActiveTip(tooltip);
				activeTip.DrawTooltip(GenUI.GetMouseAttachedWindowPos(activeTip.TipRect.width, activeTip.TipRect.height) + (UI.MousePositionOnUIInverted - Event.current.mousePosition));
			}
		}
		public static void DrawTextFieldUnit(Rect rect, string text)
		{
			Text.Anchor = TextAnchor.MiddleRight;
			Widgets.Label(new Rect(rect.x + 4, rect.y + 1, rect.width - 8, rect.height), text);
			Text.Anchor = TextAnchor.MiddleLeft;
		}

		public static float GetControlWidth(float viewWidth) =>
			viewWidth / 3 - 4;

		public static void ResetValueBuffers() => 
			ValueBuffers.Clear();
		#endregion

		#region PRIVATE METHODS
		private static ValueBuffer<T> GetValueBuffer<T>(string key, T value)
			where T : struct, IComparable
		{
			// find value buffer in dictionary
			if (ValueBuffers.TryGetValue(key, out var obj))
			{
				if (obj is ValueBuffer<T> foundVB)
				{
					// clear buffer if value changed
					if (foundVB.Value.Equals(value) != true)
						foundVB.Buffer = null;
					// remember value
					foundVB.Value = value;
					// return value buffer
					return foundVB;
				}
				Log.ErrorOnce($"'{key}' found but not of correct type: expected '{typeof(ValueBuffer<T>)}', found '{obj?.GetType()}'", key.GetHashCode());
			}
			// create new value buffer, remember current value
			var newVB = new ValueBuffer<T>(value);
			ValueBuffers[key] = newVB;
			return newVB;
		}
		#endregion

		#region CLASSES
		private class ValueBuffer<T>
			where T : struct, IComparable
		{
			public string Buffer = null;
			public T Value;

			public ValueBuffer(T value)
			{
				Value = value;
			}
		}
		#endregion
	}

	public class ValueSetting<T>
		where T : IComparable
	{
		public string Name { get; set; }
		public string Label { get; set; }
		public string Description { get; set; }

		public T DefaultValue { get; set; }
		private T _value;
		public T Value
		{
			get => _value;
			set => Util.SetValue(ref _value, value, Action);
		}
		public Action<T> Action { get; set; }

		public ValueSetting(
			string name,
			string label,
			string description,
			T value,
			T defaultValue,
			Action<T> action)
		{
			Name = name;
			Label = label;
			Description = description;

			Value = value;
			DefaultValue = defaultValue;
			Action = action;
		}
	}

	public class TargetWrapper<T>
	{
		public T Value { get; set; }

		public TargetWrapper(T value)
		{
			Value = value;
		}
	}

	public static class Util
	{
		public static void SetValue<T>(ref T storage, T value, Action<T> action = null)
			where T : IComparable
		{
			if (storage == null && value == null || storage?.Equals(value) == true)
				return;
			storage = value;
			action?.Invoke(value);
		}
	}
}
