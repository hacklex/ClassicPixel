using System;
using System.Windows.Input;

namespace PixelEditorApp.ViewModels
{
    public class NewCanvasDialogViewModel : ViewModelBase
    {
        private int _canvasWidth = 32;
        private int _canvasHeight = 32;

        public int CanvasWidth
        {
            get => _canvasWidth;
            set => SetProperty(ref _canvasWidth, value);
        }

        public int CanvasHeight
        {
            get => _canvasHeight;
            set => SetProperty(ref _canvasHeight, value);
        }

        public ICommand CreateCommand { get; }
        public ICommand CancelCommand { get; }

        public NewCanvasDialogViewModel()
        {
            CreateCommand = new RelayCommand(_ => OnCreate());
            CancelCommand = new RelayCommand(_ => OnCancel());
        }

        public event EventHandler<DialogResult>? DialogClosed;

        private void OnCreate()
        {
            DialogClosed?.Invoke(this, new DialogResult(true, CanvasWidth, CanvasHeight));
        }

        private void OnCancel()
        {
            DialogClosed?.Invoke(this, new DialogResult(false, 0, 0));
        }

        public class DialogResult
        {
            public bool Result { get; }
            public int Width { get; }
            public int Height { get; }

            public DialogResult(bool result, int width, int height)
            {
                Result = result;
                Width = width;
                Height = height;
            }
        }
    }
}