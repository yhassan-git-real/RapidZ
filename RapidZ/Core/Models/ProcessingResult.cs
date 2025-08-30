using System;
using System.Collections.Generic;

namespace RapidZ.Core.Models
{
    /// <summary>
    /// Model to hold processing result information for dialog display
    /// </summary>
    public class ProcessingResult
    {
        /// <summary>
        /// The type of operation (Import/Export)
        /// </summary>
        public required string OperationType { get; set; }
        
        /// <summary>
        /// Number of files successfully generated
        /// </summary>
        public int FileCount { get; set; }
        
        /// <summary>
        /// Number of parameter combinations processed
        /// </summary>
        public int ParameterCount { get; set; }
        
        /// <summary>
        /// Total number of combinations
        /// </summary>
        public int CombinationCount { get; set; }
        
        /// <summary>
        /// List of generated file names
        /// </summary>
        public List<string> FileNames { get; set; } = new List<string>();
        
        /// <summary>
        /// Total processing time
        /// </summary>
        public TimeSpan ProcessingTime { get; set; }
    }
}
