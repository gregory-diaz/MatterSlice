/*
Copyright (C) 2013 David Braam
Copyright (c) 2014, Lars Brubaker

This file is part of MatterSlice.

MatterSlice is free software: you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

MatterSlice is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with MatterSlice.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;

using MatterSlice.ClipperLib;

namespace MatterHackers.MatterSlice
{
    using Polygon = List<IntPoint>;
    using Polygons = List<List<IntPoint>>;

    public static class LogOutput
    {
        public static int verbose_level;

        public static void logError(string message)
        {
            Console.Write(message);
        }

        public static void _log(string message)
        {
            if (verbose_level < 1)
            {
                return;
            }

            Console.Write(message);
        }

        public static EventHandler GetLogWrites;

        public static void log(string output)
        {
            Console.Write(output);

            if (GetLogWrites != null)
            {
                GetLogWrites(output, null);
            }
        }

        public static void logProgress(string type, int value, int maxValue)
        {
            if (verbose_level < 2)
            {
                return;
            }

            Console.Write("Progress:{0}:{1}:{2}\n".FormatWith(type, value, maxValue));
        }
    }
}