// ============================================
// SÚBOR: RpaWpfComponents/AdvancedDataGrid/AdvancedDataGrid.cs
// ============================================
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RpaWpfComponents.AdvancedDataGrid.Views;
using RpaWpfComponents.AdvancedDataGrid.Models;
using RpaWpfComponents.AdvancedDataGrid.Events;

namespace RpaWpfComponents.AdvancedDataGrid
{
    /// <summary>
    /// Hlavný komponent pre pokročilý DataGrid s validáciou a real-time spracovaním
    /// </summary>
    public class AdvancedDataGridControl : UserControl
    {
        private readonly AdvancedDataGridView _internalView;

        public AdvancedDataGridControl()
        {
            _internalView = new AdvancedDataGridView();
            Content = _internalView;

            // Pripojenie error eventov
            _internalView.ErrorOccurred += OnInternalError;
        }

        #region Events

        /// <summary>
        /// Event ktorý sa spustí pri chybe v komponente
        /// </summary>
        public event EventHandler<ComponentError>? ErrorOccurred;

        #endregion

        #region Static Configuration Methods

        /// <summary>
        /// Konfiguruje dependency injection pre AdvancedDataGrid
        /// </summary>
        public static class Configuration
        {
            /// <summary>
            /// Konfiguruje služby pre AdvancedDataGrid
            /// </summary>
            public static void ConfigureServices(IServiceProvider serviceProvider)
            {
                RpaWpfComponents.AdvancedDataGrid.Configuration.DependencyInjectionConfig.ConfigureServices(serviceProvider);
            }

            /// <summary>
            /// Konfiguruje logging pre AdvancedDataGrid
            /// </summary>
            public static void ConfigureLogging(ILoggerFactory loggerFactory)
            {
                RpaWpfComponents.AdvancedDataGrid.Configuration.LoggerFactory.Configure(loggerFactory);
            }

            /// <summary>
            /// Zapne/vypne debug logging
            /// </summary>
            public static void SetDebugLogging(bool enabled)
            {
                RpaWpfComponents.AdvancedDataGrid.Helpers.DebugHelper.IsDebugEnabled = enabled;
            }
        }

        /// <summary>
        /// Extension metódy pre IServiceCollection
        /// </summary>

        #endregion

        #region Inicializácia a Konfigurácia

        /// <summary>
        /// Inicializuje komponent s konfiguráciou stĺpcov a validáciami
        /// </summary>
        /// <param name="columns">Definície stĺpcov</param>
        /// <param name="validationRules">Validačné pravidlá (voliteľné)</param>
        /// <param name="throttling">Throttling konfigurácia (voliteľné)</param>
        /// <param name="initialRowCount">Počiatočný počet riadkov</param>
        public async Task Initialize(
            List<ColumnDefinition> columns,
            List<ValidationRule>? validationRules = null,
            ThrottlingConfig? throttling = null,
            int initialRowCount = 50)
        {
            try
            {
                var internalColumns = columns.Select(c => c.ToInternal()).ToList();
                var internalRules = validationRules?.Select(r => r.ToInternal()).ToList();

                await _internalView.InitializeAsync(internalColumns, internalRules);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentError(ex, "Initialize"));
            }
        }

