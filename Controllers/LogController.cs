using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Text;

namespace LogAnalyzerLibrary.Controllers
{
    public class LogController : ControllerBase
    {

        [HttpGet("/search")]
        public IActionResult SearchLogs(string[] directories, string query)
        {
            try
            {
                if (directories == null || directories.Length == 0 || string.IsNullOrEmpty(query))
                {
                    return BadRequest("Directories and query parameters are required.");
                }

                List<LogResult> results = new List<LogResult>();

                foreach (var directory in directories)
                {
                    if (!Directory.Exists(directory))
                    {
                        // Log and continue to the next directory
                        Console.WriteLine($"Directory not found: {directory}");
                        continue;
                    }

                    SearchLogsInDirectory(directory, query, results);
                }

                return Ok(results);
            }
            catch (Exception ex)
            {

                return StatusCode(500, $"Error searching logs: {ex.Message}");
            }
           
        }

        [HttpGet("/count")]
        public IActionResult CountErrors(string[] directories)
        {
            try
            {
                if (directories == null || directories.Length == 0)
                {
                    return BadRequest("Directories parameter is required.");
                }

                Dictionary<string, int> errorCounts = new Dictionary<string, int>();

                foreach (var directory in directories)
                {
                    try
                    {
                        if (!Directory.Exists(directory))
                        {
                            // Log and continue to the next directory
                            Console.WriteLine($"Directory not found: {directory}");
                            continue;
                        }

                        CountErrorsInDirectory(directory, errorCounts);
                    }
                    catch (Exception ex)
                    {
                        // Log the error and continue to the next directory
                        Console.WriteLine($"Error counting log errors in directory {directory}: {ex.Message}");
                    }
                }

                string countResult = ExecuteCountQuery(errorCounts);
                if (countResult != null)
                {
                    // Successfully retrieved the count result
                    return Ok($"Error count is: {countResult}");
                }
            }
            catch (Exception)
            {
                // Error occurred while executing the count query
                return StatusCode(500, "Error occurred while executing count query.");
            }

            // If execution reaches this point, return a generic error message
            return StatusCode(500, "Unknown error occurred while processing the request.");
        }

        [HttpGet("/count-duplicates")]
        public IActionResult CountDuplicateErrors(string[] directories)
        {
            try
            {
                if (directories == null || directories.Length == 0)
                {
                    return BadRequest("Directories parameter is required.");
                }

                Dictionary<string, int> errorCounts = new Dictionary<string, int>();
                HashSet<string> uniqueErrors = new HashSet<string>();

                foreach (var directory in directories)
                {
                    try
                    {
                        if (!Directory.Exists(directory))
                        {
                            // Log and continue to the next directory
                            Console.WriteLine($"Directory not found: {directory}");
                            continue;
                        }

                        CountUniqueErrorsInDirectory(directory, errorCounts, uniqueErrors);
                    }
                    catch (Exception ex)
                    {
                        // Log the error and continue to the next directory
                        Console.WriteLine($"Error counting unique errors in directory {directory}: {ex.Message}");
                    }
                }

                string countResult = ExecuteCountQuery(errorCounts);
                if (countResult != null)
                {
                    // Successfully retrieved the count result
                    return Ok($"Duplicate Count is: {countResult}");
                }
                else
                {
                    // Error occurred while executing the count query
                    return StatusCode(500, "Error occurred while executing count query.");
                }
            }
            catch (Exception)
            {
                // Error occurred while processing the request
                return StatusCode(500, "Unknown error occurred while processing the request.");
            }
        }

