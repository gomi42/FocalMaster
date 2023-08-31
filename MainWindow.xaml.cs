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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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


            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);

            Title = string.Format("{0} v{1}.{2}", Title, fvi.ProductMajorPart, fvi.ProductMinorPart);
        }

        /////////////////////////////////////////////////////////////

        private void BarcodeFilesDragOver(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            e.Effects = DragDropEffects.Move;

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file).ToLower();

                if (ext != ".tif" && ext != ".jpg" && ext != ".png" && ext != ".pdf")
                {
                    e.Effects = DragDropEffects.None;
                    break;
                }
            }

            e.Handled = true;
        }

        /////////////////////////////////////////////////////////////

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
            ShowErrorsScan.Text = string.Empty;
        }

        /////////////////////////////////////////////////////////////

        private void ButtonAdd(object sender, RoutedEventArgs e)
        {
            var openDialog = new System.Windows.Forms.OpenFileDialog();
            openDialog.Filter = "Image Files (*.tif; *.jpg; *.png;)|*.tif;*.jpg;*.png|PDF Files (*.pdf)|*.pdf";
            openDialog.Multiselect = true;

            System.Windows.Forms.DialogResult result = openDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var files = BarcodeFiles.ItemsSource.Cast<string>().ToList();
                files.Add(openDialog.FileName);
                BarcodeFiles.ItemsSource = files;
            }

            ShowErrorsScan.Text = string.Empty;
        }

        /////////////////////////////////////////////////////////////

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
            ShowErrorsScan.Text = string.Empty;
        }

        /////////////////////////////////////////////////////////////

        private void ButtonRemoveAll(object sender, RoutedEventArgs e)
        {
            BarcodeFiles.ItemsSource = new List<string>();
            ShowErrorsScan.Text = string.Empty;
        }

        /////////////////////////////////////////////////////////////

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

        /////////////////////////////////////////////////////////////

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

        /////////////////////////////////////////////////////////////

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

        /////////////////////////////////////////////////////////////

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

        /////////////////////////////////////////////////////////////

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

        /////////////////////////////////////////////////////////////

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

        /////////////////////////////////////////////////////////////

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

        /////////////////////////////////////////////////////////////

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

        /////////////////////////////////////////////////////////////

        private void ButtonLoadRaw(object sender, RoutedEventArgs e)
        {
            var openDialog = new System.Windows.Forms.OpenFileDialog();
            openDialog.Filter = "Raw Files (*.*)|*.*";

            System.Windows.Forms.DialogResult result = openDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var decompiler = new Decompiler();
                decompiler.Decompile(openDialog.FileName, out string focal, out bool _);
                Focal.Text = focal;
                ShowErrors.Text = string.Empty;
            }
            else
            {
                ShowErrors.Text = $"Could not load raw file {openDialog.FileName}";
            }
        }

        /////////////////////////////////////////////////////////////

        private void ButtonValidate(object sender, RoutedEventArgs e)
        {
#if DEBUG
            if ((System.Windows.Forms.Control.ModifierKeys & System.Windows.Forms.Keys.Control) == System.Windows.Forms.Keys.Control
                && (System.Windows.Forms.Control.ModifierKeys & System.Windows.Forms.Keys.Alt) == System.Windows.Forms.Keys.Alt)
            {
                RunAutoTests();
                return;
            }
#endif

            var results = ValidateHelper.Validate(Focal.Text);
            ShowErrors.Text = string.Join("\n", results);
        }

        /////////////////////////////////////////////////////////////

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
                ShowErrorsScan.Text = "no files to scan";
                return;
            }

            var files = ((IEnumerable<string>)BarcodeFiles.ItemsSource).ToList();

            if (files.Count == 0)
            {
                ShowErrorsScan.Text = "no files to scan";
                return;
            }

            ShowScanning.Visibility = Visibility.Visible;

            byte[] code = null;
            List<ScannerResult> scannerResults = null;
            var scanner = new BarcodeScanner();

            var success = await Task.Run(() => scanner.Scan(files, out code, out scannerResults));

            var scanResults = ConvertScanResults(scannerResults, out bool error);

            if (success && code != null && code.Length > 0)
            {
                var decomp = new Decompiler();
                decomp.Decompile(code, out string focal, out bool endDetected);

                if (focal != null)
                {
                    Focal.Text = focal;
                }
                else
                {
                    Focal.Text = string.Empty;
                }

                if (!endDetected)
                {
                    scanResults.AppendLine("Warning: No END detected");
                }
            }

            ShowErrorsScan.Text = scanResults.ToString();


            if (!error)
            {
                MyTabControl.SelectedIndex = 1;
            }

            ShowScanning.Visibility = Visibility.Collapsed;
        }

        /////////////////////////////////////////////////////////////

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

            List<ImageResult> imageResults = null;
            List<ScannerResult> scannerResults = null;
            var scanner = new BarcodeScanner();

