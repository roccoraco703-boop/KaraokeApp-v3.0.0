using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace KaraokeApp.Controls
{
    public class FloatingPanel : ContentControl
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(FloatingPanel), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty TitleHeightProperty = DependencyProperty.Register(
            nameof(TitleHeight), typeof(double), typeof(FloatingPanel), new PropertyMetadata(24.0));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public double TitleHeight
        {
            get => (double)GetValue(TitleHeightProperty);
            set => SetValue(TitleHeightProperty, value);
        }

        static FloatingPanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FloatingPanel), new FrameworkPropertyMetadata(typeof(FloatingPanel)));
        }

        public FloatingPanel()
        {
            MinWidth = 140;
            MinHeight = 90;
            Template = CreateTemplate();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("PART_TitleThumb") is Thumb titleThumb)
            {
                titleThumb.DragStarted += (_, _) => Panel.SetZIndex(this, Environment.TickCount);
                titleThumb.DragDelta += TitleThumbDragDelta;
            }

            if (GetTemplateChild("PART_ResizeGrip") is Thumb resizeGrip)
            {
                resizeGrip.DragDelta += ResizeGripDragDelta;
            }
        }

        private void TitleThumbDragDelta(object sender, DragDeltaEventArgs e)
        {
            if (Parent is not Canvas canvas)
                return;

            double left = Canvas.GetLeft(this);
            double top = Canvas.GetTop(this);

            if (double.IsNaN(left))
                left = 0;

            if (double.IsNaN(top))
                top = 0;

            Canvas.SetLeft(this, Math.Max(0, left + e.HorizontalChange));
            Canvas.SetTop(this, Math.Max(0, top + e.VerticalChange));
        }

        private void ResizeGripDragDelta(object sender, DragDeltaEventArgs e)
        {
            Width = Math.Max(MinWidth, ActualWidth + e.HorizontalChange);
            Height = Math.Max(MinHeight, ActualHeight + e.VerticalChange);
            Panel.SetZIndex(this, Environment.TickCount);
        }

        private static ControlTemplate CreateTemplate()
        {
            return (ControlTemplate)System.Windows.Markup.XamlReader.Parse("""
                <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                                 TargetType="ContentControl">
                    <Border BorderBrush="#BAE6FD" BorderThickness="1" CornerRadius="4" Background="#0F172A">
                        <Grid>
                            <Grid.RowDefinitions>
                                <RowDefinition Height="24"/>
                                <RowDefinition Height="*"/>
                            </Grid.RowDefinitions>
                            <Border Grid.Row="0" CornerRadius="4,4,0,0">
                                <Border.Background>
                                    <LinearGradientBrush StartPoint="0,0" EndPoint="0,1">
                                        <GradientStop Color="#DBF4FF" Offset="0"/>
                                        <GradientStop Color="#7DD3FC" Offset="0.35"/>
                                        <GradientStop Color="#0F172A" Offset="1"/>
                                    </LinearGradientBrush>
                                </Border.Background>
                                <DockPanel LastChildFill="True">
                                    <StackPanel DockPanel.Dock="Right" Orientation="Horizontal" Margin="0,0,6,0">
                                        <TextBlock Text="?" Foreground="#082F49" FontWeight="Bold" Margin="4,0"/>
                                        <TextBlock Text="□" Foreground="#082F49" FontWeight="Bold" Margin="4,0"/>
                                        <TextBlock Text="×" Foreground="#082F49" FontWeight="Bold" Margin="4,0"/>
                                    </StackPanel>
                                    <TextBlock Text="{Binding Title, RelativeSource={RelativeSource TemplatedParent}}" Foreground="#E0F2FE" FontSize="11" FontStyle="Italic" Margin="8,0" VerticalAlignment="Center"/>
                                </DockPanel>
                            </Border>
                            <Thumb x:Name="PART_TitleThumb" Grid.Row="0" Cursor="SizeAll" Background="Transparent"/>
                            <ContentPresenter Grid.Row="1" Content="{TemplateBinding Content}"/>
                            <Thumb x:Name="PART_ResizeGrip" Grid.RowSpan="2" Width="16" Height="16" HorizontalAlignment="Right" VerticalAlignment="Bottom" Cursor="SizeNWSE" Background="#787DD3FC"/>
                        </Grid>
                    </Border>
                </ControlTemplate>
                """);
        }
    }
}
