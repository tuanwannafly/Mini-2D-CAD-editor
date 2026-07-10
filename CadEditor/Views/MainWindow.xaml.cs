using System.Windows;
using System.Windows.Input;
using CadEditor.Models;
using CadEditor.ViewModels;

namespace CadEditor.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    private void DrawingCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(DrawingCanvas);
        var point = new Point2D(pos.X, pos.Y);

        if (e.ClickCount >= 2)
        {
            ViewModel.FinishPolygon();
            return;
        }

        ViewModel.OnCanvasMouseDown(point);
        DrawingCanvas.CaptureMouse();
    }

    private void DrawingCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(DrawingCanvas);
        ViewModel.OnCanvasMouseMove(new Point2D(pos.X, pos.Y));
    }

    private void DrawingCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(DrawingCanvas);
        ViewModel.OnCanvasMouseUp(new Point2D(pos.X, pos.Y));
        DrawingCanvas.ReleaseMouseCapture();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                ViewModel.FinishPolygon();
                break;
            case Key.Escape:
                ViewModel.CancelPolygon();
                break;
        }
    }
}