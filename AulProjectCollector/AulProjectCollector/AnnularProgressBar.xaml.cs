using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AnnularUI
{
    /// <summary>
    /// Interaction logic for CProgressBar.xaml
    /// </summary>
    public partial class AnnularProgressBar : UserControl
    {
        public AnnularProgressBar()
        {
            InitializeComponent();
        }

        public new static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register("Background", typeof(Brush), typeof(AnnularProgressBar), new FrameworkPropertyMetadata(Brushes.LightGray));

        public new Brush Background
        {
            get
            {
                return (Brush)GetValue(BackgroundProperty);
            }
            set
            {
                SetValue(BackgroundProperty, value);
            }
        }

        public new static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register("Foreground", typeof(Brush), typeof(AnnularProgressBar), new FrameworkPropertyMetadata(Brushes.DeepSkyBlue));

        public new Brush Foreground
        {
            get
            {
                return (Brush)GetValue(ForegroundProperty);
            }
            set
            {
                SetValue(ForegroundProperty, value);
            }
        }

        public static readonly DependencyProperty TrackWidthProperty = DependencyProperty.Register("TrackWidth", typeof(double), typeof(AnnularProgressBar), new FrameworkPropertyMetadata(20d));

        public double TrackWidth
        {
            get
            {
                return (double)GetValue(TrackWidthProperty);
            }
            set
            {
                SetValue(TrackWidthProperty, value);
            }
        }

        public static readonly DependencyProperty DisplayedValueProperty = DependencyProperty.Register("DisplayedValue", typeof(double), typeof(AnnularProgressBar), new FrameworkPropertyMetadata(0d, new PropertyChangedCallback(DisplayedValueChangedCallback)));

        public double DisplayedValue
        {
            get
            {
                return (double)GetValue(DisplayedValueProperty);
            }
            private set
            {
                SetValue(DisplayedValueProperty, value);
            }
        }

        private static void DisplayedValueChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AnnularProgressBar t = (AnnularProgressBar)d;
            double value = (double)e.NewValue;
            UIElement[] layers;

            if (value < 0.25)
            {
                layers = new UIElement[] { t.HeadEllipse, t.PathL, t.PathR, t.TailEllipse };
            }
            else if(value < 0.50)
            {
                layers = new UIElement[] { t.TailEllipse, t.HeadEllipse, t.PathL, t.PathR };
            }
            else if (value < 0.75)
            {
                layers = new UIElement[] { t.TailEllipse, t.HeadEllipse, t.PathL, t.PathR };
            }
            else
            {
                layers = new UIElement[] { t.PathR, t.TailEllipse, t.HeadEllipse, t.PathL };
            }

            for(int i = 0; i < layers.Length; i++)
            {
                Panel.SetZIndex(layers[i], i);
            }
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(double), typeof(AnnularProgressBar), new FrameworkPropertyMetadata(-1d, new PropertyChangedCallback(ValueChangedCallback)));

        public double Value
        {
            get
            {
                return (double)GetValue(ValueProperty);
            }
            set
            {
                SetValue(ValueProperty, value);
            }
        }

        private static void ValueChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AnnularProgressBar t = (AnnularProgressBar)d;
            Storyboard valueStoryboard = new Storyboard();

            if ((double)e.NewValue == -1)
            {
                DoubleAnimation valueAnimation = new DoubleAnimation(1, new Duration(TimeSpan.FromSeconds(0.5)))
                {
                    EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(valueAnimation, d);
                Storyboard.SetTargetProperty(valueAnimation, new PropertyPath(DisplayedValueProperty));
                valueStoryboard.Children.Add(valueAnimation);

                Storyboard opacityStoryboard = new Storyboard();
                DoubleAnimation opacityAnimation = new DoubleAnimation(0, new Duration(TimeSpan.FromSeconds(0.5)))
                {
                    EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut },
                };
                Storyboard.SetTarget(opacityAnimation, t.ProgressGrid);
                Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(OpacityProperty));
                opacityStoryboard.Children.Add(opacityAnimation);
                valueStoryboard.Completed += (sender, e1) =>
                {
                    opacityStoryboard.Begin();
                };
            }
            else
            {
                DoubleAnimation valueAnimation = new DoubleAnimation((double)e.NewValue, new Duration(TimeSpan.FromSeconds(0.5)))
                {
                    EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(valueAnimation, d);
                Storyboard.SetTargetProperty(valueAnimation, new PropertyPath(DisplayedValueProperty));

                if ((double)e.OldValue == -1)
                {
                    valueAnimation.From = 0;

                    Storyboard opacityStoryboard = new Storyboard();
                    DoubleAnimation opacityAnimation = new DoubleAnimation(1, new Duration(TimeSpan.FromSeconds(0.5)))
                    {
                        EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(opacityAnimation, t.ProgressGrid);
                    Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(OpacityProperty));
                    opacityStoryboard.Children.Add(opacityAnimation);
                    opacityStoryboard.Begin();
                }

                valueStoryboard.Children.Add(valueAnimation);
            }
            valueStoryboard.Begin();
        }
    }

    public class SmallerConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double width = (double)values[0];
            double height = (double)values[1];

            double min = Math.Min(width, height);
            return min;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class CenterConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double width = (double)values[0];
            double height = (double)values[1];

            Point p = new Point(width / 2, height / 2);
            return p;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class TopCenterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double width = (double)value;

            Point p = new Point(width / 2, 0);
            return p;
        }

        public object ConvertBack(object value, Type targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class BottomCenterConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double width = (double)values[0];
            double height = (double)values[1];

            Point p = new Point(width / 2, height);
            return p;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class InnerRadiusConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double gridWidth = (double)values[0];
            double trackWidth = (double)values[1];

            double innerRadius = (gridWidth / 2) - trackWidth;
            return innerRadius;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class OutterRadiusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double gridWidth = (double)value;

            double outerRadius = gridWidth / 2;
            return outerRadius;
        }

        public object ConvertBack(object value, Type targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ArcSizeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double width = (double)values[0];
            double height = (double)values[1];

            Size s = new Size(width / 2, height / 2);
            return s;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ArcRPointConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double width = (double)values[0];
            double value = (double)values[1];

            double radius = width / 2;

            if (value > 0.50)
                return new Point(radius, 2 * radius);

            double rad = value * (2 * Math.PI);

            double ox = Math.Cos(rad - Math.PI / 2);
            double oy = Math.Sin(rad - Math.PI / 2);

            double x = ox * radius + radius;
            double y = oy * radius + radius;

            Point p = new Point(x, y);
            return p;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ArcLPointConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double width = (double)values[0];
            double value = (double)values[1];

            double radius = width / 2;

            if (value < 0.50)
                return new Point(radius, 2 * radius);

            double rad = value * (2 * Math.PI);

            double ox = Math.Cos(rad - Math.PI / 2);
            double oy = Math.Sin(rad - Math.PI / 2);

            double x = ox * radius + radius;
            double y = oy * radius + radius;

            Point p = new Point(x, y);
            return p;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class HeadEllipseMarginConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double width = (double)values[0];
            double value = (double)values[1];
            double trackWidth = (double)values[2];

            double radius = width / 2;
            double rad = value * (2 * Math.PI);

            double ox = Math.Cos(rad - Math.PI / 2);
            double oy = Math.Sin(rad - Math.PI / 2);

            double cx = ox * (radius - trackWidth / 2) + radius;
            double cy = oy * (radius - trackWidth / 2) + radius;

            double x = cx - trackWidth / 2;
            double y = cy - trackWidth / 2;

            Thickness t = new Thickness(x, y, 0, 0);
            return t;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