        [HttpDelete("/delete-logs")]
        public IActionResult DeleteLogsInDirectory(string directory, DateTime fromDate, DateTime toDate)
        {
            if (string.IsNullOrEmpty(directory) || fromDate == default || toDate == default)
            {
                return BadRequest("Directory, fromDate, and toDate parameters are required.");
            }

            try
            {
                DeleteLogsInDirectoryRecursive(directory, fromDate, toDate);
                return Ok("Logs deleted successfully.");
            }
            catch (DirectoryNotFoundException ex)
            {
                return NotFound($"Directory not found: {directory}. {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, $"Access denied. {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting logs: {ex.Message}");
            }
        }

        [HttpPost("/archive")]
        public IActionResult ArchiveLogs(string[] directories, DateTime fromDate, DateTime toDate)
        {
            try
            {
                if (directories == null || directories.Length == 0 || fromDate == default || toDate == default)
                {
                    return BadRequest("Directories, fromDate, and toDate parameters are required.");
                }

                string zipFileName = $"{fromDate:dd_MM_yyyy}-{toDate:dd_MM_yyyy}.zip";

                foreach (var directory in directories)
                {
                    try
                    {
                        if (!Directory.Exists(directory))
                        {
                            // Log and continue to the next directory
                            Console.WriteLine($"Directory not found: {directory}");
                            continue;
                        }

                        ArchiveLogsInDirectory(directory, fromDate, toDate, zipFileName);
                    }
                    catch (Exception ex)
                    {
                        // Log the error and continue to the next directory
                        Console.WriteLine($"Error archiving logs in directory {directory}: {ex.Message}");
                    }
                }

                return Ok($"Logs archived to {zipFileName} successfully.");
            }
            catch (Exception)
            {
                // Error occurred while processing the request
                return StatusCode(500, "Unknown error occurred while processing the request.");
            }
        }

        [HttpGet("/total-logs")]
        public IActionResult CountTotalLogs(string[] directories, DateTime fromDate, DateTime toDate)
        {
            try
            {
                if (directories == null || directories.Length == 0 || fromDate == default || toDate == default)
                {
                    return BadRequest("Directories, fromDate, and toDate parameters are required.");
                }

                int totalLogs = 0;

                foreach (var directory in directories)
                {
                    try
                    {
                        if (!Directory.Exists(directory))
                        {
                            // Log and continue to the next directory
                            Console.WriteLine($"Directory not found: {directory}");
                            continue;
                        }

                        totalLogs += CountLogsInDirectory(directory, fromDate, toDate);
                    }
                    catch (Exception ex)
                    {
                        // Log the error and continue to the next directory
                        Console.WriteLine($"Error counting logs in directory {directory}: {ex.Message}");
                    }
                }

                return Ok($"Total logs in the specified period: {totalLogs}");
            }
            catch (Exception)
            {
                // Error occurred while processing the request
                return StatusCode(500, "Unknown error occurred while processing the request.");
            }
        }

        [HttpGet("/search-by-size")]
        public IActionResult SearchLogsBySize(string[] directories, long minSizeKB, long maxSizeKB)
        {
            try
            {
                if (directories == null || directories.Length == 0 || minSizeKB < 0 || maxSizeKB < minSizeKB)
                {
                    return BadRequest("Directories, minSizeKB, and maxSizeKB parameters are required and must be valid.");
                }

                List<LogResult> results = new List<LogResult>();

                foreach (var directory in directories)
                {
                    try
                    {
                        if (!Directory.Exists(directory))
                        {
                            // Log and continue to the next directory
                            Console.WriteLine($"Directory not found: {directory}");
                            continue;
                        }

                        SearchLogsBySizeInDirectory(directory, minSizeKB, maxSizeKB, results);
                    }
                    catch (Exception ex)
                    {
                        // Log the error and continue to the next directory
                        Console.WriteLine($"Error searching logs by size in directory {directory}: {ex.Message}");
                    }
                }

                return Ok(results);
            }
            catch (Exception)
            {
                // Error occurred while processing the request
                return StatusCode(500, "Unknown error occurred while processing the request.");
            }
        }

        [HttpGet("/search-by-directory")]
        public IActionResult SearchLogsByDirectory(string directory, string query)
        {
            try
            {
                if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(query))
                {
                    return BadRequest("Directory and query parameters are required.");
                }

                if (!Directory.Exists(directory))
                {
                    return NotFound("Directory not found.");
                }

                List<LogResult> results = new List<LogResult>();

                try
                {
                    SearchLogsInDirectory(directory, query, results);
                }
                catch (Exception ex)
                {
                    // Log the error and return an error response
                    Console.WriteLine($"Error searching logs by directory: {ex.Message}");
                    return StatusCode(500, "Error occurred while searching logs by directory.");
                }

                return Ok(results);
            }
            catch (Exception)
            {
                // Error occurred while processing the request
                return StatusCode(500, "Unknown error occurred while processing the request.");
            }
        }





        private void DeleteLogsInDirectoryRecursive(string directory, DateTime fromDate, DateTime toDate)
        {
            // Check if the directory exists
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException($"Directory not found: {directory}");
            }

            try
            {
                // Delete files within the specified date range
                foreach (string filePath in Directory.GetFiles(directory, "*.log"))
                {
                    try
                    {
                        FileInfo fileInfo = new FileInfo(filePath);
                        if (fileInfo.LastWriteTime >= fromDate && fileInfo.LastWriteTime <= toDate)
                        {
                            fileInfo.Delete();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log and continue to the next file
                        Console.WriteLine($"Error deleting file: {ex.Message}");
                        continue;
                    }
                }

                // Recursively delete files in subdirectories
                foreach (string subdirectory in Directory.GetDirectories(directory))
                {
                    DeleteLogsInDirectoryRecursive(subdirectory, fromDate, toDate);
                }
            }
            catch (Exception ex)
            {
                // Log and rethrow the exception
                Console.WriteLine($"Error deleting logs in directory {directory}: {ex.Message}");
                throw;
            }
        }

        private string ExecuteCountQuery(Dictionary<string, int> errorCounts)
        {
            // Implement your count query logic here
            // For example, generate a formatted string with error counts
            StringBuilder resultBuilder = new StringBuilder();
            foreach (var error in errorCounts)
            {
                resultBuilder.AppendLine($"Error: {error.Key}, Count: {error.Value}");
            }
            return resultBuilder.ToString();
        }
        private void SearchLogsBySizeInDirectory(string directory, long minSizeKB, long maxSizeKB, List<LogResult> results)
        {
            foreach (string filePath in Directory.GetFiles(directory, "*.log", SearchOption.AllDirectories))
            {
                try
                {
                    FileInfo fileInfo = new FileInfo(filePath);
                    long fileSizeKB = fileInfo.Length / 1024; // Convert file size to kilobytes

                    if (fileSizeKB >= minSizeKB && fileSizeKB <= maxSizeKB)
                    {
                        // If file size is within the specified range, add it to results
                        results.Add(new LogResult
                        {
                            FilePath = filePath,
                            SizeKB = fileSizeKB
                        });
                    }
                }
                catch (Exception ex)
                {
                    // Log and continue to the next file
                    Console.WriteLine($"Error searching log by size: {ex.Message}");
                    continue;
                }
            }
        }
        private int CountLogsInDirectory(string directory, DateTime fromDate, DateTime toDate)
        {
            int logCount = 0;

            foreach (string filePath in Directory.GetFiles(directory, "*.log", SearchOption.AllDirectories))
            {
                try
                {
                    FileInfo fileInfo = new FileInfo(filePath);
                    if (fileInfo.LastWriteTime >= fromDate && fileInfo.LastWriteTime <= toDate)
                    {
                        logCount++;
                    }
                }
                catch (Exception ex)
                {
                    // Log and continue to the next file
                    Console.WriteLine($"Error counting logs: {ex.Message}");
                    continue;
                }
            }

            return logCount;
        }
        private void ArchiveLogsInDirectory(string directory, DateTime fromDate, DateTime toDate, string zipFileName)
        {
            try
            {
                string destinationPath = Path.Combine(directory, zipFileName); // Full path for the ZIP file

                using (ZipArchive archive = ZipFile.Open(destinationPath, ZipArchiveMode.Create))
                {
                    foreach (string filePath in Directory.GetFiles(directory, "*.log", SearchOption.AllDirectories))
                    {
                        FileInfo fileInfo = new FileInfo(filePath);
                        if (fileInfo.LastWriteTime >= fromDate && fileInfo.LastWriteTime <= toDate)
                        {
                            archive.CreateEntryFromFile(filePath, Path.GetFileName(filePath));
                        }
                    }
                }

                // Delete the log files after archiving
                DeleteLogsInDirectory(directory, fromDate, toDate);
            }
            catch (Exception ex)
            {
                // Log and continue to the next directory
                Console.WriteLine($"Error archiving logs: {ex.Message}");
                return;
            }
        }
        private void CountErrorsInDirectory(string directory, Dictionary<string, int> errorCounts)
        {
            foreach (string filePath in Directory.GetFiles(directory, "*.log", SearchOption.AllDirectories))
            {
                try
                {
                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (!errorCounts.ContainsKey(line))
                            {
                                errorCounts[line] = 1;
                            }
                            else
                            {
                                errorCounts[line]++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log and continue to the next file
                    Console.WriteLine($"Error reading file: {ex.Message}");
                    continue;
                }
            }
        }
        private void CountUniqueErrorsInDirectory(string directory, Dictionary<string, int> errorCounts, HashSet<string> uniqueErrors)
        {
            foreach (string filePath in Directory.GetFiles(directory, "*.log", SearchOption.AllDirectories))
            {
                try
                {
                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            // Assuming each line represents an error
                            if (!uniqueErrors.Contains(line))
                            {
                                uniqueErrors.Add(line);
                            }
                            else
                            {
                                if (!errorCounts.ContainsKey(line))
                                {
                                    errorCounts[line] = 1;
                                }
                                else
                                {
                                    errorCounts[line]++;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log and continue to the next file
                    Console.WriteLine($"Error reading file: {ex.Message}");
                    continue;
                }
            }
        }
        private void SearchLogsInDirectory(string directory, string query, List<LogResult> results)
        {
            foreach (string filePath in Directory.GetFiles(directory, "*.log", SearchOption.AllDirectories))
            {
                try
                {
                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.Contains(query))
                            {
                                results.Add(new LogResult
                                {
                                    FilePath = filePath,
                                    Line = line.Trim()
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log and continue to the next file
                    Console.WriteLine($"Error reading file: {ex.Message}");
                    continue;
                }
            }
        }

        public class LogResult
        {
            public string FilePath { get; set; }
            public string Line { get; set; }
            public long SizeKB { get; set; }
        }
    }

}





















