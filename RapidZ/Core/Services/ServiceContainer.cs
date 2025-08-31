using System;
using Microsoft.Extensions.Configuration;
using RapidZ.Core.Cancellation;
using RapidZ.Core.Controllers;
using RapidZ.Core.Database;
using RapidZ.Core.Models;
using RapidZ.Core.Validation;
using RapidZ.Features.Common.ViewModels;
using RapidZ.Features.Export;
using RapidZ.Features.Export.Services;
using RapidZ.Features.Import;
using RapidZ.Features.Import.Services;
using RapidZ.Features.Monitoring.Services;

namespace RapidZ.Core.Services
{
    /// <summary>
    /// Container for managing all application services and their dependencies
    /// </summary>
    public class ServiceContainer
    {
        // Core Services
        public ExportExcelService ExcelService { get; private set; } = null!;
        public ImportExcelService ImportService { get; private set; } = null!;
        public ICancellationManager CancellationManager { get; private set; } = null!;
        public MonitoringService MonitoringService { get; private set; } = null!;

        // Validation Services
        public ExportObjectValidationService ExportObjectValidationService { get; private set; } = null!;
        public ImportObjectValidationService ImportObjectValidationService { get; private set; } = null!;
        public IParameterValidator ParameterValidator { get; private set; } = null!;
        public DatabaseObjectValidator DatabaseObjectValidator { get; private set; } = null!;
        public IValidationService ValidationService { get; private set; } = null!;
        public IResultProcessorService ResultProcessorService { get; private set; } = null!;

        // Controllers
        public IExportController ExportController { get; private set; } = null!;
        public IImportController ImportController { get; private set; } = null!;
        
        // UI Action Service
        public IUIActionService UIActionService { get; private set; } = null!;

        // View Models for dropdown support
        public DbObjectSelectorViewModel ExportDbObjectViewModel { get; private set; } = null!;
        public DbObjectSelectorViewModel ImportDbObjectViewModel { get; private set; } = null!;

        /// <summary>
        /// Initialize all services with their dependencies
        /// </summary>
        public void InitializeServices()
        {
            try
            {
                // Initialize core services using static cache methods
                var exportSettings = ConfigurationCacheService.GetExportSettings();
                var importSettings = ConfigurationCacheService.GetImportSettings();
                
                // Initialize validation services first (reuse instances)
                ExportObjectValidationService = new ExportObjectValidationService(exportSettings);
                ImportObjectValidationService = new ImportObjectValidationService(importSettings);
                ParameterValidator = new ParameterValidator();
                ValidationService = new ValidationService(ParameterValidator, ExportObjectValidationService, ImportObjectValidationService);
                ResultProcessorService = new ResultProcessorService();
                
                // Initialize core services using the validation services
                ExcelService = new ExportExcelService(ExportObjectValidationService, new RapidZ.Core.DataAccess.ExportDataAccess());
                ImportService = new ImportExcelService();
                CancellationManager = new CancellationManager();
                MonitoringService = new MonitoringService();
                
                // Initialize controllers
                ExportController = new ExportController(
                    ExcelService,
                    ValidationService,
                    ResultProcessorService,
                    MonitoringService);
                    
                ImportController = new ImportController(
                    ImportService,
                    ValidationService,
                    ResultProcessorService,
                    MonitoringService);
                
                // Initialize database object validator
                var dbSettings = ConfigurationCacheService.GetSharedDatabaseSettings();
                DatabaseObjectValidator = new DatabaseObjectValidator(dbSettings.ConnectionString);

                // Initialize view models for database object selection (dropdown support)
                ExportDbObjectViewModel = new DbObjectSelectorViewModel(
                    ExportObjectValidationService.GetAvailableViews(),
                    ExportObjectValidationService.GetAvailableStoredProcedures(),
                    ExportObjectValidationService.GetDefaultViewName(),
                    ExportObjectValidationService.GetDefaultStoredProcedureName());
                    
                ImportDbObjectViewModel = new DbObjectSelectorViewModel(
                    ImportObjectValidationService.GetAvailableViews(),
                    ImportObjectValidationService.GetAvailableStoredProcedures(),
                    ImportObjectValidationService.GetDefaultViewName(),
                    ImportObjectValidationService.GetDefaultStoredProcedureName());

                // Initialize UIActionService
                UIActionService = new UIActionService(
                    (ExportController)ExportController,
                    (ImportController)ImportController,
                    ResultProcessorService);
                UIActionService.SetServiceContainer(this);

                MonitoringService.UpdateStatus(RapidZ.Features.Monitoring.Models.StatusType.Idle, "All services initialized successfully");
            }
            catch (Exception ex)
            {
                MonitoringService?.UpdateStatus(RapidZ.Features.Monitoring.Models.StatusType.Error, "Service initialization failed");
                throw new InvalidOperationException($"Failed to initialize services: {ex.Message}", ex);
            }
        }
    }
}
