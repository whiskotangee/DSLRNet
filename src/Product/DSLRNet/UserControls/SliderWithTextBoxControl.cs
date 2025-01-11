using System.Windows;

namespace DSLRNet.UserControls
{
    public partial class SliderWithTextUserControl : System.Windows.Controls.UserControl
    {
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(double), typeof(SliderWithTextUserControl), new PropertyMetadata(0.0));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(double), typeof(SliderWithTextUserControl), new PropertyMetadata(100.0));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(SliderWithTextUserControl), new PropertyMetadata(0.0));

        public static readonly DependencyProperty TickFrequencyProperty =
            DependencyProperty.Register("TickFrequency", typeof(double), typeof(SliderWithTextUserControl), new PropertyMetadata(1.0));

        public static readonly DependencyProperty IsSnapToTickEnabledProperty =
            DependencyProperty.Register("IsSnapToTickEnabled", typeof(bool), typeof(SliderWithTextUserControl), new PropertyMetadata(true));

        public static readonly DependencyProperty IsPercentileProperty =
            DependencyProperty.Register("IsPercentile", typeof(bool), typeof(SliderWithTextUserControl), new PropertyMetadata(false));

        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public double TickFrequency
        {
            get { return (double)GetValue(TickFrequencyProperty); }
            set { SetValue(TickFrequencyProperty, value); }
        }

        public bool IsSnapToTickEnabled
        {
            get { return (bool)GetValue(TickFrequencyProperty); }
            set { SetValue(TickFrequencyProperty, value); }
        }

        public bool IsPercentile
        {
            get { return (bool)GetValue(IsPercentileProperty); }
            set { SetValue(IsPercentileProperty, value); }
        }
    }
}
