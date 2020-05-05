// @formatter:off
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
// @formatter:on

namespace QQWingLib
{
    public enum LogType
    {
        GIVEN,
        SINGLE,
        HIDDEN_SINGLE_ROW,
        HIDDEN_SINGLE_COLUMN,
        HIDDEN_SINGLE_SECTION,
        GUESS,
        ROLLBACK,
        NAKED_PAIR_ROW,
        NAKED_PAIR_COLUMN,
        NAKED_PAIR_SECTION,
        POINTING_PAIR_TRIPLE_ROW,
        POINTING_PAIR_TRIPLE_COLUMN,
        ROW_BOX,
        COLUMN_BOX,
        HIDDEN_PAIR_ROW,
        HIDDEN_PAIR_COLUMN,
        HIDDEN_PAIR_SECTION
    }

    public static class LogTypeExtensions
    {
        private static readonly string[] descriptions = new string[]
        {
            "Mark given",
            "Mark only possibility for cell",
            "Mark single possibility for value in row",
            "Mark single possibility for value in column",
            "Mark single possibility for value in section",
            "Mark guess (start round)",
            "Roll back round",
            "Remove possibilities for naked pair in row",
            "Remove possibilities for naked pair in column",
            "Remove possibilities for naked pair in section",
            "Remove possibilities for row because all values are in one section",
            "Remove possibilities for column because all values are in one section",
            "Remove possibilities for section because all values are in one row",
            "Remove possibilities for section because all values are in one column",
            "Remove possibilities from hidden pair in row",
            "Remove possibilities from hidden pair in column",
            "Remove possibilities from hidden pair in section"
        };

        public static string getDescription(this LogType item)
        {
            return descriptions[(int)item];
        }
    }
}
