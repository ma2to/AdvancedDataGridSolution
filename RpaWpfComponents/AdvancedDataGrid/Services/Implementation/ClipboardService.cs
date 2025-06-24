// RpaWpfComponents/AdvancedDataGrid/Services/Implementation/ClipboardService.cs
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;
using RpaWpfComponents.AdvancedDataGrid.Services.Interfaces;

namespace RpaWpfComponents.AdvancedDataGrid.Services.Implementation
{
    internal class ClipboardService : IClipboardService
    {
        private readonly ILogger<ClipboardService> _logger;

        public ClipboardService(ILogger<ClipboardService>? logger = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ClipboardService>.Instance;
        }

        public async Task<string> GetClipboardDataAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var result = Application.Current.Dispatcher.Invoke(() =>
                    {
                        return Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty;
                    });

                    _logger.LogDebug("Retrieved clipboard data, length: {Length}", result?.Length ?? 0);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting clipboard data");
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
                            _logger.LogDebug("Set clipboard data, length: {Length}", data.Length);
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error setting clipboard data");
                }
            });
        }

        public async Task<bool> HasClipboardDataAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var result = Application.Current.Dispatcher.Invoke(() => Clipboard.ContainsText());
                    _logger.LogDebug("Clipboard contains text: {HasData}", result);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking clipboard data");
                    return false;
                }
            });
        }

        public string ConvertToExcelFormat(string[,] data)
        {
            try
            {
                if (data == null || data.Length == 0)
                {
                    _logger.LogWarning("ConvertToExcelFormat called with null or empty data");
                    return string.Empty;
                }

                var sb = new StringBuilder();
                int rows = data.GetLength(0);
                int cols = data.GetLength(1);

                _logger.LogDebug("Converting {Rows}x{Cols} data to Excel format", rows, cols);

                for (int i = 0; i < rows; i++)
                {
                    var rowData = new string[cols];
                    for (int j = 0; j < cols; j++)
                    {
                        rowData[j] = data[i, j] ?? "";
                    }

                    if (i > 0)
                        sb.AppendLine();

                    sb.Append(string.Join("\t", rowData));
                }

                var result = sb.ToString();
                _logger.LogDebug("Excel format conversion completed, result length: {Length}", result.Length);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting data to Excel format");
                return string.Empty;
            }
        }

        public string[,] ParseFromExcelFormat(string clipboardData)
        {
            try
            {
                if (string.IsNullOrEmpty(clipboardData))
                {
                    _logger.LogWarning("ParseFromExcelFormat called with null or empty data");
                    return new string[0, 0];
                }

                _logger.LogDebug("Parsing clipboard data, length: {Length}", clipboardData.Length);

                var normalizedData = clipboardData.Replace("\r\n", "\n").Replace("\r", "\n");
                var lines = normalizedData.Split(new[] { '\n' }, StringSplitOptions.None);

                var lastNonEmptyLine = lines.Length - 1;
                while (lastNonEmptyLine >= 0 && string.IsNullOrEmpty(lines[lastNonEmptyLine]))
                {
                    lastNonEmptyLine--;
                }

                if (lastNonEmptyLine < 0)
                    return new string[0, 0];

                var actualLines = lines.Take(lastNonEmptyLine + 1).ToArray();

                if (actualLines.Length == 0)
                    return new string[0, 0];

                var maxCols = actualLines.Max(line => line.Split('\t').Length);

                if (actualLines.Length == 1 && !actualLines[0].Contains('\t'))
                {
                    var result = new string[1, 1];
                    result[0, 0] = actualLines[0];
                    _logger.LogDebug("Parsed single cell data");
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

                _logger.LogDebug("Parsed clipboard data to {Rows}x{Cols} array", actualLines.Length, maxCols);
                return resultArray;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing clipboard data from Excel format");
                var fallbackResult = new string[1, 1];
                fallbackResult[0, 0] = clipboardData ?? "";
                return fallbackResult;
            }
        }
    }
}