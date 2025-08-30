using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using RapidZ.Core.Models;

namespace RapidZ.Features.Common.ViewModels
{
    /// <summary>
    /// ViewModel for database object selection in the UI (dropdown support)
    /// </summary>
    public class DbObjectSelectorViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<DbObjectOption> _views = null!;
        private ObservableCollection<DbObjectOption> _storedProcedures = null!;
        private DbObjectOption _selectedView = null!;
        private DbObjectOption _selectedStoredProcedure = null!;

        /// <summary>
        /// Collection of available views
        /// </summary>
        public ObservableCollection<DbObjectOption> Views
        {
            get => _views;
            set
            {
                _views = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Collection of available stored procedures
        /// </summary>
        public ObservableCollection<DbObjectOption> StoredProcedures
        {
            get => _storedProcedures;
            set
            {
                _storedProcedures = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Currently selected view
        /// </summary>
        public DbObjectOption SelectedView
        {
            get => _selectedView;
            set
            {
                if (_selectedView != value)
                {
                    _selectedView = value;
                    OnPropertyChanged();
                    OnSelectionChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// Currently selected stored procedure
        /// </summary>
        public DbObjectOption SelectedStoredProcedure
        {
            get => _selectedStoredProcedure;
            set
            {
                if (_selectedStoredProcedure != value)
                {
                    _selectedStoredProcedure = value;
                    OnPropertyChanged();
                    OnSelectionChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// Event raised when selection changes
        /// </summary>
        public event Action? OnSelectionChanged;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="views">Available views</param>
        /// <param name="storedProcedures">Available stored procedures</param>
        /// <param name="defaultViewName">Default view to select</param>
        /// <param name="defaultStoredProcedureName">Default stored procedure to select</param>
        public DbObjectSelectorViewModel(
            IList<DbObjectOption> views,
            IList<DbObjectOption> storedProcedures,
            string? defaultViewName = null,
            string? defaultStoredProcedureName = null)
        {
            Views = new ObservableCollection<DbObjectOption>(views ?? new List<DbObjectOption>());
            StoredProcedures = new ObservableCollection<DbObjectOption>(storedProcedures ?? new List<DbObjectOption>());

            // Set default selections
            _selectedView = Views.FirstOrDefault(v => v.Name == defaultViewName) ?? Views.FirstOrDefault() ?? new DbObjectOption("", "");
            _selectedStoredProcedure = StoredProcedures.FirstOrDefault(sp => sp.Name == defaultStoredProcedureName) ?? StoredProcedures.FirstOrDefault() ?? new DbObjectOption("", "");
        }

        /// <summary>
        /// Update available views
        /// </summary>
        /// <param name="views">New views collection</param>
        public void UpdateViews(IList<DbObjectOption> views)
        {
            Views.Clear();
            foreach (var view in views ?? new List<DbObjectOption>())
            {
                Views.Add(view);
            }

            // Reset selection to first available
            SelectedView = Views.FirstOrDefault() ?? new DbObjectOption("", "");
        }

        /// <summary>
        /// Update available stored procedures
        /// </summary>
        /// <param name="storedProcedures">New stored procedures collection</param>
        public void UpdateStoredProcedures(IList<DbObjectOption> storedProcedures)
        {
            StoredProcedures.Clear();
            foreach (var sp in storedProcedures ?? new List<DbObjectOption>())
            {
                StoredProcedures.Add(sp);
            }

            // Reset selection to first available
            SelectedStoredProcedure = StoredProcedures.FirstOrDefault() ?? new DbObjectOption("", "");
        }

        /// <summary>
        /// Get currently selected view name
        /// </summary>
        /// <returns>Selected view name or empty string</returns>
        public string GetSelectedViewName()
        {
            return SelectedView?.Name ?? string.Empty;
        }

        /// <summary>
        /// Get currently selected stored procedure name
        /// </summary>
        /// <returns>Selected stored procedure name or empty string</returns>
        public string GetSelectedStoredProcedureName()
        {
            return SelectedStoredProcedure?.Name ?? string.Empty;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
