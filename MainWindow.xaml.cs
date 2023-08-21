﻿//
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FocalCompiler;
using FocalDecompiler;
using FocalMaster.Helper;

namespace FocalMaster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var list = new List<string>();
#if DEBUG
            list.Add(@"D:\Programme\HP41\FocalComp\Testdaten\Git\Test1.jpg");
            list.Add(@"D:\Rechner\Taschenrechner\HP-41\Wand\Manual\34.jpg");
#endif
            BarcodeFiles.ItemsSource = list;
            

            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);

            Title = string.Format("{0} v{1}.{2}", Title, fvi.ProductMajorPart, fvi.ProductMinorPart);
        }

        private void BarcodeFilesDragOver(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            e.Effects = DragDropEffects.Move;

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file);

                if (ext != ".tif" && ext != ".jpg" && ext != ".png")
                {
                    e.Effects = DragDropEffects.None;
                    break;
                }
            }

            e.Handled = true;
        }

        private void BarcodeFilesDrop(object sender, DragEventArgs e)
        {
            string[] newFiles = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (newFiles.Length < 1)
            {
                return;
            }

            var files = BarcodeFiles.ItemsSource.Cast<string>().ToList();
            files.AddRange(newFiles);
            BarcodeFiles.ItemsSource = files;
        }

        private void ButtonAdd(object sender, RoutedEventArgs e)
        {
            var openDialog = new System.Windows.Forms.OpenFileDialog();
            openDialog.Filter = "Image Files (*.tif; *.jpg; *.png)|*.tif;*.jpg;*.png";
            openDialog.Multiselect = true;

            System.Windows.Forms.DialogResult result = openDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var files = BarcodeFiles.ItemsSource.Cast<string>().ToList();
                files.Add(openDialog.FileName);
                BarcodeFiles.ItemsSource = files;
            }
        }

        private void ButtonRemove(object sender, RoutedEventArgs e)
        {
            var selectedIndex = BarcodeFiles.SelectedIndex;

            if (selectedIndex < 0)
            {
                return;
            }

            var files = BarcodeFiles.ItemsSource.Cast<string>().ToList();
            files.RemoveAt(selectedIndex);
            BarcodeFiles.ItemsSource = files;
        }

        private void ButtonRemoveAll(object sender, RoutedEventArgs e)
        {
            BarcodeFiles.ItemsSource = new List<string>();
        }

        private void ButtonUp(object sender, RoutedEventArgs e)
        {
            var selectedIndex = BarcodeFiles.SelectedIndex;

            if (selectedIndex <= 0)
            {
                return;
            }

            var files = BarcodeFiles.ItemsSource.Cast<string>().ToList();
            var file = files[selectedIndex];
            files.RemoveAt(selectedIndex);
            files.Insert(selectedIndex - 1, file);
            BarcodeFiles.ItemsSource = files;
        }

        private void ButtonDown(object sender, RoutedEventArgs e)
        {
            var selectedIndex = BarcodeFiles.SelectedIndex;
            var files = BarcodeFiles.ItemsSource.Cast<string>().ToList();

            if (selectedIndex < 0 || selectedIndex >= files.Count - 1)
            {
                return;
            }

            var file = files[selectedIndex];
            files.RemoveAt(selectedIndex);

            if (selectedIndex + 1 >= files.Count)
            {
                files.Add(file);
            }
            else
            {
                files.Insert(selectedIndex + 1, file);
            }

            BarcodeFiles.ItemsSource = files;
        }

        private void ButtonSort(object sender, RoutedEventArgs e)
        {
            if (BarcodeFiles.ItemsSource == null)
            {
                return;
            }

            var files = ((IEnumerable<string>)BarcodeFiles.ItemsSource).ToList();

            if (files.Count == 0)
            {
                return;
            }

            files.Sort();
            BarcodeFiles.ItemsSource = files;
        }

        private void ButtonLoadFocal(object sender, RoutedEventArgs e)
        {
            var openDialog = new System.Windows.Forms.OpenFileDialog();
            openDialog.Filter = "Focal Files (*.*)|*.*";

            System.Windows.Forms.DialogResult result = openDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                using (StreamReader reader = new StreamReader(openDialog.FileName))
                {
                    Focal.Text = reader.ReadToEnd();
                    ShowErrors.Text = string.Empty;
                }
            }
        }

        private void ButtonSaveFocal(object sender, RoutedEventArgs e)
        {
            var saveDialog = new System.Windows.Forms.SaveFileDialog();
            saveDialog.Filter = "Focal Files (*.*)|*.*";

            System.Windows.Forms.DialogResult result = saveDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                using (StreamWriter writer = new StreamWriter(saveDialog.FileName))
                {
                    writer.Write(Focal.Text);
                }
            }
        }

        private void ButtonCreateBarcode(object sender, RoutedEventArgs e)
        {
            MyImages.Visibility = Visibility.Collapsed;
            MyImages.ItemsSource = null;
            
            var generator = new DrawingVisualBarcodeGenerator();

            if (generator.GenerateVisual(Focal.Text, out DrawingVisual visual))
            {
                BarcodeVisualizer.Child = visual;
                MyTabControl.SelectedIndex = 2;
                ShowErrors.Text = string.Empty;
            }
            else
            {
                ShowErrors.Text = string.Join("\n", generator.Errors);
            }
        }

