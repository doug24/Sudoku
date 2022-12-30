/*
 * qqwing - Sudoku solver and generator
 * Copyright (C) 2014 Stephen Ostermiller
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System.Diagnostics;
using System.Text;

namespace QQWingLib
{
    /// <summary>
    /// While solving the puzzle, log steps taken in a log item. This is useful for
    /// later printing out the solve history or gathering statistics about how hard
    /// the puzzle was to solve.
    /// </summary>
    public class LogItem
    {
        /// <summary>
        /// The recursion level at which this item was gathered. Used for backing out
        /// log items solve branches that don't lead to a solution.
        /// </summary>
        private int round;

        /// <summary>
        /// The type of log message that will determine the message printed.
        /// </summary>
        private LogType type;

        /// <summary>
        /// Value that was set by the operation (or zero for no value)
        /// </summary>
        private int value;

        /// <summary>
        /// position on the board at which the value (if any) was set.
        /// </summary>
        private int position;

        public LogItem(int r, LogType t)
        {
            Init(r, t, 0, -1);
        }

        public LogItem(int r, LogType t, int v, int p)
        {
            Init(r, t, v, p);
        }

        private void Init(int r, LogType t, int v, int p)
        {
            round = r;
            type = t;
            value = v;
            position = p;
        }

        public int GetRound()
        {
            return round;
        }

        /// <summary>
        /// Get the type of this log item.
        /// </summary>
        public LogType GetLogType()
        {
            return type;
        }

        public void Print()
        {
            Debug.WriteLine(ToString());
        }

        /// <summary>
        /// Get the row (1 indexed), or -1 if no row
        /// </summary>
        public int GetRow()
        {
            if (position <= -1) return -1;
            return QQWing.CellToRow(position) + 1;
        }

        /// <summary>
        /// Get the column (1 indexed), or -1 if no column
        /// </summary>
        public int GetColumn()
        {
            if (position <= -1) return -1;
            return QQWing.CellToColumn(position) + 1;
        }

        /// <summary>
        /// Get the position (0-80) on the board or -1 if no position
        /// </summary>
        public int GetPosition()
        {
            return position;
        }

        /// <summary>
        /// Get the value, or -1 if no value
        /// </summary>
        public int GetValue()
        {
            if (value <= 0) return -1;
            return value;
        }

        /// <summary>
        /// Print the current log item. The message used is determined by the type of
        /// log item.
        /// </summary>
        public string GetDescription()
        {
            StringBuilder sb = new();
            sb.Append("Round: ").Append(GetRound());
            sb.Append(" - ");
            sb.Append(GetLogType().GetDescription());
            if (value > 0 || position > -1)
            {
                sb.Append(" (");
                if (position > -1)
                {
                    sb.Append("Row: ").Append(GetRow()).Append(" - Column: ").Append(GetColumn());
                }
                if (value > 0)
                {
                    if (position > -1) sb.Append(" - ");
                    sb.Append("Value: ").Append(GetValue());
                }
                sb.Append(')');
            }
            return sb.ToString();
        }

        public override string ToString()
        {
            return GetDescription();
        }
    }
}
