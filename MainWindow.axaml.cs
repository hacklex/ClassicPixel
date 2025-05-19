using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PixelEditorApp.ViewModels;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform.Storage;

namespace PixelEditorApp
{
    public partial class MainWindow : Window
    {
        private Point? _lastPoint;
        private Point? _selectionStartPoint;

        private MainViewModel? _currentSubscriptionViewModel;

        public MainWindow()
        {
            InitializeComponent();

            // Subscribe to ViewModel events
            DataContextChanged += OnDataContextChanged;
            OnDataContextChanged(this, null);
        }

        private void OnDataContextChanged(object? sender, EventArgs? args)
        {
            UnsubscribeFromViewModel(_currentSubscriptionViewModel);
            _currentSubscriptionViewModel = DataContext as MainViewModel;
            SubscribeToViewModel(_currentSubscriptionViewModel);
        }

        private void SubscribeToViewModel(MainViewModel? viewModel)
        {
            if (viewModel == null) return;
            viewModel.ExitRequested += OnViewModelOnExitRequested;
            viewModel.NewCanvasRequested += OnViewModelOnNewCanvasRequested;
            viewModel.OpenRequested += OnViewModelOnOpenRequested;
            viewModel.SaveRequested += OnViewModelOnSaveRequested;
        }

        private async void OnViewModelOnSaveRequested(object? o, FileOperationEventArgs fileOperationEventArgs)
        {
            try
            {
                await SaveFile();
            }
            catch
            {
                // Handle errors silently or add error logging
            }
        }

        private async void OnViewModelOnOpenRequested(object? o, FileOperationEventArgs fileOperationEventArgs)
        {
            try
            {
                await OpenFile();
            }
            catch
            {
                // Handle errors silently or add error logging
            }
        }

        private async void OnViewModelOnNewCanvasRequested(object? o,
            NewCanvasRequestEventArgs newCanvasRequestEventArgs)
        {
            try
            {
                await ShowNewCanvasDialog();
            }
            catch
            {
                // Handle errors silently or add error logging
            }
        }

        private void OnViewModelOnExitRequested(object? o, EventArgs eventArgs) => Close();

        private void UnsubscribeFromViewModel(MainViewModel? viewModel)
        {
            if (viewModel == null) return;
            viewModel.ExitRequested -= OnViewModelOnExitRequested;
            viewModel.NewCanvasRequested -= OnViewModelOnNewCanvasRequested;
            viewModel.OpenRequested -= OnViewModelOnOpenRequested;
            viewModel.SaveRequested -= OnViewModelOnSaveRequested;
        }

        private async Task ShowNewCanvasDialog()
        {
            var dialog = new NewCanvasDialog
            {
                DataContext = new NewCanvasDialogViewModel()
            };

            var viewModel = (NewCanvasDialogViewModel)dialog.DataContext;
            viewModel.DialogClosed += (sender, result) =>
            {
                if (result.Result)
                {
                    if (DataContext is MainViewModel mainViewModel)
                    {
                        mainViewModel.CreateNewCanvas(result.Width, result.Height);
                    }
                }

                dialog.Close();
            };

            await dialog.ShowDialog(this);
        }

        private async Task OpenFile()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Image",
                AllowMultiple = false,
                FileTypeFilter = new FilePickerFileType[]
                {
                    new("Image Files") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp" } },
                    new("All Files") { Patterns = new[] { "*.*" } }
                }
            });

            if (files.Count > 0)
            {
                if (DataContext is MainViewModel viewModel)
                {
                    using var stream = await files[0].OpenReadAsync();
                    viewModel.LoadFromStream(stream);
                }
            }
        }

        private async Task SaveFile()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Image",
                DefaultExtension = "png",
                FileTypeChoices = new FilePickerFileType[]
                {
                    new("PNG Files") { Patterns = new[] { "*.png" } },
                    new("All Files") { Patterns = new[] { "*.*" } }
                }
            });

            if (file != null)
            {
                if (DataContext is MainViewModel viewModel)
                {
                    using var stream = await file.OpenWriteAsync();
                    viewModel.SaveToStream(stream);
                }
            }
        }

        private void CanvasPointerMoved(object sender, PointerEventArgs e)
        {
            var position = e.GetPosition(sender as Canvas);
            var x = (int)position.X;
            var y = (int)position.Y;

            if (DataContext is MainViewModel viewModel)
            {
                viewModel.UpdatePositionCommand.Execute(new PixelEventArgs(x, y, false));

                if (e.GetCurrentPoint(sender as Visual).Properties.IsLeftButtonPressed ||
                    e.GetCurrentPoint(sender as Visual).Properties.IsRightButtonPressed)
                {
                    var isLeftButton = e.GetCurrentPoint(sender as Visual).Properties.IsLeftButtonPressed;

                    if (viewModel.IsPencilToolSelected && _lastPoint.HasValue)
                    {
                        viewModel.DrawPixelCommand.Execute(new PixelEventArgs(x, y, isLeftButton));
                    }
                    else if (viewModel.IsSelectionToolSelected && _selectionStartPoint.HasValue)
                    {
                        viewModel.SelectionUpdateCommand.Execute(new SelectionEventArgs(
                            (int)_selectionStartPoint.Value.X,
                            (int)_selectionStartPoint.Value.Y,
                            x, y));
                    }
                }

                _lastPoint = position;
            }
        }

        private void CanvasPointerPressed(object sender, PointerPressedEventArgs e)
        {
            var position = e.GetPosition(sender as Canvas);
            var x = (int)position.X;
            var y = (int)position.Y;

            if (DataContext is MainViewModel viewModel)
            {
                bool isLeftButton = e.GetCurrentPoint(sender as Visual).Properties.IsLeftButtonPressed;

                if (viewModel.IsPencilToolSelected)
                {
                    viewModel.DrawPixelCommand.Execute(new PixelEventArgs(x, y, isLeftButton));
                }
                else if (viewModel.IsSelectionToolSelected)
                {
                    _selectionStartPoint = position;
                    viewModel.SelectionStartCommand.Execute(new PixelEventArgs(x, y, isLeftButton));
                }
                else if (viewModel.IsFillToolSelected)
                {
                    viewModel.DrawPixelCommand.Execute(new PixelEventArgs(x, y, isLeftButton));
                }
            }
        }

        private void CanvasPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            var position = e.GetPosition(sender as Canvas);
            var x = (int)position.X;
            var y = (int)position.Y;

            if (DataContext is MainViewModel viewModel && _selectionStartPoint.HasValue && viewModel.IsSelectionToolSelected)
            {
                viewModel.SelectionEndCommand.Execute(new SelectionEventArgs(
                    (int)_selectionStartPoint.Value.X,
                    (int)_selectionStartPoint.Value.Y,
                    x, y));
                _selectionStartPoint = null;
            }

            _lastPoint = null;
        }
    }
}