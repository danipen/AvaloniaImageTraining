using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace AvaloniaImageTraining
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DragDrop.SetAllowDrop(this, true);

            AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
            AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
            AddHandler(DragDrop.DropEvent, OnDrop);

            mImagePanel = new ImagePanel();
            Content = mImagePanel;
        }

        void OnDragEnter(object? sender, DragEventArgs e)
        {
            Background = Brushes.LightGray;
        }

        void OnDragLeave(object? sender, RoutedEventArgs e)
        {
            Background = Brushes.Transparent;
        }

        void OnDrop(object? sender, DragEventArgs e)
        {
            var files = e.Data.GetFileNames();

            if (files == null)
                return;

            IList<string> fileList = files.ToList();

            if (fileList.Count == 0)
                return;

            string file = fileList[0];

            using (var stream = System.IO.File.OpenRead(file))
            {
                var bitmap = WriteableBitmap.Decode(stream);
                Width = bitmap.Size.Width;
                Height = bitmap.Size.Height;

                mImagePanel.SetImage(bitmap);
            }
        }

        class ImagePanel : UserControl
        {
            public void SetImage(WriteableBitmap image)
            {
                mImage = image;
                mImageCopy = new WriteableBitmap(
                    mImage.PixelSize,
                    mImage.Dpi,
                    Avalonia.Platform.PixelFormat.Bgra8888,
                    Avalonia.Platform.AlphaFormat.Premul);
            }

            protected override void OnPointerMoved(PointerEventArgs e)
            {
                base.OnPointerMoved(e);

                mLastMousePoint = e.GetPosition(this);

                InvalidateVisual();
            }

            public unsafe override void Render(DrawingContext context)
            {
                base.Render(context);

                if (mImage == null)
                    return;

                ReDrawImage();

                context.DrawImage(mImageCopy, new Rect(0, 0, mImageCopy.PixelSize.Width, mImageCopy.PixelSize.Height));
            }

            unsafe void ReDrawImage()
            {
                using (var frameBuffer = mImage.Lock())
                using (var copyFrameBuffer = mImageCopy.Lock())
                {
                    byte* buffer = (byte*)frameBuffer.Address.ToPointer();
                    byte* copyBuffer = (byte*)copyFrameBuffer.Address.ToPointer();

                    for (int x = 0; x < mImage.Size.Width; x++)
                    {
                        for (int y = 0; y < mImage.Size.Height; y++)
                        {
                            int loc = (int)(x + y * mImage.Size.Width);

                            byte b = buffer[loc * 4];
                            byte g = buffer[loc * 4 + 1];
                            byte r = buffer[loc * 4 + 2];
                            byte a = buffer[loc * 4 + 3];

                            int brightness = Brightness(r, g, b);

                            double mapMousePosition = mLastMousePoint.X.Map(0, Bounds.Width, 0, 255);

                            if (brightness > mapMousePosition)
                            {
                                copyBuffer[loc * 4] = 0;
                                copyBuffer[loc * 4 + 1] = 0;
                                copyBuffer[loc * 4 + 2] = 0;
                                copyBuffer[loc * 4 + 3] = a;
                            }
                            else
                            {
                                copyBuffer[loc * 4] = 255;
                                copyBuffer[loc * 4 + 1] = 255;
                                copyBuffer[loc * 4 + 2] = 255;
                                copyBuffer[loc * 4 + 3] = a;
                            }
                        }
                    }
                }
            }

            private static byte Brightness(byte r, byte g, byte b)
            {
                return (byte)((r + g + b) / 3);
            }

            WriteableBitmap mImage;
            WriteableBitmap mImageCopy;
            Point mLastMousePoint;
        }

        ImagePanel mImagePanel;
    }

    static class Extensions
    {
        public static double Map(this double value, double fromSource, double toSource, double fromTarget, double toTarget)
        {
            return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
        }
    }
}