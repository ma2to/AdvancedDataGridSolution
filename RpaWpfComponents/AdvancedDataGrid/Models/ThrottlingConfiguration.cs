// ===========================================
// RpaWpfComponents/AdvancedDataGrid/Models/ThrottlingConfiguration.cs - KOMPLETNÁ NÁHRADA
// ===========================================
using System;

namespace RpaWpfComponents.AdvancedDataGrid.Models
{
    /// <summary>
    /// Konfigurácia pre throttling real-time validácie
    /// POZNÁMKA: InitialRowCount je teraz nastavovaný separátne pri inicializácii
    /// </summary>
    public class ThrottlingConfiguration
    {
        /// <summary>
        /// Delay pre real-time validáciu počas písania (default: 100ms)
        /// </summary>
        public int TypingDelayMs { get; set; } = 100;

        /// <summary>
        /// Delay pre validáciu po paste operácii (default: 50ms)
        /// </summary>
        public int PasteDelayMs { get; set; } = 50;

        /// <summary>
        /// Delay pre batch validáciu všetkých riadkov (default: 200ms)
        /// </summary>
        public int BatchValidationDelayMs { get; set; } = 200;

        /// <summary>
        /// Maximálny počet súčasných validácií (default: 5)
        /// </summary>
        public int MaxConcurrentValidations { get; set; } = 5;

        /// <summary>
        /// Či je throttling povolený (default: true)
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Delay pre complex validácie (DB calls, API calls) (default: 300ms)
        /// </summary>
        public int ComplexValidationDelayMs { get; set; } = 300;

        /// <summary>
        /// Default konfigurácia s 100ms delay
        /// </summary>
        public static ThrottlingConfiguration Default => new ThrottlingConfiguration();

        /// <summary>
        /// Fast konfigurácia pre jednoduché validácie
        /// </summary>
        public static ThrottlingConfiguration Fast => new ThrottlingConfiguration
        {
            TypingDelayMs = 50,
            PasteDelayMs = 25,
            BatchValidationDelayMs = 100,
            ComplexValidationDelayMs = 150
        };

        /// <summary>
        /// Slow konfigurácia pre complex validácie alebo pomalé systémy
        /// </summary>
        public static ThrottlingConfiguration Slow => new ThrottlingConfiguration
        {
            TypingDelayMs = 250,
            PasteDelayMs = 100,
            BatchValidationDelayMs = 500,
            ComplexValidationDelayMs = 750
        };

        /// <summary>
        /// Large konfigurácia pre veľké datasety
        /// </summary>
        public static ThrottlingConfiguration Large => new ThrottlingConfiguration
        {
            TypingDelayMs = 150,
            PasteDelayMs = 75,
            BatchValidationDelayMs = 400,
            ComplexValidationDelayMs = 500
        };

        /// <summary>
        /// Small konfigurácia pre malé datasety
        /// </summary>
        public static ThrottlingConfiguration Small => new ThrottlingConfiguration
        {
            TypingDelayMs = 75,
            PasteDelayMs = 35,
            BatchValidationDelayMs = 150,
            ComplexValidationDelayMs = 200
        };

        /// <summary>
        /// Disabled throttling - okamžitá validácia
        /// </summary>
        public static ThrottlingConfiguration Disabled => new ThrottlingConfiguration
        {
            IsEnabled = false,
            TypingDelayMs = 0,
            PasteDelayMs = 0,
            BatchValidationDelayMs = 0,
            ComplexValidationDelayMs = 0
        };

        /// <summary>
        /// Custom konfigurácia s vlastným typing delay
        /// </summary>
        public static ThrottlingConfiguration Custom(int typingDelayMs)
        {
            return new ThrottlingConfiguration
            {
                TypingDelayMs = typingDelayMs,
                PasteDelayMs = Math.Max(10, typingDelayMs / 2),
                BatchValidationDelayMs = typingDelayMs * 2,
                ComplexValidationDelayMs = typingDelayMs * 3
            };
        }

        /// <summary>
        /// Validácia konfigurácie
        /// </summary>
        public void Validate()
        {
            if (TypingDelayMs < 0)
                throw new ArgumentException("TypingDelayMs must be >= 0");

            if (PasteDelayMs < 0)
                throw new ArgumentException("PasteDelayMs must be >= 0");

            if (BatchValidationDelayMs < 0)
                throw new ArgumentException("BatchValidationDelayMs must be >= 0");

            if (MaxConcurrentValidations < 1)
                throw new ArgumentException("MaxConcurrentValidations must be >= 1");
        }
    }
}