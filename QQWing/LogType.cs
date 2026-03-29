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

namespace QQWingLib;

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
    HIDDEN_PAIR_SECTION,
    X_WING_ROW,
    X_WING_COLUMN,
    Y_WING,
    SIMPLE_COLORING,
    NAKED_TRIPLE_ROW,
    NAKED_TRIPLE_COLUMN,
    NAKED_TRIPLE_SECTION,
    NAKED_QUAD_ROW,
    NAKED_QUAD_COLUMN,
    NAKED_QUAD_SECTION,
    SWORDFISH_ROW,
    SWORDFISH_COLUMN,
    HIDDEN_TRIPLE_ROW,
    HIDDEN_TRIPLE_COLUMN,
    HIDDEN_TRIPLE_SECTION,
    HIDDEN_QUAD_ROW,
    HIDDEN_QUAD_COLUMN,
    HIDDEN_QUAD_SECTION,
    XYZ_WING,
    JELLYFISH_ROW,
    JELLYFISH_COLUMN
}

public static class LogTypeExtensions
{
    private static readonly string[] descriptions =
    [
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
        "Remove possibilities from hidden pair in section",
        "Remove possibilities from X-Wing in rows",
        "Remove possibilities from X-Wing in columns",
        "Remove possibilities from Y-Wing",
        "Remove possibilities from simple coloring",
        "Remove possibilities for naked triple in row",
        "Remove possibilities for naked triple in column",
        "Remove possibilities for naked triple in section",
        "Remove possibilities for naked quad in row",
        "Remove possibilities for naked quad in column",
        "Remove possibilities for naked quad in section",
        "Remove possibilities from Swordfish in rows",
        "Remove possibilities from Swordfish in columns",
        "Remove possibilities from hidden triple in row",
        "Remove possibilities from hidden triple in column",
        "Remove possibilities from hidden triple in section",
        "Remove possibilities from hidden quad in row",
        "Remove possibilities from hidden quad in column",
        "Remove possibilities from hidden quad in section",
        "Remove possibilities from XYZ-Wing",
        "Remove possibilities from Jellyfish in rows",
        "Remove possibilities from Jellyfish in columns"
    ];

    public static string GetDescription(this LogType item)
    {
        return descriptions[(int)item];
    }
}