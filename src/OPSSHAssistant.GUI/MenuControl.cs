using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;

namespace OPSSHAssistant.GUI;

public class MenuControl : Control
{
    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<MenuControl, string>(nameof(Text), defaultValue: string.Empty);
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    
    public static readonly StyledProperty<int> StepNumberProperty = AvaloniaProperty.Register<MenuControl, int>(nameof(StepNumber), defaultValue: 0);
    public int StepNumber
    {
        get => GetValue(StepNumberProperty);
        set => SetValue(StepNumberProperty, value);
    }

    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<MenuControl, bool>(nameof(IsSelected), defaultValue: false);
    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }
    
    

    public static readonly StyledProperty<ICommand> ClickProperty = AvaloniaProperty.Register<MenuControl, ICommand>(nameof(Click));
    public ICommand Click
    {
        get => GetValue(ClickProperty);
        set => SetValue(ClickProperty, value);
    }

    List<Point> points = new List<Point>();
    StreamGeometry? geometry = null;
    
    FormattedText? formattedText = null;
    IBrush regularBackgroundBrush;
    IBrush disabledBackgroundBrush;
    Pen selectedPen;
    Pen unselectedPen;
    
    bool isHovered = false;
    SolidColorBrush selectedTextColor = new SolidColorBrush(Colors.White, 1);
    SolidColorBrush unselectedTextColor = new SolidColorBrush(Colors.White, 0.5);
    
    public MenuControl()
    {
        Height = 75;

        regularBackgroundBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new GradientStop(Color.FromRgb(147, 204, 234), 0),
                new GradientStop(Color.FromRgb(100, 149, 237), 1),
            }
        };
        
        disabledBackgroundBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new GradientStop(Colors.SlateGray, 0),
                new GradientStop(Colors.DarkSlateGray, 1),
            }
        };
        
        selectedPen = new Pen(new SolidColorBrush(Colors.Black), 1);
        unselectedPen = new Pen(new SolidColorBrush(Colors.Black), 1);
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        
        var penOffset = selectedPen.Thickness / 2;

        points.Clear();
        points.Add(new Point(penOffset, penOffset));
        points.Add(new Point(e.NewSize.Width - penOffset, penOffset));
        points.Add(new Point(e.NewSize.Width + 25 - penOffset, e.NewSize.Height / 2.0));
        points.Add(new Point(e.NewSize.Width - penOffset, e.NewSize.Height - penOffset));
        points.Add(new Point(0, e.NewSize.Height - penOffset));
        
        geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(points[0], true);
            for (int i = 1; i < points.Count; ++i)
            {
                ctx.LineTo(points[i]);
            }
            ctx.EndFigure(true);
        }
    }
    

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        isHovered = true;
        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        isHovered = false;
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        Click?.Execute(null);
    }


    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        //Console.WriteLine($"{StepNumber} = {change.Property.Name}");

        if (change.Property.Name == nameof(Text))
        {
            formattedText = new FormattedText(Text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 20, unselectedTextColor);
            UpdateFontColor();
            InvalidateVisual();
        }
        else if (change.Property.Name == nameof(IsSelected))
        {
            UpdateFontColor();
            InvalidateVisual();
        }
        else if (change.Property.Name == nameof(IsEnabled))
        {
            UpdateFontColor();
            InvalidateVisual();
        }
    }


    void UpdateFontColor()
    {
        if (IsEnabled)
        {
            if (IsSelected)
            {
                formattedText?.SetForegroundBrush(selectedTextColor);
            }
            else
            {
                formattedText?.SetForegroundBrush(unselectedTextColor);
            }
        }
        else
        {
            formattedText?.SetForegroundBrush(unselectedTextColor);
        }
    }
    

    public override void Render(DrawingContext context)
    {
        if (geometry is not null)
        {
            if (IsEnabled)
            {
                if (isHovered)
                {
                    context.DrawGeometry(regularBackgroundBrush, selectedPen, geometry);
                    context.DrawGeometry(new SolidColorBrush(Colors.White, 0.25), selectedPen, geometry);
                }
                else
                {
                    if (IsSelected)
                    {
                        context.DrawGeometry(regularBackgroundBrush, selectedPen, geometry);
                    }
                    else
                    {
                        context.DrawGeometry(regularBackgroundBrush, unselectedPen, geometry);
                    }
                }
            }
            else
            {
                context.DrawGeometry(disabledBackgroundBrush, unselectedPen, geometry);
            }
        }

        if (formattedText != null)
        {
            context.DrawText(formattedText, new Point((25/2) + (Bounds.Width - formattedText.Width) / 2, (Bounds.Height - formattedText.Height) / 2));
        }
    }
}