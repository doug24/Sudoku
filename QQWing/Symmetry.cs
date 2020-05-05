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
using System;
using System.Globalization;

namespace QQWingLib
{
    public enum Symmetry
    {
        NONE,
        ROTATE90,
        ROTATE180,
        MIRROR,
        FLIP,
        RANDOM
    }

    public static class SymmetryExtensions
    {
        public static Symmetry get(string name)
        {
            if (name == null) return Symmetry.NONE;

            name = name.ToUpperInvariant();
            if (Enum.TryParse(name, out Symmetry result))
                return result;

            return Symmetry.NONE;
        }

        public static string getName(this Symmetry item)
        {
            TextInfo ti = new CultureInfo("en-US", false).TextInfo;
            return ti.ToTitleCase(item.ToString());
        }

        public static Symmetry[] Values()
        {
            return new Symmetry[]
            {
                Symmetry.NONE,
                Symmetry.ROTATE90,
                Symmetry.ROTATE180,
                Symmetry.MIRROR,
                Symmetry.FLIP,
                Symmetry.RANDOM
           };
        }
    }
}
