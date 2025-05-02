using Microsoft.AspNetCore.Mvc;
using GRApplication.Models;
using GRApplication.Models.XmlDtos; // Use correct namespace
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration; // Needed for configuration
using System.Threading.Tasks; // Added for async potential

// --- TODO: Define Your Repository Interfaces (e.g., in an Interfaces folder) ---
public interface IFtpRepository
{
    FtpDetail GetFtpDetail();
    // Example method signatures (adapt to your chosen FTP library/approach)
    // Returns filename downloaded, or null if none found/error
    Task<string?> DownloadFirstOrDefaultFileAsync(string ftpUrl, string fileNamePattern, string localDirectory, string username, string password);
    Task<bool> DeleteFileAsync(string ftpUrl, string fileName, string username, string password);
}
public interface IGRRepository
{
    Task<GoodReceipt?> GetByVectorGRIdAsync(int vectorId); // Make async
    Task AddGoodReceiptAsync(GoodReceipt gr); // Make async
    Task<List<GoodReceipt>> GetAllGoodReceiptsAsync(); // Make async
    Task<GoodReceipt?> GetByIdAsync(int id); // Make async
    Task UpdateGoodReceiptAsync(GoodReceipt gr); // Make async
    // Add Delete methods if needed
}
public interface IGRItemRepository
{
    Task AddGoodReceiptItemAsync(GoodReceiptItem item); // Make async
    // TODO: Add methods to get items by GR Id, update, delete
    Task<List<GoodReceiptItem>> GetItemsByGRIdAsync(int grId); // Example
}

// --- TODO: Define FtpDetail Class (e.g., in Models folder) ---
// Ensure properties match your configuration keys
public class FtpDetail
{
    public string GR_Url { get; set; } = ""; // e.g., "ftp://ftp.drivehq.com/your/gr/path/"
    public string GR_FileNamePattern { get; set; } = "GR_*.xml"; // e.g., "GR_*.xml"
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    // Add Host, Port if needed by your FTP library
}


// --- Controller ---
public class GRController : Controller
{
    // --- Injected Dependencies ---
    private readonly IFtpRepository _ftpRepository;
    private readonly IGRRepository _grRepository;
    private readonly IGRItemRepository _grItemRepository;
    private readonly ILogger<GRController> _logger;
    private readonly IConfiguration _configuration; // Inject configuration

    public GRController(
        IFtpRepository ftpRepository,
        IGRRepository grRepository,
        IGRItemRepository grItemRepository,
        ILogger<GRController> logger,
        IConfiguration configuration) // Add IConfiguration
    {
        _ftpRepository = ftpRepository;
        _grRepository = grRepository;
        _grItemRepository = grItemRepository;
        _logger = logger;
        _configuration = configuration; // Store configuration
    }

    // --- Action for GR List View ---
    public async Task<IActionResult> GR() // Make async if repository calls are async
    {
        // TODO: Replace dummy data with real data fetching once repositories are implemented
        // List<GoodReceipt> grList = await _grRepository.GetAllGoodReceiptsAsync(); // Example async
        List<GoodReceipt> grList = GetDummyGRData(); // Using dummy for now
        return View(grList ?? new List<GoodReceipt>());
    }

    // --- Action for GR Detail View ---
    public async Task<IActionResult> GRDetail(int id) // Make async
    {
        // TODO: Replace dummy data with real data fetching once repositories are implemented
        // GoodReceipt model = await _grRepository.GetByIdAsync(id); // Example async
        List<GoodReceipt> dummyData = GetDummyGRData(); // Using dummy for now
        GoodReceipt model = dummyData.FirstOrDefault(gr => gr.Id == id);

        if (model == null) { return NotFound(); }
        return View(model);
    }

