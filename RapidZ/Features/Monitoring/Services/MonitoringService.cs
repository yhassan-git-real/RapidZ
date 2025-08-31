using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using RapidZ.Features.Monitoring.Models;
using RapidZ.Core.Logging;

namespace RapidZ.Features.Monitoring.Services
{
    /// <summary>
    /// Simplified monitoring service for RapidZ (excluding real-time UI monitoring)
    /// </summary>
    public class MonitoringService : INotifyPropertyChanged
    {
        private StatusInfo _currentStatus;
        private readonly ObservableCollection<MonitoringLog> _logs;

        public MonitoringService()
        {
            _currentStatus = new StatusInfo 
            { 
                Type = StatusType.Idle, 
                Message = "Ready", 
                Timestamp = DateTime.Now 
            };
            _logs = new ObservableCollection<MonitoringLog>();
        }

        public StatusInfo CurrentStatus
        {
            get => _currentStatus;
            private set
            {
                _currentStatus = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<MonitoringLog> Logs => _logs;

        public void UpdateStatus(StatusType status, string message, string category = "")
        {
            CurrentStatus = new StatusInfo
            {
                Type = status,
                Message = message,
                Category = category,
                Timestamp = DateTime.Now
            };

            // No file logging needed - monitoring is for UI only
        }

        public void AddLog(LogLevel level, string message, string category = "")
        {
            var log = new MonitoringLog
            {
                Level = level,
                Message = message,
                Category = category,
                Timestamp = DateTime.Now
            };

            _logs.Add(log);

            // No file logging needed - monitoring is for UI display only
        }

        public void SetError(string message, string category = "")
        {
            UpdateStatus(StatusType.Error, message, category);
            AddLog(LogLevel.Error, message, category);
        }

        public void SetInfo(string message, string category = "")
        {
            AddLog(LogLevel.Info, message, category);
        }

        public void Clear()
        {
            _logs.Clear();
            UpdateStatus(StatusType.Idle, "Ready");
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
