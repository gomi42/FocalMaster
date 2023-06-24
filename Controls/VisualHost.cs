//
// Author:
//   Michael Göricke
//
// Copyright (c) 2023
//
// This file is part of FocalMaster.
//
// The FocalMaster is free software: you can redistribute it and/or modify
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

using System.Windows;
using System.Windows.Media;

namespace FocalMaster
{
    public partial class MainWindow
    {
        public class VisualHost : FrameworkElement
        {
            private DrawingVisual visual;

            public VisualHost(DrawingVisual visual)
            {
                this.visual = visual;
                AddVisualChild(visual);
            }

            protected override int VisualChildrenCount
            {
                get { return visual != null ? 1 : 0; }
            }

            protected override Visual GetVisualChild(int index)
            {
                return visual;
            }

            protected override Size MeasureOverride(Size availableSize)
            {
                return visual.ContentBounds.Size;
            }
        }
    }
}