#if DEBUG
            if ((System.Windows.Forms.Control.ModifierKeys & System.Windows.Forms.Keys.Shift) == System.Windows.Forms.Keys.Shift)
            {
                await Task.Run(() => scanner.ScanDebugBoxes(files, out imageResults));
            }
            else
#endif
            {
                await Task.Run(() => scanner.ScanDebug(files, out imageResults, out scannerResults));
            }

            var bitmaps = new List<BitmapSource>();

            foreach (var imageData in imageResults)
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

            if (scannerResults != null)
            {
                ShowErrorsScan.Text = ConvertScanResults(scannerResults, out _).ToString();
            }
            else
            {
                ShowErrorsScan.Text = string.Empty;
            }

            MyImages.ItemsSource = bitmaps;

            ShowScanning.Visibility = Visibility.Collapsed;
        }

        /////////////////////////////////////////////////////////////

#if DEBUG
        private async void RunAutoTests()
        {
            int numErrors = 0;
            var results = new StringBuilder();
            List<string> entries = new List<string>();

            var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            location = location.Remove(location.Length - 9);

            using (StreamReader reader = File.OpenText(Path.Combine(location, "Tests.txt")))
            {
                string line;

                void ReadLines()
                {
                    entries.Clear();
                    line = reader.ReadLine();

                    do
                    {
                        entries.Add(line);
                        line = reader.ReadLine();
                    }
                    while (line != null && line != string.Empty);
                }

                ReadLines();
                DoCreateBarcodeTest(entries, results);

                while (line != null)
                {
                    ReadLines();
                    var error = await DoScanTest(entries, results);

                    if (error)
                    {
                        numErrors++;
                    }
                }
            }

            results.AppendLine($"ready with {numErrors} error(s)");
            Focal.Text = results.ToString();
        }

        /////////////////////////////////////////////////////////////

        private void DoCreateBarcodeTest(List<string> statements, StringBuilder results)
        {
            results.AppendLine("DrawingVisualBarcodeGenerator");
            var generator = new DrawingVisualBarcodeGenerator();
            var focal = string.Join("\r\n", statements);

            if (!generator.GenerateVisual(focal, out _))
            {
                results.Append(generator.Errors.ToString());
            }

            Focal.Text = results.ToString();
        }

        /////////////////////////////////////////////////////////////

        private async Task<bool> DoScanTest(List<string> files, StringBuilder results)
        {
            MyTabControl.SelectedIndex = 1;

            results.AppendLine(Path.GetFileName(files[0]));
            Focal.Text = results.ToString();

            byte[] code = null;
            List<ScannerResult> scannerResults = null;
            var scanner = new BarcodeScanner();

            var success = await Task.Run(() => scanner.Scan(files, out code, out scannerResults));

            results.Append(ConvertScanResults(scannerResults, out bool error));

            if (success && code != null && code.Length > 0)
            {
                var decomp = new Decompiler();
                decomp.Decompile(code, out string focal, out bool endDetected);

                if (!endDetected)
                {
                    results.AppendLine("Warning: No END detected");
                }

                var compiler = new Compiler();
                string[] lines = focal.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                int sourceLineNr = 1;

                foreach (var line in lines)
                {
                    if (compiler.Compile(line, out _, out string ErrorMsg))
                    {
                        results.AppendLine(string.Format("{0}, line {1}, \"{2}\"", ErrorMsg, sourceLineNr, line));
                    }

                    sourceLineNr++;
                }
            }

            Focal.Text = results.ToString();

            return error;
        }
#endif

        /////////////////////////////////////////////////////////////

        private StringBuilder ConvertScanResults(List<ScannerResult> results, out bool error)
        {
            var sb = new StringBuilder();
            error = false;

            foreach (ScannerResult result in results)
            {
                string fileInfo;

                if (result.PageNumber > 0)
                {
                    if (result.IsGraphic)
                    {
                        fileInfo = $"\"{result.Filename}\", page {result.PageNumber}, graphic";
                    }
                    else
                    {
                        fileInfo = $"\"{result.Filename}\", page {result.PageNumber}, image {result.ImageNumber}";
                    }
                }
                else
                {
                    fileInfo = result.Filename;
                }

                switch (result.ScanResult)
                {
                    case ScanerResultId.NoBarcodeFound:
                        sb.AppendLine($"Info: No barcodes found in {fileInfo}");
                        break;

                    case ScanerResultId.NoProgramCode:
                        sb.AppendLine($"Info: No programm barcodes found in {fileInfo}");
                        break;

                    case ScanerResultId.InvalidSignature:
                        sb.AppendLine($"Warning: Invalid barcode signature found in {fileInfo}");
                        break;

                    case ScanerResultId.CheckSumError:
                        sb.AppendLine($"Error: Checksum error in {fileInfo}");
                        error = true;
                        break;

                    case ScanerResultId.ProgramCode:
                        sb.AppendLine($"Info: Barcodes successfully read in {fileInfo}");
                        break;

                    case ScanerResultId.CannotOpenFile:
                        sb.AppendLine($"Error: Cannot open file {fileInfo}");
                        error = true;
                        break;
                }
            }

            return sb;
        }
    }
}