#if AlternativeImplementationUsingDrawing
        private void ButtonCreateBarcode2(object sender, RoutedEventArgs e)
        {
            MyImages.Visibility = Visibility.Collapsed;
            MyImages.ItemsSource = null;

            var generator = new DrawingBarcodeGenerator();

                if (generator.GenerateDrawing(Focal.Text, out Drawing drawing))
                {
                    DrawingImage drawingImage = new DrawingImage(drawing);
                    drawingImage.Freeze();

                    BarcodeImage.Source = drawingImage;

                    MyTabControl.SelectedIndex = 2;
                    ShowErrors.Text = string.Empty;
                }
                else
                {
                    ShowErrors.Text = string.Join("\n", generator.Errors);
                }
        }
#endif

        private void ButtonExportBarcode(object sender, RoutedEventArgs e)
        {
            var saveDialog = new System.Windows.Forms.SaveFileDialog();
            saveDialog.Filter = "PDF File (*.pdf)|*.pdf|JPG File (*.jpg)|*.jpg|PNG File (*.png)|*.png|TIF File (*.tif)|*.tif|SVG File (*.svg)|*.svg|EMF File (*.emf)|*.emf";

            System.Windows.Forms.DialogResult result = saveDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var fileExtension = Path.GetExtension(saveDialog.FileName).ToLower();

                switch (fileExtension)
                {
                    case ".jpg":
                    {
                        var generator = new JpgBarcodeGenerator();

                        if (generator.GenerateImage(Focal.Text, saveDialog.FileName))
                        {
                            ShowErrors.Text = string.Empty;
                        }
                        else
                        {
                            ShowErrors.Text = string.Join("\n", generator.Errors);
                        }

                        break;
                    }

                    case ".png":
                    {
                        var generator = new PngBarcodeGenerator();

                        if (generator.GenerateImage(Focal.Text, saveDialog.FileName))
                        {
                            ShowErrors.Text = string.Empty;
                        }
                        else
                        {
                            ShowErrors.Text = string.Join("\n", generator.Errors);
                        }

                        break;
                    }

                    case ".tif":
                    {
                        var generator = new TifBarcodeGenerator();

                        if (generator.GenerateImage(Focal.Text, saveDialog.FileName))
                        {
                            ShowErrors.Text = string.Empty;
                        }
                        else
                        {
                            ShowErrors.Text = string.Join("\n", generator.Errors);
                        }

                        break;
                    }

                    case ".pdf":
                    {
                        var generator = new PdfBarcodeGenerator();

                        if (generator.GeneratePdf(Focal.Text, saveDialog.FileName))
                        {
                            ShowErrors.Text = string.Empty;
                        }
                        else
                        {
                            ShowErrors.Text = string.Join("\n", generator.Errors);
                        }

                        break;
                    }

                    case ".svg":
                    {
                        var generator = new SvgBarcodeGenerator();

                        if (generator.GenerateSvg(Focal.Text, saveDialog.FileName))
                        {
                            ShowErrors.Text = string.Empty;
                        }
                        else
                        {
                            ShowErrors.Text = string.Join("\n", generator.Errors);
                        }

                        break;
                    }

                    case ".emf":
                    {
                        var generator = new EmfBarcodeGenerator();

                        if (generator.GenerateEmf(Focal.Text, saveDialog.FileName))
                        {
                            ShowErrors.Text = string.Empty;
                        }
                        else
                        {
                            ShowErrors.Text = string.Join("\n", generator.Errors);
                        }

                        break;
                    }
                }
            }
        }

        private void ButtonExportRaw(object sender, RoutedEventArgs e)
        {
            var saveDialog = new System.Windows.Forms.SaveFileDialog();
            saveDialog.Filter = "Raw Files (*.*)|*.*";

            System.Windows.Forms.DialogResult result = saveDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var generator = new FocalRawGenerator();

                if (generator.CompileString(Focal.Text, saveDialog.FileName))
                {
                    ShowErrors.Text = string.Empty;
                }
                else
                {
                    ShowErrors.Text = string.Join("\n", generator.Errors);
                }
            }
        }

        private void ButtonLoadRaw(object sender, RoutedEventArgs e)
        {
            var openDialog = new System.Windows.Forms.OpenFileDialog();
            openDialog.Filter = "Raw Files (*.*)|*.*";

            System.Windows.Forms.DialogResult result = openDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var decompiler = new Decompiler();
                decompiler.Decompile(openDialog.FileName, out string focal);
                Focal.Text = focal;
                ShowErrors.Text = string.Empty;
            }
            else
            {
                ShowErrors.Text = $"Could not load raw file {openDialog.FileName}";
            }
        }

        private void ButtonValidate(object sender, RoutedEventArgs e)
        {
            var results = ValidateHelper.Validate(Focal.Text);
            ShowErrors.Text = string.Join("\n", results);
        }

        private async void ButtonScan(object sender, RoutedEventArgs e)
        {
            MyImages.ItemsSource = null;
            MyImages.Visibility = Visibility.Collapsed;

            if ((System.Windows.Forms.Control.ModifierKeys & System.Windows.Forms.Keys.Control) == System.Windows.Forms.Keys.Control)
            {
                TestScan();
                return;
            }

            ShowErrorsScan.Text = string.Empty;

            if (BarcodeFiles.ItemsSource == null)
            {
                return;
            }

            var files = ((IEnumerable<string>)BarcodeFiles.ItemsSource).ToList();

            if (files.Count == 0)
            {
                return;
            }

            ShowScanning.Visibility = Visibility.Visible;

            var scanner = new BarcodeScanner();
            var focal = await Task.Run(() => scanner.Scan(files));

            if (focal != null)
            {
                Focal.Text = focal;
                MyTabControl.SelectedIndex = 1;
                ShowErrors.Text = string.Empty;
            }
            else
            {
                Focal.Text = string.Empty;
                ShowErrorsScan.Text = string.Join("\n", scanner.Errors);

                // the BitmapSource needs to be created in the main thread
                var errorImageData = scanner.ErrorImageData;

                if (errorImageData != null)
                {
                    var results = BitmapSourceConverter.GetBitmapSource(errorImageData.GrayImage, errorImageData.BarcodeAreas, errorImageData.AreaResults);
                    MyImages.ItemsSource = new List<BitmapSource> { results };
                }

                MyImages.Visibility = Visibility.Visible;
            }

            ShowScanning.Visibility = Visibility.Collapsed;
        }

