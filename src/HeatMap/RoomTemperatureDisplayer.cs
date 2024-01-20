using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace HeatMap
{
    public class RoomTemperatureDisplayer
    {
        public List<IntVec3> LabelCells { get; } = new List<IntVec3>();

		private Map _prevMap = null;
        private int _nextUpdateTick = 0;

        public void Update(int updateDelay)
		{
			var tick = Find.TickManager.TicksGame;
			var map = Find.CurrentMap;
			if (_prevMap == map && tick < _nextUpdateTick)
                return;
            _prevMap = map;

            _nextUpdateTick = tick + updateDelay;
            LabelCells.Clear();

			foreach (var room in map.regionGrid.allRooms)
            {
                if (room.PsychologicallyOutdoors || room.Fogged || room.IsDoorway || room.BorderCells.Count() == 0)
                    continue;

                var cell = GetBestCellForRoom(room, map);
                LabelCells.Add(cell);
            }
        }

        private static IntVec3 GetBestCellForRoom(Room room, Map map)
		{
			var topLeftCorner = room.BorderCells.First();

            var left = int.MaxValue;
            var top = int.MinValue;
            var right = int.MinValue;
            var bottom = int.MaxValue;

            foreach (var cell in room.BorderCells)
            {
                if (cell.x < topLeftCorner.x || cell.z > topLeftCorner.z)
                    topLeftCorner = cell;

                if (cell.x < left)
                    left = cell.x;
                else if (cell.x > right)
                    right = cell.x;

                if (cell.z > top)
                    top = cell.z;
                else if (cell.z < bottom)
                    bottom = cell.z;
            }

            var midX = (int)((right - left) / 2f) + left;
            var midZ = (int)((top - bottom) / 2f) + bottom;

            var midCell = new IntVec3(midX, 0, midZ);

			if (midCell.GetRoom(map) == room)
                return midCell;

            var possiblyBetterTopLeftCorner = topLeftCorner;
            possiblyBetterTopLeftCorner.x++;
            possiblyBetterTopLeftCorner.z--;
            if (possiblyBetterTopLeftCorner.GetRoom(map) == room)
                topLeftCorner = possiblyBetterTopLeftCorner;

            return topLeftCorner;
        }

        public void Reset()
		{
			LabelCells.Clear();
            _nextUpdateTick = 0;
		}
		
		public void OnGUI()
        {
            var map = Find.CurrentMap;
			Text.Font = GameFont.Tiny;
			foreach (var cell in LabelCells)
            {
                var room = cell.GetRoom(map);
                if (room == null)
                    continue;

                var drawTopLeft = GenMapUI.LabelDrawPosFor(cell);
                var labelRect = new Rect(drawTopLeft.x, drawTopLeft.y, 40f, 20f);
                Widgets.Label(labelRect, room.Temperature.ToStringTemperature("F0"));
            }
        }
    }
}