        /// <summary>
        /// Resetuje komponent do pôvodného stavu
        /// </summary>
        public void Reset()
        {
            try
            {
                _internalView.Reset();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentError(ex, "Reset"));
            }
        }

        #endregion

        #region Načítanie Dát

        /// <summary>
        /// Načíta dáta z DataTable s automatickou validáciou
        /// </summary>
        public async Task LoadData(DataTable dataTable)
        {
            try
            {
                await _internalView.LoadDataAsync(dataTable);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentError(ex, "LoadData"));
            }
        }

        /// <summary>
        /// Načíta dáta zo zoznamu dictionary objektov
        /// </summary>
        public async Task LoadData(List<Dictionary<string, object>> data)
        {
            try
            {
                await _internalView.LoadDataAsync(data);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentError(ex, "LoadData"));
            }
        }

        #endregion

        #region Export Dát

        /// <summary>
        /// Exportuje validné dáta do DataTable
        /// </summary>
        public async Task<DataTable> ExportToDataTable()
        {
            try
            {
                return await _internalView.ExportDataAsync();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentError(ex, "ExportToDataTable"));
                return new DataTable();
            }
        }

        #endregion

        #region Validácia

        /// <summary>
        /// Validuje všetky riadky a vráti true ak sú všetky validné
        /// </summary>
        public async Task<bool> ValidateAll()
        {
            try
            {
                return await _internalView.ValidateAllRowsAsync();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentError(ex, "ValidateAll"));
                return false;
            }
        }

        #endregion

        #region Manipulácia s Riadkami

        /// <summary>
        /// Vymaže všetky dáta zo všetkých buniek
        /// </summary>
        public async Task ClearAllData()
        {
            try
            {
                await _internalView.ClearAllDataAsync();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentError(ex, "ClearAllData"));
            }
        }

        /// <summary>
        /// Odstráni všetky prázdne riadky
        /// </summary>
        public async Task RemoveEmptyRows()
        {
            try
            {
                await _internalView.RemoveEmptyRowsAsync();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentError(ex, "RemoveEmptyRows"));
            }
        }

        /// <summary>
        /// Odstráni riadky ktoré spĺňajú zadanú podmienku
        /// </summary>
        public async Task RemoveRowsByCondition(string columnName, Func<object, bool> condition)
        {
            try
            {
                await _internalView.RemoveRowsByConditionAsync(columnName, condition);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentError(ex, "RemoveRowsByCondition"));
            }
        }

        /// <summary>
        /// Odstráni riadky ktoré nevyhovujú vlastným validačným pravidlám
        /// </summary>
        public async Task<int> RemoveRowsByValidation(List<ValidationRule> customRules)
        {
            try
            {
                var internalRules = customRules.Select(r => r.ToInternal()).ToList();
                return await _internalView.RemoveRowsByCustomValidationAsync(internalRules);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentError(ex, "RemoveRowsByValidation"));
                return 0;
            }
        }

        #endregion

        #region Private Event Handlers

        private void OnInternalError(object? sender, ComponentErrorEventArgs e)
        {
            OnErrorOccurred(new ComponentError(e.Exception, e.Operation, e.AdditionalInfo));
        }

        private void OnErrorOccurred(ComponentError error)
        {
            ErrorOccurred?.Invoke(this, error);
        }

        #endregion
    }

    #region Public API Models

    /// <summary>
    /// Definícia stĺpca pre AdvancedDataGrid
    /// </summary>
    public class ColumnDefinition
    {
        public string Name { get; set; } = string.Empty;
        public Type DataType { get; set; } = typeof(string);
        public double MinWidth { get; set; } = 80;
        public double MaxWidth { get; set; } = 300;
        public bool AllowResize { get; set; } = true;
        public bool AllowSort { get; set; } = true;
        public bool IsReadOnly { get; set; } = false;

        internal ColumnDefinitionModel ToInternal() => new()
        {
            Name = Name,
            DataType = DataType,
            MinWidth = MinWidth,
            MaxWidth = MaxWidth,
            AllowResize = AllowResize,
            AllowSort = AllowSort,
            IsReadOnly = IsReadOnly
        };
    }

    /// <summary>
    /// Validačné pravidlo pre bunky v DataGrid
    /// </summary>
    public class ValidationRule
    {
        public string ColumnName { get; set; } = string.Empty;
        public Func<object, GridDataRow, bool> ValidationFunction { get; set; } = (value, row) => true;
        public string ErrorMessage { get; set; } = string.Empty;
        public Func<GridDataRow, bool> ApplyCondition { get; set; } = _ => true;
        public int Priority { get; set; } = 0;
        public string RuleName { get; set; }

        public ValidationRule()
        {
            RuleName = Guid.NewGuid().ToString();
        }

        internal ValidationRuleModel ToInternal() => new()
        {
            ColumnName = ColumnName,
            ValidationFunction = (value, row) => ValidationFunction?.Invoke(value, new GridDataRow(row)) ?? true,
            ErrorMessage = ErrorMessage,
            ApplyCondition = row => ApplyCondition?.Invoke(new GridDataRow(row)) ?? true,
            Priority = Priority,
            RuleName = RuleName
        };
    }

    /// <summary>
    /// Konfigurácia pre throttling validácií
    /// </summary>
    public class ThrottlingConfig
    {
        public int TypingDelayMs { get; set; } = 300;
        public int PasteDelayMs { get; set; } = 100;
        public int MaxConcurrentValidations { get; set; } = 3;
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Informácie o chybe v komponente
    /// </summary>
    public class ComponentError : EventArgs
    {
        public Exception Exception { get; set; }
        public string Operation { get; set; }
        public string AdditionalInfo { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public ComponentError(Exception exception, string operation, string additionalInfo = null)
        {
            Exception = exception;
            Operation = operation;
            AdditionalInfo = additionalInfo;
        }

        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Operation}: {Exception.Message}" +
                   (string.IsNullOrEmpty(AdditionalInfo) ? "" : $" - {AdditionalInfo}");
        }
    }

    /// <summary>
    /// Wrapper pre prístup k riadku v validačných funkciách
    /// </summary>
    public class GridDataRow
    {
        private readonly DataGridRowModel _internal;

        internal GridDataRow(DataGridRowModel internalModel)
        {
            _internal = internalModel;
        }

        /// <summary>
        /// Získa hodnotu z bunky podľa názvu stĺpca
        /// </summary>
        public object? GetValue(string columnName) => _internal.GetCell(columnName)?.Value;
    }

    #endregion

    #region Static Validation Helpers

    /// <summary>
    /// Pomocné metódy pre tvorbu validačných pravidiel
    /// </summary>
    public static class Validation
    {
        /// <summary>
        /// Vytvorí pravidlo pre povinné pole
        /// </summary>
        public static ValidationRule Required(string columnName, string errorMessage = null)
        {
            return new ValidationRule
            {
                ColumnName = columnName,
                ValidationFunction = (value, row) => !string.IsNullOrWhiteSpace(value?.ToString()),
                ErrorMessage = errorMessage ?? $"{columnName} je povinné pole",
                RuleName = $"{columnName}_Required"
            };
        }

        /// <summary>
        /// Vytvorí pravidlo pre kontrolu dĺžky textu
        /// </summary>
        public static ValidationRule Length(string columnName, int minLength, int maxLength = int.MaxValue, string errorMessage = null)
        {
            return new ValidationRule
            {
                ColumnName = columnName,
                ValidationFunction = (value, row) =>
                {
                    var text = value?.ToString() ?? "";
                    return text.Length >= minLength && text.Length <= maxLength;
                },
                ErrorMessage = errorMessage ?? $"{columnName} musí mať dĺžku medzi {minLength} a {maxLength} znakmi",
                RuleName = $"{columnName}_Length"
            };
        }

        /// <summary>
        /// Vytvorí pravidlo pre kontrolu číselného rozsahu
        /// </summary>
        public static ValidationRule Range(string columnName, double min, double max, string errorMessage = null)
        {
            return new ValidationRule
            {
                ColumnName = columnName,
                ValidationFunction = (value, row) =>
                {
                    if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                        return true;

                    if (double.TryParse(value.ToString(), out double numValue))
                    {
                        return numValue >= min && numValue <= max;
                    }

                    return false;
                },
                ErrorMessage = errorMessage ?? $"{columnName} musí byť medzi {min} a {max}",
                RuleName = $"{columnName}_Range"
            };
        }

        /// <summary>
        /// Vytvorí podmienené validačné pravidlo
        /// </summary>
        public static ValidationRule Conditional(string columnName,
            Func<object, GridDataRow, bool> validationFunction,
            Func<GridDataRow, bool> condition,
            string errorMessage,
            string ruleName = null)
        {
            return new ValidationRule
            {
                ColumnName = columnName,
                ValidationFunction = validationFunction,
                ApplyCondition = condition,
                ErrorMessage = errorMessage,
                RuleName = ruleName ?? $"{columnName}_Conditional_{Guid.NewGuid().ToString("N")[..8]}"
            };
        }

        /// <summary>
        /// Vytvorí pravidlo pre validáciu číselných hodnôt
        /// </summary>
        public static ValidationRule Numeric(string columnName, string errorMessage = null)
        {
            return new ValidationRule
            {
                ColumnName = columnName,
                ValidationFunction = (value, row) =>
                {
                    if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                        return true;

                    return double.TryParse(value.ToString(), out _);
                },
                ErrorMessage = errorMessage ?? $"{columnName} musí byť číslo",
                RuleName = $"{columnName}_Numeric"
            };
        }

        /// <summary>
        /// Vytvorí pravidlo pre validáciu emailu
        /// </summary>
        public static ValidationRule Email(string columnName, string errorMessage = null)
        {
            return new ValidationRule
            {
                ColumnName = columnName,
                ValidationFunction = (value, row) =>
                {
                    if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                        return true;

                    var email = value.ToString();
                    return email.Contains("@") && email.Contains(".") && email.Length > 5;
                },
                ErrorMessage = errorMessage ?? $"{columnName} musí mať platný formát emailu",
                RuleName = $"{columnName}_Email"
            };
        }
    }

    #endregion
}