#if DEBUG
        private void ButtonScan2(object sender, RoutedEventArgs e)
        {
            TestScan();
        }
#endif

        private async void TestScan()
        {
            var files = ((IEnumerable<string>)BarcodeFiles.ItemsSource).ToList();

            if (files.Count == 0)
            {
                MyImages.Visibility = Visibility.Collapsed;
                MyImages.ItemsSource = null;
                
                return;
            }

            ShowScanning.Visibility = Visibility.Visible;

            List<ErrorImageData> results;
            var scanner = new BarcodeScanner();

#if DEBUG
            if ((System.Windows.Forms.Control.ModifierKeys & System.Windows.Forms.Keys.Shift) == System.Windows.Forms.Keys.Shift)
            {
                results = await Task.Run(() => scanner.ScanDebugBoxes(files));
            }
            else
#endif
            {
                results = await Task.Run(() => scanner.ScanDebug(files));
            }

            var bitmaps = new List<BitmapSource>();

            foreach (var imageData in results)
            {
                BitmapSource bitmap;

                if (imageData.Edges != null)
                {
                    bitmap = BitmapSourceConverter.GetBitmapSource(imageData.Edges, imageData.BarcodeAreas, imageData.AreaResults);
                }
                else
                {
                    bitmap = BitmapSourceConverter.GetBitmapSource(imageData.GrayImage, imageData.BarcodeAreas, imageData.AreaResults);
                }

                bitmaps.Add(bitmap);
            }

            MyImages.Visibility = Visibility.Visible;
            MyTabControl.SelectedIndex = 2;
            ShowErrorsScan.Text = string.Join("\n", scanner.Errors);

            MyImages.ItemsSource = bitmaps;

            ShowScanning.Visibility = Visibility.Collapsed;
        }
    }
}
