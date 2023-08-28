//
// Author:
//   Michael Göricke
//
// Copyright (c) 2023
//
// This file is part of FocalMaster.
//
// ShapeConverter is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program. If not, see<http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace ShapeConverter
{
    /// <summary>
    /// Base class for a color definition
    /// </summary>
    public abstract class GraphicBrush
    {
    }

    /// <summary>
    /// A solid color definition
    /// </summary>
    /// <seealso cref="ShapeConverter.GraphicBrush" />
    public class GraphicSolidColorBrush : GraphicBrush
    {
        public Color Color { get; set; }
    }
}