    // --- Action to Trigger FTP Check (called via AJAX)---
    [HttpPost]
    public async Task<IActionResult> CheckForNewGRs() // Make async
    {
        string message = "Check failed. See logs.";
        bool success = false;
        try
        {
            message = await ProcessFTPGRsAsync(); // Call async version
            success = !message.StartsWith("Error") && !message.StartsWith("Check failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during CheckForNewGRs process.");
            message = $"An error occurred: {ex.Message}";
            success = false;
        }
        return Json(new { success = success, message = message });
    }

    // --- Method to Process FTP Files ---
    private async Task<string> ProcessFTPGRsAsync() // Make async
    {
        _logger.LogInformation("Starting ProcessFTPGRsAsync...");

        // --- Get FTP Details from Configuration ---
        // Store these in appsettings.json or User Secrets!
        // Example keys: "FtpSettings:GR_Url", "FtpSettings:Username", etc.
        FtpDetail ftp = new FtpDetail
        {
            GR_Url = _configuration["FtpSettings:GR_Url"] ?? "ftp://DEFAULT_URL/GR/", // Provide defaults or throw if null
            GR_FileNamePattern = _configuration["FtpSettings:GR_FileNamePattern"] ?? "GR_*.xml",
            Username = _configuration["FtpSettings:Username"] ?? "dummy_user",
            Password = _configuration["FtpSettings:Password"] ?? "dummy_pass"
        };
        _logger.LogInformation($"Using FTP URL: {ftp.GR_Url}");
        // --- End Get FTP Details ---

        string localTempPath = Path.Combine(Path.GetTempPath(), "GRDownloads_" + Guid.NewGuid());
        Directory.CreateDirectory(localTempPath);
        string downloadedFilePath = "";
        string? ftpFileNameToDelete = null; // Use nullable string
        string resultMessage = "No new GR files found on FTP.";
        bool fileProcessedSuccessfully = false;

        try
        {
            // === TODO: Implement Robust FTP Download Logic using _ftpRepository ===
            _logger.LogInformation($"Attempting FTP download from {ftp.GR_Url} matching {ftp.GR_FileNamePattern}");
            // This call should connect, find the first matching file, download it to localTempPath,
            // and return the *name* of the downloaded file (or null if none).
            ftpFileNameToDelete = await _ftpRepository.DownloadFirstOrDefaultFileAsync(ftp.GR_Url, ftp.GR_FileNamePattern, localTempPath, ftp.Username, ftp.Password);
            // *** REPLACE ABOVE WITH YOUR ACTUAL FTP REPOSITORY CALL ***
            // === END TODO ===

            if (string.IsNullOrEmpty(ftpFileNameToDelete))
            {
                _logger.LogInformation("No matching file found on FTP or download failed.");
                Directory.Delete(localTempPath, true);
                return resultMessage;
            }

            downloadedFilePath = Path.Combine(localTempPath, ftpFileNameToDelete);
            _logger.LogInformation($"Successfully downloaded '{ftpFileNameToDelete}' to '{downloadedFilePath}'. Processing...");

            string xmlInput = await System.IO.File.ReadAllTextAsync(downloadedFilePath);
            if (string.IsNullOrWhiteSpace(xmlInput))
            {
                _logger.LogWarning($"Downloaded XML file '{ftpFileNameToDelete}' is empty.");
                resultMessage = $"File '{ftpFileNameToDelete}' was empty.";
                fileProcessedSuccessfully = true; // Treat empty file as 'processed' for deletion
            }
            else
            {
                PurchaseNoteXmlArray? grArray = DeserializeXml<PurchaseNoteXmlArray>(xmlInput);

                if (grArray == null || grArray.PurchaseNotes == null || !grArray.PurchaseNotes.Any())
                {
                    resultMessage = $"File '{ftpFileNameToDelete}' processed, but contained no valid PurchaseNote records.";
                    fileProcessedSuccessfully = true; // File had no records, but processed ok
                }
                else
                {
                    _logger.LogInformation($"Deserialized {grArray.PurchaseNotes.Count} PurchaseNote records from '{ftpFileNameToDelete}'.");
                    StringBuilder processLog = new StringBuilder();
                    int newCount = 0;
                    int skippedCount = 0;

                    foreach (var noteXml in grArray.PurchaseNotes)
                    {
                        if (!int.TryParse(noteXml.ReceiptId, out int vectorId) || vectorId <= 0)
                        {
                            _logger.LogWarning($"Skipping record with invalid ReceiptId: {noteXml.ReceiptId}");
                            skippedCount++;
                            continue;
                        }

                        // Check if GR already exists using VectorGRId
                        GoodReceipt? existingGR = await _grRepository.GetByVectorGRIdAsync(vectorId);

                        if (existingGR == null)
                        {
                            GoodReceipt newGR = MapXmlToGoodReceipt(noteXml);

                            // === TODO: Implement Database Save using _grRepository ===
                            await _grRepository.AddGoodReceiptAsync(newGR); // Assumes saves & populates newGR.Id
                            _logger.LogInformation($"Added GoodReceipt. VectorGRId: {newGR.VectorGRId}. Assigned DB ID: {newGR.Id}");
                            // === END TODO ===
                            newCount++;

                            // Save Items
                            foreach (var lineXml in noteXml.Lines)
                            {
                                if (lineXml.IsActive)
                                {
                                    GoodReceiptItem newItem = MapXmlToGoodReceiptItem(lineXml);
                                    // newItem.GoodReceiptId = newGR.Id; // ** LINK TO PARENT if using FK **

                                    // === TODO: Implement Database Save using _grItemRepository ===
                                    await _grItemRepository.AddGoodReceiptItemAsync(newItem);
                                    // === END TODO ===
                                }
                            }
                            processLog.Append($"GR (ReceiptId {noteXml.ReceiptId}) added; ");
                        }
                        else
                        {
                            _logger.LogInformation($"Skipping existing GoodReceipt. VectorGRId: {vectorId}");
                            skippedCount++;
                            processLog.Append($"GR (ReceiptId {noteXml.ReceiptId}) exists; ");
                        }
                    }
                    resultMessage = $"Processed '{ftpFileNameToDelete}': Added {newCount}, Skipped {skippedCount}.";
                    fileProcessedSuccessfully = true;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing GR file '{ftpFileNameToDelete ?? "N/A"}'.");
            resultMessage = $"Error processing file: {ex.Message}";
            fileProcessedSuccessfully = false;
        }
        finally
        {
            if (Directory.Exists(localTempPath))
            {
                try { Directory.Delete(localTempPath, true); _logger.LogDebug($"Deleted local temp folder: {localTempPath}"); } catch (Exception dex) { _logger.LogWarning(dex, $"Failed to delete temp folder: {localTempPath}"); }
            }

            // Delete from FTP only if processed successfully
            if (fileProcessedSuccessfully && !string.IsNullOrEmpty(ftpFileNameToDelete))
            {
                _logger.LogInformation($"Attempting to delete successfully processed file from FTP: {ftpFileNameToDelete}");
                // === TODO: Implement FTP Delete Logic using _ftpRepository ===
                // bool deleteSuccess = await _ftpRepository.DeleteFileAsync(ftp.GR_Url, ftpFileNameToDelete, ftp.Username, ftp.Password);
                // if (!deleteSuccess) { _logger.LogWarning($"Failed to delete file '{ftpFileNameToDelete}' from FTP."); }
                _logger.LogWarning("FTP Delete logic not implemented yet.");
                // === END TODO ===
            }
        }
        _logger.LogInformation("Finished ProcessFTPGRsAsync.");
        return resultMessage;
    }


    // --- Helper Method to Deserialize XML ---
    private T? DeserializeXml<T>(string input) where T : class { /* ... as before ... */ }

    // --- Helper Method to Map XML DTO to Domain Model ---
    private GoodReceipt MapXmlToGoodReceipt(PurchaseNoteXml noteXml) { /* ... as before, using clarified mappings ... */ }
    private GoodReceiptItem MapXmlToGoodReceiptItem(PurchaseNoteLineXml lineXml) { /* ... as before, with defaults for missing fields ... */ }
    private GRStatus MapXmlStatusToEnum(int xmlStatus) { /* ... as before ... */ }


    // --- TODO: Replace Dummy Data Method ---
    private List<GoodReceipt> GetDummyGRData() { /* ... keep for now ... */ }

    // --- Actions for Update/Delete GR Header (Placeholders) ---
    [HttpPost][ValidateAntiForgeryToken] public IActionResult UpdateGR(GoodReceipt model) { /* TODO: Implement DB update via _grRepository */ return RedirectToAction("GRDetail", new { id = model.Id }); }
    [HttpPost][ValidateAntiForgeryToken] public IActionResult DeleteGR(int id) { /* TODO: Implement DB status update/delete via _grRepository */ return RedirectToAction("GR"); }

    // --- Actions for Item Table DataTables and Item Update/Delete (Placeholders) ---
    [HttpPost] // Needs to match DataTable AJAX type
    public async Task<IActionResult> GetGRItems(int id) // Receives GR *Database* ID
    {
        // TODO: Implement DataTables Server-Side Processing Logic
        // 1. Read DataTables parameters (draw, start, length, search, order) from Request.Form
        // 2. Fetch *only* the required page of data from _grItemRepository.GetItemsByGRIdAsync(id),
        //    applying filtering, sorting, and pagination IN THE DATABASE QUERY if possible.
        // 3. Get total counts (totalRecords and filteredRecords) from repository.
        // 4. Return JSON in DataTables format:
        //    return Json(new { draw = draw, recordsFiltered = filteredCount, recordsTotal = totalCount, data = pagedData });

        _logger.LogWarning($"GetGRItems action not fully implemented yet for GR ID: {id}");
        // Returning dummy empty data for now to avoid errors
        var draw = Request.Form["draw"].FirstOrDefault();
        var dummyItems = (await _grRepository.GetByIdAsync(id))?.Items ?? new List<GoodReceiptItem>(); // Example temporary fetch
        return Json(new { draw = draw, recordsFiltered = 0, recordsTotal = 0, data = new List<GoodReceiptItem>() });
    }

    [HttpPost]
    // [ValidateAntiForgeryToken] // Add if called via form post, or implement custom token handling for AJAX
    public async Task<IActionResult> UpdateGRItem([FromBody] GoodReceiptItem itemData) // Assume data sent as JSON body
    {
        // TODO: Implement DB update via _grItemRepository using itemData.Id and other properties
        _logger.LogWarning($"UpdateGRItem action not fully implemented yet for Item ID: {itemData?.Id}");
        // Example response
        if (itemData != null)
        {
            // var success = await _grItemRepository.UpdateItemAsync(itemData);
            // return Json(new { success = success, message = success ? "Item updated." : "Failed to update item." });
            return Json(new { success = true, message = $"Placeholder: Update item {itemData.Id}" });
        }
        return Json(new { success = false, message = "Invalid item data." });
    }

    [HttpPost]
    // [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteGRItem(int itemId) // Can receive simple ID
    {
        // TODO: Implement DB delete/status update via _grItemRepository using itemId
        _logger.LogWarning($"DeleteGRItem action not fully implemented yet for Item ID: {itemId}");
        // Example response
        if (itemId > 0)
        {
            // var success = await _grItemRepository.DeleteItemAsync(itemId);
            // return Json(new { success = success, message = success ? "Item deleted." : "Failed to delete item." });
            return Json(new { success = true, message = $"Placeholder: Delete item {itemId}" });
        }
        return Json(new { success = false, message = "Invalid item ID." });
    }
}