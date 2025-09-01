using System.ComponentModel;
using System.Runtime.CompilerServices;
using ReactiveUI;

namespace RapidZ.Views.Models;

// Filter criteria for export data queries
public class ExportDataFilter : ReactiveObject
{
    private string _hsCode = string.Empty;
    private string _product = string.Empty;
    private string _exporter = string.Empty;
    private string _iec = string.Empty;
    private string _foreignParty = string.Empty;
    private string _foreignCountry = string.Empty;
    private string _port = string.Empty;
    private string _fromMonth = string.Empty;
    private string _toMonth = string.Empty;
    private string _mode = "Export";
    private string _customFilePath = string.Empty;
    private bool _useCustomPath = false;

    // HS code for product classification
    public string HSCode 
    { 
        get => _hsCode;
        set => this.RaiseAndSetIfChanged(ref _hsCode, value);
    }
    
    // Product description
    public string Product 
    { 
        get => _product;
        set => this.RaiseAndSetIfChanged(ref _product, value);
    }
    
    // Exporter name
    public string Exporter 
    { 
        get => _exporter;
        set => this.RaiseAndSetIfChanged(ref _exporter, value);
    }
    
    // Importer-Exporter Code
    public string IEC 
    { 
        get => _iec;
        set => this.RaiseAndSetIfChanged(ref _iec, value);
    }
    
    // Foreign party name
    public string ForeignParty 
    { 
        get => _foreignParty;
        set => this.RaiseAndSetIfChanged(ref _foreignParty, value);
    }
    
    // Foreign country
    public string ForeignCountry 
    { 
        get => _foreignCountry;
        set => this.RaiseAndSetIfChanged(ref _foreignCountry, value);
    }
    
    // Port name/code
    public string Port 
    { 
        get => _port;
        set => this.RaiseAndSetIfChanged(ref _port, value);
    }
    
    // Start period (YYYYMM format)
    public string FromMonth 
    { 
        get => _fromMonth;
        set => this.RaiseAndSetIfChanged(ref _fromMonth, value);
    }
    
    // End period (YYYYMM format)
    public string ToMonth 
    { 
        get => _toMonth;
        set => this.RaiseAndSetIfChanged(ref _toMonth, value);
    }
    
    // Import or Export mode
    public string Mode 
    { 
        get => _mode;
        set => this.RaiseAndSetIfChanged(ref _mode, value);
    }
    
    // Custom file path for output
    public string CustomFilePath 
    { 
        get => _customFilePath;
        set => this.RaiseAndSetIfChanged(ref _customFilePath, value);
    }
    
    // Whether to use custom file path
    public bool UseCustomPath 
    { 
        get => _useCustomPath;
        set => this.RaiseAndSetIfChanged(ref _useCustomPath, value);
    }
}
