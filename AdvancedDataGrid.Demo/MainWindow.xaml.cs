// MainWindow.xaml.cs - UKÁŽKA SPRÁVNEHO POUŽITIA AdvancedDataGrid
// MainWindow.xaml.cs - UKÁŽKA SPRÁVNEHO POUŽITIA AdvancedDataGridControl WRAPPER
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RpaWpfComponents.AdvancedDataGrid;

namespace YourApplication
{
    public partial class MainWindow : Window
    {
        private AdvancedDataGridControl _advancedDataGrid;

        public MainWindow()
        {
            InitializeComponent();
            InitializeAdvancedDataGrid();
        }

        private async void InitializeAdvancedDataGrid()
        {
            try
            {
                // Vytvor komponent
                _advancedDataGrid = new AdvancedDataGridControl();

                // Subscribe na error events
                _advancedDataGrid.ErrorOccurred += OnGridErrorOccurred;

                // Pridaj do UI (predpokladajme že máš Grid alebo iný container)
                MainContainer.Children.Add(_advancedDataGrid);

                System.Diagnostics.Debug.WriteLine("🚀 Spúšťam inicializáciu AdvancedDataGrid...");

                // ==============================================
                // KROK 1-6: INICIALIZÁCIA S VALIDÁCIAMI NAJPRV!
                // ==============================================
                await InitializeGridWithValidations();

                // ==============================================  
                // KROK 7: NAČÍTANIE DÁT (až po inicializácii)
                // ==============================================
                await LoadSampleData();

                System.Diagnostics.Debug.WriteLine("✅ AdvancedDataGrid úspešne inicializovaný a pripravený!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba pri inicializácii: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// KROK 1-6: Definícia stĺpcov a validačných pravidiel
        /// DÔLEŽITÉ: Volá sa PRED načítaním dát!
        /// </summary>
        private async Task InitializeGridWithValidations()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== KROK 1-6: Definujem stĺpce a validácie ===");

                // 1. DEFINÍCIA STĹPCOV
                var columns = new List<ColumnDefinition>
                {
                    new()
                    {
                        Name = "Name",
                        DataType = typeof(string),
                        MinWidth = 150,
                        MaxWidth = 300
                    },
                    new()
                    {
                        Name = "Age",
                        DataType = typeof(int),
                        MinWidth = 80,
                        MaxWidth = 120
                    },
                    new()
                    {
                        Name = "Email",
                        DataType = typeof(string),
                        MinWidth = 200,
                        MaxWidth = 400
                    },
                    new()
                    {
                        Name = "ValidAlerts",
                        DataType = typeof(string),
                        MinWidth = 120,
                        MaxWidth = 200
                    },
                    new()
                    {
                        Name = "Salary",
                        DataType = typeof(decimal),
                        MinWidth = 100,
                        MaxWidth = 150
                    },
                    new()
                    {
                        Name = "DeleteAction",
                        DataType = typeof(string),
                        MinWidth = 120,
                        MaxWidth = 200
                    },
                    new()
                    {
                        Name = "Department",
                        DataType = typeof(string),
                        MinWidth = 120,
                        MaxWidth = 200
                    }

                };

                // 2. DEFINÍCIA VALIDAČNÝCH PRAVIDIEL
                var validationRules = new List<ValidationRule>
                {
                    // Name - povinné pole, minimálne 2 znaky
                    Validation.Required("Name", "Meno je povinné"),
                    Validation.Length("Name", 2, 50, "Meno musí mať 2-50 znakov"),

                    // Age - rozsah 18-65
                    Validation.Range("Age", 18, 65, "Vek musí byť medzi 18-65 rokmi"),

                    // Email - povinné pole
                    Validation.Required("Email", "Email je povinný"),

                    // Custom email validation
                    new ValidationRule
                    {
                        ColumnName = "Email",
                        ValidationFunction = (value, row) =>
                        {
                            var email = value?.ToString();
                            return !string.IsNullOrEmpty(email) && email.Contains("@") && email.Contains(".");
                        },
                        ErrorMessage = "Email musí mať platný formát",
                        RuleName = "Email_Format"
                    },

                    // Salary - minimálne 1000
                    Validation.Range("Salary", 1000, 999999, "Plat musí byť medzi 1000-999999"),

                    // Department - povinné pole
                    Validation.Required("Department", "Oddelenie je povinné"),

                    // Conditional validation - ak je vek > 50, plat musí byť > 3000
                    Validation.Conditional(
                        "Salary",
                        (value, row) =>
                        {
                            if (decimal.TryParse(value?.ToString(), out decimal salary))
                            {
                                return salary >= 3000;
                            }
                            return false;
                        },
                        row =>
                        {
                            var ageValue = row.GetValue("Age");
                            if (ageValue != null && int.TryParse(ageValue.ToString(), out int age))
                            {
                                return age > 50;
                            }
                            return false;
                        },
                        "Zamestnanci nad 50 rokov musia mať plat aspoň 3000€",
                        "Salary_SeniorEmployee"
                    )
                };

                System.Diagnostics.Debug.WriteLine($"📋 Definované: {columns.Count} stĺpcov, {validationRules.Count} validačných pravidiel");

                // 3. ⭐ NAJDÔLEŽITEJŠIE: INICIALIZÁCIA S VALIDÁCIAMI PRED DÁTAMI!
                await _advancedDataGrid.Initialize(columns, validationRules, new ThrottlingConfig
                {
                    TypingDelayMs = 100,              // Hlavný delay
                    PasteDelayMs = 50,                // Paste delay  
                    MaxConcurrentValidations = 5,     // Max súčasných validácií
                    IsEnabled = true                  // Zapnutý/vypnutý
                }, 100);

                System.Diagnostics.Debug.WriteLine("✅ Grid inicializovaný s validáciami - pripravený na dáta");
            }
            catch (Exception ex)
            {
                throw new Exception($"Chyba pri inicializácii stĺpcov a validácií: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// KROK 7: Načítanie dát (validácie sa aplikujú automaticky)
        /// </summary>
        private async Task LoadSampleData()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== KROK 7: Načítavam dáta s aplikovaním validácií ===");

                // Vytvor sample dáta
                var dataTable = CreateSampleDataTable();

                System.Diagnostics.Debug.WriteLine($"📊 Načítavam {dataTable.Rows.Count} záznamov...");

                // Načítaj dáta - validácie sa aplikujú real-time
                await _advancedDataGrid.LoadData(dataTable);

                System.Diagnostics.Debug.WriteLine("✅ Dáta načítané s real-time validáciou");
            }
            catch (Exception ex)
            {
                throw new Exception($"Chyba pri načítavaní dát: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Vytvorenie sample dát pre demonštráciu
        /// </summary>
        private DataTable CreateSampleDataTable()
        {
            var dataTable = new DataTable();

            // Pridaj stĺpce
            dataTable.Columns.Add("Name", typeof(string));
            dataTable.Columns.Add("Age", typeof(int));
            dataTable.Columns.Add("Email", typeof(string));
            dataTable.Columns.Add("Salary", typeof(decimal));
            dataTable.Columns.Add("Department", typeof(string));

            // Pridaj sample dáta (niektoré validné, niektoré nevalidné pre testovanie)
            var sampleData = new[]
            {
                // Validné záznamy
                new object[] { "Ján Novák", 25, "jan.novak@email.com", 2500m, "IT" },
                new object[] { "Mária Svobodová", 30, "maria.svoboda@email.com", 3200m, "HR" },
                new object[] { "Peter Dvořák", 45, "peter.dvorak@email.com", 4500m, "Finance" },
                
                // Nevalidné záznamy pre testovanie validácie
                new object[] { "", 17, "invalid-email", 500m, "" },                    // Všetko nevalidné
                new object[] { "A", 70, "missing@", 200m, "Test" },                   // Krátke meno, vysoký vek, zlý email, nízky plat
                new object[] { "Starý Zamestnanec", 55, "senior@company.com", 2500m, "Management" }, // Senior s nízkym platom
                new object[] { "Mladý Programátor", 22, "junior@company.com", 1500m, "IT" },
                
                // Prázdne záznamy
                new object[] { "", null, "", null, "" },
                new object[] { "Iba Meno", null, "", null, "" }
            };

            foreach (var row in sampleData)
            {
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        #region Event Handlers a Testing Methods

        private void OnGridErrorOccurred(object sender, ComponentError e)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Grid Error: {e.Operation} - {e.Exception.Message}");

            // V produkčnej aplikácii by ste mohli zobraziť user-friendly message
            Dispatcher.BeginInvoke(new Action(() =>
            {
                MessageBox.Show($"Chyba v gridu: {e.Exception.Message}", "Grid Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }));
        }

        // UKÁŽKY ĎALŠÍCH FUNKCIÍ
        private async void ValidateAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var isValid = await _advancedDataGrid.ValidateAll();
                MessageBox.Show(isValid ? "Všetky dáta sú validné!" : "Nájdené nevalidné dáta!",
                    "Validácia", MessageBoxButton.OK,
                    isValid ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba pri validácii: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ClearDataButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _advancedDataGrid.ClearAllData();
                MessageBox.Show("Všetky dáta vymazané!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba pri mazaní: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RemoveInvalidRowsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Vymaž zamestnancov mladších ako 20 alebo starších ako 60
                var customRules = new List<ValidationRule>
                {
                    Validation.Range("Age", 20, 60, "Vek mimo rozsahu 20-60"),
                    Validation.Required("Email", "Chýba email")
                };

                var removedCount = await _advancedDataGrid.RemoveRowsByValidation(customRules);
                MessageBox.Show($"Vymazané {removedCount} nevalidných riadkov!", "Custom Remove",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba pri custom remove: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RemoveEmptyRowsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _advancedDataGrid.RemoveEmptyRows();
                MessageBox.Show("Prázdne riadky vymazané!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba pri mazaní prázdnych riadkov: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExportDataButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dataTable = await _advancedDataGrid.ExportToDataTable();
                MessageBox.Show($"Export úspešný! Počet riadkov: {dataTable.Rows.Count}", "Export",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Tu by ste mohli uložiť do súboru, databázy, atď.
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba pri exporte: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // UKÁŽKA: Načítanie dát z databázy/API
        private async void LoadFromDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Simulácia načítania z databázy
                var databaseData = await SimulateLoadFromDatabase();

                // Načítaj dáta
                await _advancedDataGrid.LoadData(databaseData);

                MessageBox.Show("Dáta z databázy načítané!", "Database Load",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba pri načítaní z databázy: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<DataTable> SimulateLoadFromDatabase()
        {
            // Simulácia async načítania z databázy
            await Task.Delay(500);

            var dataTable = new DataTable();
            dataTable.Columns.Add("Name", typeof(string));
            dataTable.Columns.Add("Age", typeof(int));
            dataTable.Columns.Add("Email", typeof(string));
            dataTable.Columns.Add("Salary", typeof(decimal));
            dataTable.Columns.Add("Department", typeof(string));

            // Simulované dáta z DB
            var dbData = new[]
            {
                new object[] { "Database User 1", 28, "user1@db.com", 3000m, "Development" },
                new object[] { "Database User 2", 35, "user2@db.com", 3500m, "Testing" },
                new object[] { "Database User 3", 42, "user3@db.com", 4200m, "DevOps" }
            };

            foreach (var row in dbData)
            {
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        // NOVÉ FUNKCIE PRE UKÁŽKU WRAPPER API + KONFIGURÁCIA
        private void ConfigureDependencyInjectionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ukážka konfigurácie DI (obvykle v App.xaml.cs)
                var services = new ServiceCollection();
                services.AddAdvancedDataGrid(); // Extension method z wrappera
                services.AddLogging();

                var serviceProvider = services.BuildServiceProvider();
                AdvancedDataGridControl.Configuration.ConfigureServices(serviceProvider);

                MessageBox.Show("Dependency Injection configured!", "DI Config",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba pri konfigurácii DI: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfigureLoggingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ukážka konfigurácie logovania
                using var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddConsole().AddDebug().SetMinimumLevel(LogLevel.Debug);
                });

                AdvancedDataGridControl.Configuration.ConfigureLogging(loggerFactory);
                AdvancedDataGridControl.Configuration.SetDebugLogging(true);

                MessageBox.Show("Logging configured!", "Logging Config",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba pri konfigurácii logovania: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void LoadFromDictionaryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ukážka načítania z List<Dictionary>
                var data = new List<Dictionary<string, object>>
                {
                    new() { {"Name", "Test User 1"}, {"Age", 25}, {"Email", "test1@example.com"}, {"Salary", 2800m}, {"Department", "QA"} },
                    new() { {"Name", "Test User 2"}, {"Age", 30}, {"Email", "test2@example.com"}, {"Salary", 3200m}, {"Department", "Dev"} }
                };

                await _advancedDataGrid.LoadData(data);
                MessageBox.Show("Dáta z dictionary načítané!", "Dictionary Load",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba pri načítaní z dictionary: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RemoveByConditionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ukážka odstránenia riadkov podľa podmienky
                await _advancedDataGrid.RemoveRowsByCondition("Age", age =>
                {
                    if (age != null && int.TryParse(age.ToString(), out int ageValue))
                        return ageValue < 25; // Odstráni všetkých mladších ako 25
                    return false;
                });

                MessageBox.Show("Riadky s vekom < 25 odstránené!", "Remove by Condition",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba pri remove by condition: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Cleanup

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Cleanup
                if (_advancedDataGrid != null)
                {
                    _advancedDataGrid.ErrorOccurred -= OnGridErrorOccurred;
                    _advancedDataGrid.Reset();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cleanup error: {ex.Message}");
            }

            base.OnClosed(e);
        }

        #endregion
    }
}

/* 
==============================================
SÚHRN ZMIEN PRE WRAPPER:
==============================================

1. USING STATEMENTS:
   ✅ Len: using RpaWpfComponents.AdvancedDataGrid;
   ❌ Odstránené: Views, Models, Helpers, Events

2. TYPY ZMENY:
   ✅ AdvancedDataGridView → AdvancedDataGridControlControl
   ✅ ColumnDefinitionModel → ColumnDefinition
   ✅ ValidationRuleModel → ValidationRule
   ✅ ComponentErrorEventArgs → ComponentError
   ✅ ThrottlingConfiguration → ThrottlingConfig

3. API METHODS ZMENY:
   ✅ ValidationHelper.* → Validation.*
   ✅ InitializeAsync() → Initialize()
   ✅ LoadDataAsync() → LoadData()
   ✅ ValidateAllRowsAsync() → ValidateAll()
   ✅ ClearAllDataAsync() → ClearAllData()
   ✅ RemoveEmptyRowsAsync() → RemoveEmptyRows()
   ✅ ExportDataAsync() → ExportToDataTable()
   ✅ RemoveRowsByCustomValidationAsync() → RemoveRowsByValidation()

4. KONFIGURÁCIA ZMENY:
   ✅ DependencyInjectionConfig.* → Configuration.*
   ✅ services.AddAdvancedDataGrid() (wrapper extension)
   ✅ Configuration.ConfigureServices()
   ✅ Configuration.ConfigureLogging()
   ✅ Configuration.SetDebugLogging()

4. VALIDAČNÉ FUNKCIE:
   ✅ row.GetCell("Age") → row.GetValue("Age")
   ✅ GridDataRow namiesto DataRow

==============================================
WRAPPER API TERAZ ÚPLNE FUNKČNÉ! 🚀
==============================================
*/


/*using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using RpaWpfComponents.AdvancedDataGrid.Views;
using RpaWpfComponents.AdvancedDataGrid.Models;
using RpaWpfComponents.AdvancedDataGrid.Helpers;
using RpaWpfComponents.AdvancedDataGrid.Events;

namespace YourApplication
{
    public partial class MainWindow : Window
    {
        private AdvancedDataGridView _advancedDataGrid;

        public MainWindow()
        {
            InitializeComponent();
            InitializeAdvancedDataGrid();
        }

        private async void InitializeAdvancedDataGrid()
        {
            try
            {
                // Vytvor komponent
                _advancedDataGrid = new AdvancedDataGridView();

                // Subscribe na error events
                _advancedDataGrid.ErrorOccurred += OnGridErrorOccurred;

                // Pridaj do UI (predpokladajme že máš Grid alebo iný container)
                MainContainer.Children.Add(_advancedDataGrid);

                System.Diagnostics.Debug.WriteLine("🚀 Spúšťam inicializáciu AdvancedDataGrid...");

                // ==============================================
                // KROK 1-6: INICIALIZÁCIA S VALIDÁCIAMI NAJPRV!
                // ==============================================
                await InitializeGridWithValidations();

                // ==============================================  
                // KROK 7: NAČÍTANIE DÁT (až po inicializácii)
                // ==============================================
                await LoadSampleData();

                System.Diagnostics.Debug.WriteLine("✅ AdvancedDataGrid úspešne inicializovaný a pripravený!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba pri inicializácii: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
      
        /// <summary>
        /// KROK 1-6: Definícia stĺpcov a validačných pravidiel
        /// DÔLEŽITÉ: Volá sa PRED načítaním dát!
        /// </summary>
        private async Task InitializeGridWithValidations()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== KROK 1-6: Definujem stĺpce a validácie ===");

                // 1. DEFINÍCIA STĹPCOV
                var columns = new List<ColumnDefinitionModel>
                {
                    new()
                    {
                        Name = "Name",
                        DataType = typeof(string),
                        MinWidth = 150,
                        MaxWidth = 300
                    },
                    new()
                    {
                        Name = "Age",
                        DataType = typeof(int),
                        MinWidth = 80,
                        MaxWidth = 120
                    },
                    new()
                    {
                        Name = "Email",
                        DataType = typeof(string),
                        MinWidth = 200,
                        MaxWidth = 400
                    },
                    new()
                    {
                        Name = "ValidAlerts",
                        DataType = typeof(string),
                        MinWidth = 120,
                        MaxWidth = 200
                    },
                    new()
                    {
                        Name = "Salary",
                        DataType = typeof(decimal),
                        MinWidth = 100,
                        MaxWidth = 150
                    },
                    new()
                    {
                        Name = "DeleteAction",
                        DataType = typeof(string),
                        MinWidth = 120,
                        MaxWidth = 200
                    },
                    new()
                    {
                        Name = "Department",
                        DataType = typeof(string),
                        MinWidth = 120,
                        MaxWidth = 200
                    }
                    
                };

                // 2. DEFINÍCIA VALIDAČNÝCH PRAVIDIEL
                var validationRules = new List<ValidationRuleModel>
                {
                    // Name - povinné pole, minimálne 2 znaky
                    ValidationHelper.CreateRequiredRule("Name", "Meno je povinné"),
                    ValidationHelper.CreateLengthRule("Name", 2, 50, "Meno musí mať 2-50 znakov"),

                    // Age - rozsah 18-65
                    ValidationHelper.CreateRangeRule("Age", 18, 65, "Vek musí byť medzi 18-65 rokmi"),

                    // Email - povinné pole
                    ValidationHelper.CreateRequiredRule("Email", "Email je povinný"),

                    // Custom email validation
                    new ValidationRuleModel
                    {
                        ColumnName = "Email",
                        ValidationFunction = (value, row) =>
                        {
                            var email = value?.ToString();
                            return !string.IsNullOrEmpty(email) && email.Contains("@") && email.Contains(".");
                        },
                        ErrorMessage = "Email musí mať platný formát",
                        RuleName = "Email_Format"
                    },

                    // Salary - minimálne 1000
                    ValidationHelper.CreateRangeRule("Salary", 1000, 999999, "Plat musí byť medzi 1000-999999"),

                    // Department - povinné pole
                    ValidationHelper.CreateRequiredRule("Department", "Oddelenie je povinné"),

                    // Conditional validation - ak je vek > 50, plat musí byť > 3000
                    ValidationHelper.CreateConditionalRule(
                        "Salary",
                        (value, row) =>
                        {
                            if (decimal.TryParse(value?.ToString(), out decimal salary))
                            {
                                return salary >= 3000;
                            }
                            return false;
                        },
                        row =>
                        {
                            var ageCell = row.GetCell("Age");
                            if (ageCell?.Value != null && int.TryParse(ageCell.Value.ToString(), out int age))
                            {
                                return age > 50;
                            }
                            return false;
                        },
                        "Zamestnanci nad 50 rokov musia mať plat aspoň 3000€",
                        "Salary_SeniorEmployee"
                    )
                };

                System.Diagnostics.Debug.WriteLine($"📋 Definované: {columns.Count} stĺpcov, {validationRules.Count} validačných pravidiel");

                // 3. ⭐ NAJDÔLEŽITEJŠIE: INICIALIZÁCIA S VALIDÁCIAMI PRED DÁTAMI!
                await _advancedDataGrid.InitializeAsync(columns, validationRules, new ThrottlingConfiguration
                {
                    TypingDelayMs = 100,              // Hlavný delay
                    PasteDelayMs = 50,                // Paste delay  
                    MaxConcurrentValidations = 5,     // Max súčasných validácií
                    IsEnabled = true                  // Zapnutý/vypnutý
                },100);

                System.Diagnostics.Debug.WriteLine("✅ Grid inicializovaný s validáciami - pripravený na dáta");
            }
            catch (Exception ex)
            {
                throw new Exception($"Chyba pri inicializácii stĺpcov a validácií: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// KROK 7: Načítanie dát (validácie sa aplikujú automaticky)
        /// </summary>
        private async Task LoadSampleData()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== KROK 7: Načítavam dáta s aplikovaním validácií ===");

                // Vytvor sample dáta
                var dataTable = CreateSampleDataTable();

                System.Diagnostics.Debug.WriteLine($"📊 Načítavam {dataTable.Rows.Count} záznamov...");

                // Načítaj dáta - validácie sa aplikujú real-time
                await _advancedDataGrid.LoadDataAsync(dataTable);

                System.Diagnostics.Debug.WriteLine("✅ Dáta načítané s real-time validáciou");
            }
            catch (Exception ex)
            {
                throw new Exception($"Chyba pri načítavaní dát: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Vytvorenie sample dát pre demonštráciu
        /// </summary>
        private DataTable CreateSampleDataTable()
        {
            var dataTable = new DataTable();

            // Pridaj stĺpce
            dataTable.Columns.Add("Name", typeof(string));
            dataTable.Columns.Add("Age", typeof(int));
            dataTable.Columns.Add("Email", typeof(string));
            dataTable.Columns.Add("Salary", typeof(decimal));
            dataTable.Columns.Add("Department", typeof(string));

            // Pridaj sample dáta (niektoré validné, niektoré nevalidné pre testovanie)
            var sampleData = new[]
            {
                // Validné záznamy
                new object[] { "Ján Novák", 25, "jan.novak@email.com", 2500m, "IT" },
                new object[] { "Mária Svobodová", 30, "maria.svoboda@email.com", 3200m, "HR" },
                new object[] { "Peter Dvořák", 45, "peter.dvorak@email.com", 4500m, "Finance" },
                
                // Nevalidné záznamy pre testovanie validácie
                new object[] { "", 17, "invalid-email", 500m, "" },                    // Všetko nevalidné
                new object[] { "A", 70, "missing@", 200m, "Test" },                   // Krátke meno, vysoký vek, zlý email, nízky plat
                new object[] { "Starý Zamestnanec", 55, "senior@company.com", 2500m, "Management" }, // Senior s nízkym platom
                new object[] { "Mladý Programátor", 22, "junior@company.com", 1500m, "IT" },
                
                // Prázdne záznamy
                new object[] { "", null, "", null, "" },
                new object[] { "Iba Meno", null, "", null, "" }
            };

            foreach (var row in sampleData)
            {
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        #region Event Handlers a Testing Methods

        private void OnGridErrorOccurred(object sender, ComponentErrorEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Grid Error: {e.Operation} - {e.Exception.Message}");

            // V produkčnej aplikácii by ste mohli zobraziť user-friendly message
            Dispatcher.BeginInvoke(new Action(() =>
            {
                MessageBox.Show($"Chyba v gridu: {e.Exception.Message}", "Grid Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }));
        }

        // UKÁŽKY ĎALŠÍCH FUNKCIÍ
        private async void ValidateAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var isValid = await _advancedDataGrid.ValidateAllRowsAsync();
                MessageBox.Show(isValid ? "Všetky dáta sú validné!" : "Nájdené nevalidné dáta!",
                    "Validácia", MessageBoxButton.OK,
                    isValid ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba pri validácii: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ClearDataButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _advancedDataGrid.ClearAllDataAsync();
                MessageBox.Show("Všetky dáta vymazané!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba pri mazaní: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RemoveInvalidRowsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Vymaž zamestnancov mladších ako 20 alebo starších ako 60
                var customRules = new List<ValidationRuleModel>
                {
                    ValidationHelper.CreateRangeRule("Age", 20, 60, "Vek mimo rozsahu 20-60"),
                    ValidationHelper.CreateRequiredRule("Email", "Chýba email")
                };

                var removedCount = await _advancedDataGrid.RemoveRowsByCustomValidationAsync(customRules);
                MessageBox.Show($"Vymazané {removedCount} nevalidných riadkov!", "Custom Remove",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba pri custom remove: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RemoveEmptyRowsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _advancedDataGrid.RemoveEmptyRowsAsync();
                MessageBox.Show("Prázdne riadky vymazané!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba pri mazaní prázdnych riadkov: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExportDataButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dataTable = await _advancedDataGrid.ExportDataAsync();
                MessageBox.Show($"Export úspešný! Počet riadkov: {dataTable.Rows.Count}", "Export",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Tu by ste mohli uložiť do súboru, databázy, atď.
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba pri exporte: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // UKÁŽKA: Načítanie dát z databázy/API
        private async void LoadFromDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Simulácia načítania z databázy
                var databaseData = await SimulateLoadFromDatabase();

                // Ak grid nie je inicializovaný, najprv ho inicializuj
                if (!_advancedDataGrid.ViewModel?.IsInitialized == true)
                {
                    await InitializeGridWithValidations();
                }

                // Načítaj dáta
                await _advancedDataGrid.LoadDataAsync(databaseData);

                MessageBox.Show("Dáta z databázy načítané!", "Database Load",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba pri načítaní z databázy: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<DataTable> SimulateLoadFromDatabase()
        {
            // Simulácia async načítania z databázy
            await Task.Delay(500);

            var dataTable = new DataTable();
            dataTable.Columns.Add("Name", typeof(string));
            dataTable.Columns.Add("Age", typeof(int));
            dataTable.Columns.Add("Email", typeof(string));
            dataTable.Columns.Add("Salary", typeof(decimal));
            dataTable.Columns.Add("Department", typeof(string));

            // Simulované dáta z DB
            var dbData = new[]
            {
                new object[] { "Database User 1", 28, "user1@db.com", 3000m, "Development" },
                new object[] { "Database User 2", 35, "user2@db.com", 3500m, "Testing" },
                new object[] { "Database User 3", 42, "user3@db.com", 4200m, "DevOps" }
            };

            foreach (var row in dbData)
            {
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        #endregion

        #region Cleanup

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Cleanup
                if (_advancedDataGrid != null)
                {
                    _advancedDataGrid.ErrorOccurred -= OnGridErrorOccurred;
                    _advancedDataGrid.Reset();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cleanup error: {ex.Message}");
            }

            base.OnClosed(e);
        }

        #endregion
    }
}*/

/* 
==============================================
SÚHRN SPRÁVNEHO POUŽITIA:
==============================================

1. INICIALIZÁCIA (KROK 1-6):
   ✅ Najprv definuj stĺpce
   ✅ Potom definuj validačné pravidlá  
   ✅ Zavolaj InitializeAsync(columns, validationRules)

2. NAČÍTANIE DÁT (KROK 7):
   ✅ Až PO inicializácii volaj LoadDataAsync(dataTable)
   ✅ Validácie sa aplikujú real-time automaticky

3. FUNKČNOSTI:
   ✅ Real-time validácia pri písaní/paste
   ✅ Bunka ↔ Mirror Editor synchronizácia
   ✅ ESC = reset na originálne hodnoty
   ✅ Custom remove podľa vlastných pravidiel
   ✅ Export, Clear, Remove empty rows

4. CHYBNÉ POUŽITIE:
   ❌ LoadDataAsync() PRED InitializeAsync()
   ❌ Validácie definované PO načítaní dát
   ❌ Chýbajúca error handling

==============================================
VŠETKY FUNKCIE TERAZ FUNGUJÚ SPRÁVNE! 🚀
==============================================
*/