// Services/Implementation/ClipboardService.cs - OPRAVENÝ
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Components.AdvancedDataGrid.Services.Interfaces;

namespace Components.AdvancedDataGrid.Services.Implementation
{
    public class ClipboardService : IClipboardService
    {
        public async Task<string> GetClipboardDataAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    return Application.Current.Dispatcher.Invoke(() =>
                    {
                        return Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty;
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ClipboardService GetClipboardDataAsync error: {ex.Message}");
                    return string.Empty;
                }
            });
        }

        public async Task SetClipboardDataAsync(string data)
        {
            await Task.Run(() =>
            {
                try
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (!string.IsNullOrEmpty(data))
                        {
                            Clipboard.SetText(data);
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ClipboardService SetClipboardDataAsync error: {ex.Message}");
                }
            });
        }

        public async Task<bool> HasClipboardDataAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    return Application.Current.Dispatcher.Invoke(() => Clipboard.ContainsText());
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ClipboardService HasClipboardDataAsync error: {ex.Message}");
                    return false;
                }
            });
        }

        public string ConvertToExcelFormat(string[,] data)
        {
            try
            {
                if (data == null || data.Length == 0)
                    return string.Empty;

                var sb = new StringBuilder();
                int rows = data.GetLength(0);
                int cols = data.GetLength(1);

                for (int i = 0; i < rows; i++)
                {
                    var rowData = new string[cols];
                    for (int j = 0; j < cols; j++)
                    {
                        rowData[j] = data[i, j] ?? "";
                    }

                    if (i > 0)
                        sb.AppendLine(); // Add line break before each row except first

                    sb.Append(string.Join("\t", rowData));
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ClipboardService ConvertToExcelFormat error: {ex.Message}");
                return string.Empty;
            }
        }

        public string[,] ParseFromExcelFormat(string clipboardData)
        {
            try
            {
                if (string.IsNullOrEmpty(clipboardData))
                    return new string[0, 0];

                // Handle different line endings
                var normalizedData = clipboardData.Replace("\r\n", "\n").Replace("\r", "\n");
                var lines = normalizedData.Split(new[] { '\n' }, StringSplitOptions.None);

                // Remove empty trailing lines
                var lastNonEmptyLine = lines.Length - 1;
                while (lastNonEmptyLine >= 0 && string.IsNullOrEmpty(lines[lastNonEmptyLine]))
                {
                    lastNonEmptyLine--;
                }

                if (lastNonEmptyLine < 0)
                    return new string[0, 0];

                // Take only non-empty lines
                var actualLines = lines.Take(lastNonEmptyLine + 1).ToArray();

                if (actualLines.Length == 0)
                    return new string[0, 0];

                // Find maximum number of columns
                var maxCols = actualLines.Max(line => line.Split('\t').Length);

                // Handle single cell case
                if (actualLines.Length == 1 && !actualLines[0].Contains('\t'))
                {
                    var result = new string[1, 1];
                    result[0, 0] = actualLines[0];
                    return result;
                }

                var resultArray = new string[actualLines.Length, maxCols];

                for (int i = 0; i < actualLines.Length; i++)
                {
                    var cells = actualLines[i].Split('\t');
                    for (int j = 0; j < maxCols; j++)
                    {
                        resultArray[i, j] = j < cells.Length ? (cells[j] ?? "") : "";
                    }
                }

                return resultArray;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ClipboardService ParseFromExcelFormat error: {ex.Message}");
                // Return single cell with original data as fallback
                var fallbackResult = new string[1, 1];
                fallbackResult[0, 0] = clipboardData ?? "";
                return fallbackResult;
            }
        }
    }
}