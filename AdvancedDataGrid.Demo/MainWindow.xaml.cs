// MainWindow.xaml.cs - KOMPLETNE OPRAVENÝ
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Components.AdvancedDataGrid.Models;
using Components.AdvancedDataGrid.Configuration;
using Components.AdvancedDataGrid.Events;
using Components.AdvancedDataGrid.Helpers;

namespace AdvancedDataGrid.Demo
{
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private bool _isInitialized = false;

        public MainWindow()
        {
            InitializeComponent();

            // Setup Dependency Injection
            _serviceProvider = CreateServiceProvider();

            // Initialize component
            this.Loaded += MainWindow_Loaded;
        }

        private IServiceProvider CreateServiceProvider()
        {
            var services = new ServiceCollection();

            // Register AdvancedDataGrid services
            services.AddAdvancedDataGrid();

            return services.BuildServiceProvider();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatus("Initializing AdvancedDataGrid...");

                // Create basic column definitions
                var columns = new List<ColumnDefinitionModel>
                {
                    new() { Name = "ID", DataType = typeof(int), MinWidth = 60, MaxWidth = 100, IsReadOnly = true },
                    new() { Name = "FirstName", DataType = typeof(string), MinWidth = 120, MaxWidth = 200 },
                    new() { Name = "LastName", DataType = typeof(string), MinWidth = 120, MaxWidth = 200 },
                    new() { Name = "Email", DataType = typeof(string), MinWidth = 200, MaxWidth = 300 },
                    new() { Name = "Age", DataType = typeof(int), MinWidth = 80, MaxWidth = 100 },
                    new() { Name = "Salary", DataType = typeof(decimal), MinWidth = 120, MaxWidth = 150 },
                    new() { Name = "Department", DataType = typeof(string), MinWidth = 150, MaxWidth = 200 },
                    new() { Name = "IsActive", DataType = typeof(bool), MinWidth = 80, MaxWidth = 100 },
                    new() { Name = "Notes", DataType = typeof(string), MinWidth = 200, MaxWidth = 400 },
                    new() { Name = "DeleteAction", DataType = typeof(string), MinWidth = 60, MaxWidth = 60 },
                    new() { Name = "ValidAlerts", DataType = typeof(string), MinWidth = 200, MaxWidth = 400 }
                };

                // Initialize the component
                await AdvancedDataGrid.InitializeAsync(columns);
                _isInitialized = true;

                UpdateStatus("AdvancedDataGrid initialized successfully. Load sample data to begin.");
                UpdateValidationStatus("Pripravené");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Initialization failed: {ex.Message}");
                MessageBox.Show($"Failed to initialize AdvancedDataGrid:\n{ex.Message}",
                               "Initialization Error",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }

        #region Data Management Buttons

        private async void LoadSampleDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                MessageBox.Show("Component not initialized yet.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                UpdateStatus("Loading sample data with validation rules...");
                UpdateValidationStatus("Pridávajú sa validačné pravidlá...");

                // Najprv pridaj validation rules
                await AddValidationRulesAsync();

                UpdateValidationStatus("Načítavajú sa dáta...");

                var sampleData = CreateSampleData();
                await AdvancedDataGrid.LoadDataAsync(sampleData);

                UpdateStatus($"Loaded {sampleData.Rows.Count} sample records with validation successfully.");
                UpdateValidationStatus("Pripravené");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Failed to load sample data: {ex.Message}");
                UpdateValidationStatus("Chyba pri načítavaní");
                MessageBox.Show($"Failed to load sample data:\n{ex.Message}",
                               "Load Error",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }

        private async void RemoveInvalidRowsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                MessageBox.Show("Component not initialized yet.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Dialog pre výber stĺpcov a podmienok
                var result = MessageBox.Show(
                    "This will remove all rows that have validation errors in ANY column.\n\n" +
                    "Do you want to continue?",
                    "Remove Invalid Rows",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                UpdateStatus("Removing invalid rows...");
                UpdateValidationStatus("Odstraňujú sa nevalidné riadky...");

                // Validuj všetky riadky najprv
                await AdvancedDataGrid.ValidateAllRowsAsync();

                // Odstráň riadky s chybami
                await AdvancedDataGrid.RemoveRowsByConditionAsync("HasValidationErrors", value =>
                {
                    if (value is bool hasErrors)
                        return hasErrors;
                    return false;
                });

                UpdateStatus("Invalid rows removed successfully.");
                UpdateValidationStatus("Pripravené");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Failed to remove invalid rows: {ex.Message}");
                UpdateValidationStatus("Chyba pri odstraňovaní");
                MessageBox.Show($"Failed to remove invalid rows:\n{ex.Message}",
                               "Remove Error",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }

        private async void ExportDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                MessageBox.Show("Component not initialized yet.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                UpdateStatus("Exporting data...");

                var dataTable = await AdvancedDataGrid.ExportDataAsync();

                UpdateStatus($"Exported {dataTable.Rows.Count} rows successfully.");

                MessageBox.Show($"Data exported successfully!\n\nRows: {dataTable.Rows.Count}\nColumns: {dataTable.Columns.Count}",
                               "Export Success",
                               MessageBoxButton.OK,
                               MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Failed to export data: {ex.Message}");
                MessageBox.Show($"Failed to export data:\n{ex.Message}",
                               "Export Error",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }

        #endregion

        #region Component Action Buttons

        private async void ValidateAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                MessageBox.Show("Component not initialized yet.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                UpdateStatus("Validating all rows...");
                UpdateValidationStatus("Validujú sa všetky riadky...");

                var isAllValid = await AdvancedDataGrid.ValidateAllRowsAsync();

                var message = isAllValid ? "All rows are valid!" : "Some rows have validation errors. Check the validation alerts column for details.";
                var icon = isAllValid ? MessageBoxImage.Information : MessageBoxImage.Warning;

                UpdateStatus(isAllValid ? "Validation completed - all rows valid" : "Validation completed - errors found");
                UpdateValidationStatus(isAllValid ? "Všetky riadky sú validné" : "Nájdené validačné chyby");

                MessageBox.Show(message, "Validation Results", MessageBoxButton.OK, icon);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Validation failed: {ex.Message}");
                UpdateValidationStatus("Chyba pri validácii");
                MessageBox.Show($"Validation failed:\n{ex.Message}",
                               "Validation Error",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }

        private async void RemoveEmptyRowsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                MessageBox.Show("Component not initialized yet.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                UpdateStatus("Removing empty rows...");
                await AdvancedDataGrid.RemoveEmptyRowsAsync();
                UpdateStatus("Empty rows removed successfully.");
                UpdateValidationStatus("Pripravené");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Failed to remove empty rows: {ex.Message}");
                MessageBox.Show($"Failed to remove empty rows:\n{ex.Message}",
                               "Remove Error",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }

        private async void CopyDataButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Trigger copy command on the component
                if (AdvancedDataGrid.ViewModel?.CopyCommand?.CanExecute(null) == true)
                {
                    AdvancedDataGrid.ViewModel.CopyCommand.Execute(null);
                    UpdateStatus("Data copied to clipboard");
                }
                else
                {
                    UpdateStatus("No data to copy");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Copy failed: {ex.Message}");
                MessageBox.Show($"Failed to copy data:\n{ex.Message}",
                               "Copy Error",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }

        private async void ClearAllDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                MessageBox.Show("Component not initialized yet.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (MessageBox.Show("Are you sure you want to clear all data?",
                                   "Confirm Clear",
                                   MessageBoxButton.YesNo,
                                   MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    UpdateStatus("Clearing all data...");
                    await AdvancedDataGrid.ClearAllDataAsync();
                    UpdateStatus("All data cleared successfully.");
                    UpdateValidationStatus("Pripravené");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Failed to clear data: {ex.Message}");
                MessageBox.Show($"Failed to clear data:\n{ex.Message}",
                               "Clear Error",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }

        #endregion

        #region Helper Methods

        private async Task AddValidationRulesAsync()
        {
            // Create validation rules
            var validationRules = new List<ValidationRuleModel>
            {
                ValidationHelper.CreateRequiredRule("FirstName", "First Name is required"),
                ValidationHelper.CreateRequiredRule("LastName", "Last Name is required"),
                ValidationHelper.CreateRequiredRule("Email", "Email is required"),
                ValidationHelper.CreateLengthRule("FirstName", 2, 50, "First Name must be 2-50 characters"),
                ValidationHelper.CreateLengthRule("LastName", 2, 50, "Last Name must be 2-50 characters"),
                ValidationHelper.CreateRangeRule("Age", 18, 99, "Age must be between 18 and 99"),
                ValidationHelper.CreateRangeRule("Salary", 0, 999999, "Salary must be between 0 and 999,999"),
                ValidationHelper.CreateConditionalRule("Department",
                    (value, row) => !string.IsNullOrWhiteSpace(value?.ToString()),
                    row => row.GetValue<bool>("IsActive"),
                    "Department is required for active employees")
            };

            // Re-initialize with validation rules
            var columns = new List<ColumnDefinitionModel>
            {
                new() { Name = "ID", DataType = typeof(int), MinWidth = 60, MaxWidth = 100, IsReadOnly = true },
                new() { Name = "FirstName", DataType = typeof(string), MinWidth = 120, MaxWidth = 200 },
                new() { Name = "LastName", DataType = typeof(string), MinWidth = 120, MaxWidth = 200 },
                new() { Name = "Email", DataType = typeof(string), MinWidth = 200, MaxWidth = 300 },
                new() { Name = "Age", DataType = typeof(int), MinWidth = 80, MaxWidth = 100 },
                new() { Name = "Salary", DataType = typeof(decimal), MinWidth = 120, MaxWidth = 150 },
                new() { Name = "Department", DataType = typeof(string), MinWidth = 150, MaxWidth = 200 },
                new() { Name = "IsActive", DataType = typeof(bool), MinWidth = 80, MaxWidth = 100 },
                new() { Name = "Notes", DataType = typeof(string), MinWidth = 200, MaxWidth = 400 },
                new() { Name = "DeleteAction", DataType = typeof(string), MinWidth = 60, MaxWidth = 60 },
                new() { Name = "ValidAlerts", DataType = typeof(string), MinWidth = 200, MaxWidth = 400 }
            };

            await AdvancedDataGrid.InitializeAsync(columns, validationRules);
        }

        private DataTable CreateSampleData()
        {
            var dataTable = new DataTable();

            // Define columns
            dataTable.Columns.Add("ID", typeof(int));
            dataTable.Columns.Add("FirstName", typeof(string));
            dataTable.Columns.Add("LastName", typeof(string));
            dataTable.Columns.Add("Email", typeof(string));
            dataTable.Columns.Add("Age", typeof(int));
            dataTable.Columns.Add("Salary", typeof(decimal));
            dataTable.Columns.Add("Department", typeof(string));
            dataTable.Columns.Add("IsActive", typeof(bool));
            dataTable.Columns.Add("Notes", typeof(string));

            // Add sample data
            var sampleData = new[]
            {
                new object[] { 1, "John", "Doe", "john.doe@example.com", 30, 75000m, "Engineering", true, "Senior Developer" },
                new object[] { 2, "Jane", "Smith", "jane.smith@example.com", 28, 68000m, "Marketing", true, "Marketing Specialist" },
                new object[] { 3, "Bob", "Johnson", "bob.johnson@example.com", 35, 82000m, "Engineering", true, "Team Lead" },
                new object[] { 4, "Alice", "Williams", "alice.williams@example.com", 25, 55000m, "HR", false, "HR Assistant" },
                new object[] { 5, "Charlie", "Brown", "charlie.brown@example.com", 42, 95000m, "Engineering", true, "Senior Engineer" },
                new object[] { 6, "Diana", "Davis", "diana.davis@example.com", 29, 72000m, "Sales", true, "Sales Manager" },
                new object[] { 7, "Eve", "Wilson", "eve.wilson@example.com", 33, 78000m, "Marketing", true, "Marketing Manager" },
                new object[] { 8, "Frank", "Miller", "frank.miller@example.com", 27, 62000m, "Engineering", true, "Junior Developer" },
                new object[] { 9, "Grace", "Taylor", "", 31, 85000m, "Finance", true, "Financial Analyst" }, // Missing email for validation test
                new object[] { 10, "", "Anderson", "thomas.anderson@example.com", 150, 125000m, "IT", true, "Invalid age for validation test" } // Missing first name and invalid age
            };

            foreach (var row in sampleData)
            {
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        private void UpdateStatus(string message)
        {
            Dispatcher.Invoke(() =>
            {
                StatusTextBlock.Text = $"{DateTime.Now:HH:mm:ss} - {message}";
            });
        }

        private void UpdateValidationStatus(string message)
        {
            Dispatcher.Invoke(() =>
            {
                ValidationStatusTextBlock.Text = message;
            });
        }

        #endregion

        #region Event Handlers

        private void AdvancedDataGrid_ErrorOccurred(object sender, ComponentErrorEventArgs e)
        {
            UpdateStatus($"Component Error: {e.Operation} - {e.Exception.Message}");

            // Log to debug console
            System.Diagnostics.Debug.WriteLine($"AdvancedDataGrid Error: {e.Operation} - {e.Exception.Message}");

            // Optionally show message box for critical errors
            if (e.Operation.Contains("Initialize"))
            {
                MessageBox.Show($"Critical Error in {e.Operation}:\n{e.Exception.Message}",
                               "Component Error",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            // Cleanup
            if (_serviceProvider is IDisposable disposableProvider)
            {
                disposableProvider.Dispose();
            }

            base.OnClosed(e);
        }
    }
}