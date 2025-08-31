namespace RapidZ.Core.Logging.Models
{
    /// <summary>
    /// Represents detailed parameters for process logging
    /// </summary>
    public sealed class ProcessParameters
    {
        /// <summary>
        /// Gets or sets the start month for the process
        /// </summary>
        public string FromMonth { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the end month for the process
        /// </summary>
        public string ToMonth { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the HS Code filter
        /// </summary>
        public string HsCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the product filter
        /// </summary>
        public string Product { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the IEC filter
        /// </summary>
        public string Iec { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the exporter/importer filter
        /// </summary>
        public string ExporterOrImporter { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the country filter
        /// </summary>
        public string Country { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name filter
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the port filter
        /// </summary>
        public string Port { get; set; } = string.Empty;

        /// <summary>
        /// Formats the parameters for logging display
        /// </summary>
        /// <returns>A formatted string representation of the parameters</returns>
        public override string ToString()
        {
            return $"Period: {FromMonth} to {ToMonth}, HS: {HsCode}, Product: {Product}, IEC: {Iec}, Entity: {ExporterOrImporter}, Country: {Country}, Name: {Name}, Port: {Port}";
        }
    }
}