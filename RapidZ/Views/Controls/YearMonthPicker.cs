using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using RapidZ.Core.Helpers;

namespace RapidZ.Views.Controls;

// Custom control for selecting Year and Month
public class YearMonthPicker : UserControl
{
    // Define dependency properties for Year and Month
    public static readonly DirectProperty<YearMonthPicker, int> YearProperty =
        AvaloniaProperty.RegisterDirect<YearMonthPicker, int>(
            nameof(Year),
            o => o.Year,
            (o, v) => o.Year = v,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly DirectProperty<YearMonthPicker, int> MonthProperty =
        AvaloniaProperty.RegisterDirect<YearMonthPicker, int>(
            nameof(Month),
            o => o.Month,
            (o, v) => o.Month = v,
            defaultBindingMode: BindingMode.TwoWay);

    // Legacy Value property for backward compatibility
    public static readonly DirectProperty<YearMonthPicker, string> ValueProperty =
        AvaloniaProperty.RegisterDirect<YearMonthPicker, string>(
            nameof(Value),
            o => o.Value,
            (o, v) => o.Value = v,
            defaultBindingMode: BindingMode.TwoWay);
    
    // Internal ComboBox controls
    private readonly ComboBox _yearComboBox;
    private readonly ComboBox _monthComboBox;
    private readonly Grid _mainGrid;
    
    // Property backing fields
    private int _year;
    private int _month;
    private string _value = string.Empty;
    
    // Properties
    public int Year
    {
        get => _year;
        set
        {
            if (SetAndRaise(YearProperty, ref _year, value))
            {
                UpdateYearControl();
                UpdateValue();
            }
        }
    }

    public int Month
    {
        get => _month;
        set
        {
            if (SetAndRaise(MonthProperty, ref _month, value))
            {
                UpdateMonthControl();
                UpdateValue();
            }
        }
    }

    // Legacy Value property for backward compatibility
    public string Value
    {
        get => _value;
        set
        {
            if (SetAndRaise(ValueProperty, ref _value, value))
            {
                if (DateHelper.IsValidYearMonth(value))
                {
                    _year = int.Parse(value.Substring(0, 4));
                    _month = int.Parse(value.Substring(4, 2));
                    
                    UpdateControls();
                    RaisePropertyChanged(YearProperty, _year, _year);
                    RaisePropertyChanged(MonthProperty, _month, _month);
                }
            }
        }
    }
    
    // Constructor
    public YearMonthPicker()
    {
        // Initialize with current date
        _year = DateTime.Now.Year;
        _month = DateTime.Now.Month;
        
        // Create year combobox
        _yearComboBox = new ComboBox
        {
            Width = 80,
            PlaceholderText = "Year",
            Margin = new Thickness(0, 0, 2, 0)
        };
        
        // Create month combobox
        _monthComboBox = new ComboBox
        {
            Width = 65,
            PlaceholderText = "Month",
            Margin = new Thickness(2, 0, 3, 0)
        };
        
        // Create main layout grid
        _mainGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto, Auto"),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 3, 0) 
        };
        
        // Add controls to grid
        _mainGrid.Children.Add(_yearComboBox);
        _mainGrid.Children.Add(_monthComboBox);
        
        // Set grid column positions
        Grid.SetColumn(_yearComboBox, 0);
        Grid.SetColumn(_monthComboBox, 1);
        
        // Set the grid as content
        Content = _mainGrid;
        
        // Initialize the control
        InitializeControl();
    }
    
    // Initialize the control
    private void InitializeControl()
    {
        // Populate year dropdown (current year -5 to +5)
        int currentYear = DateTime.Now.Year;
        for (int year = currentYear - 5; year <= currentYear + 5; year++)
        {
            _yearComboBox.Items?.Add(year.ToString());
        }
        
        // Populate month dropdown
        for (int month = 1; month <= 12; month++)
        {
            _monthComboBox.Items?.Add(month.ToString("D2"));
        }
        
        // Set default selection
        _yearComboBox.SelectedItem = _year.ToString();
        _monthComboBox.SelectedItem = _month.ToString("D2");
        
        // Set up event handlers
        _yearComboBox.SelectionChanged += YearComboBox_SelectionChanged;
        _monthComboBox.SelectionChanged += MonthComboBox_SelectionChanged;
        
        // Update the value from the default selections
        UpdateValue();
    }
    
    // Handle year combo box selection changes
    private void YearComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_yearComboBox.SelectedItem is string yearStr && int.TryParse(yearStr, out int selectedYear))
        {
            Year = selectedYear;
        }
    }
    
    // Handle month combo box selection changes
    private void MonthComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_monthComboBox.SelectedItem is string monthStr && int.TryParse(monthStr, out int selectedMonth))
        {
            Month = selectedMonth;
        }
    }
    
    // Update the value based on selection
    private void UpdateValue()
    {
        _value = $"{_year}{_month:D2}";
        RaisePropertyChanged(ValueProperty, _value, _value);
    }
    
    // Update the controls based on the year and month
    private void UpdateControls()
    {
        UpdateYearControl();
        UpdateMonthControl();
    }
    
    // Update the year combobox selection
    private void UpdateYearControl()
    {
        string yearStr = _year.ToString();
        if (_yearComboBox.Items?.Contains(yearStr) == true)
        {
            _yearComboBox.SelectedItem = yearStr;
        }
    }
    
    // Update the month combobox selection
    private void UpdateMonthControl()
    {
        string monthStr = _month.ToString("D2");
        if (_monthComboBox.Items?.Contains(monthStr) == true)
        {
            _monthComboBox.SelectedItem = monthStr;
        }
    }
}
