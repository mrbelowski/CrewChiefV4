// This file is part of iRacingSDK.
//
// Copyright 2014 Dean Netherton
// https://github.com/vipoo/iRacingSDK.Net
//
// iRacingSDK is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// iRacingSDK is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with iRacingSDK.  If not, see <http://www.gnu.org/licenses/>.

using System.Diagnostics;

namespace iRacingSDK.Support
{
    public static class TraceWarning
    {
        const string Category = "WARNING";

        public static void WriteLine(string value, params object[] args)
        {
            Trace.WriteLine(value.F(args), Category);
        }

        public static void Write(string value, params object[] args)
        {
            Trace.Write(value.F(args), Category);
        }

        public static void WriteLineIf(bool condition, string value, params object[] args)
        {
            Trace.WriteLineIf(condition, value.F(args), Category);
        }

        public static void WriteIf(bool condition, string value, params object[] args)
        {
            Trace.WriteIf(condition, value.F(args), Category);
        }
    }
}
