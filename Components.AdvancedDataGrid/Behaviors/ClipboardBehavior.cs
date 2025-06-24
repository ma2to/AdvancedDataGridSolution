// ===========================================
// RpaWpfComponents/AdvancedDataGrid/Behaviors/ClipboardBehavior.cs
// ===========================================
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace RpaWpfComponents.AdvancedDataGrid.Behaviors
{
    public class ClipboardBehavior : Behavior<DataGrid>
    {
        public static readonly DependencyProperty CopyCommandProperty =
            DependencyProperty.Register(nameof(CopyCommand), typeof(ICommand), typeof(ClipboardBehavior));

        public static readonly DependencyProperty PasteCommandProperty =
            DependencyProperty.Register(nameof(PasteCommand), typeof(ICommand), typeof(ClipboardBehavior));

        public ICommand CopyCommand
        {
            get => (ICommand)GetValue(CopyCommandProperty);
            set => SetValue(CopyCommandProperty, value);
        }

        public ICommand PasteCommand
        {
            get => (ICommand)GetValue(PasteCommandProperty);
            set => SetValue(PasteCommandProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.C:
                        if (CopyCommand?.CanExecute(null) == true)
                        {
                            CopyCommand.Execute(null);
                            e.Handled = true;
                        }
                        break;
                    case Key.V:
                        if (PasteCommand?.CanExecute(null) == true)
                        {
                            PasteCommand.Execute(null);
                            e.Handled = true;
                        }
                        break;
                }
            }
        }
    }
}