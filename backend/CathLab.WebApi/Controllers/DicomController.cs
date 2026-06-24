using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;

[ApiController]
[Route("api/dicom")]
public class DicomController : ControllerBase
{
    private readonly HttpClient _http;
    private readonly string _orthancBase = "http://localhost:8042";

    public DicomController(IHttpClientFactory f) => _http = f.CreateClient("orthanc");

    [HttpGet("file/{orthancInstanceId}")]
    public async Task<IActionResult> GetInstanceFile(string orthancInstanceId)
    {
        var resp = await _http.GetAsync($"{_orthancBase}/instances/{orthancInstanceId}/file");
        if (!resp.IsSuccessStatusCode)
            return StatusCode((int)resp.StatusCode, $"Orthanc error: {resp.ReasonPhrase}");
        var bytes = await resp.Content.ReadAsByteArrayAsync();
        return File(bytes, "application/dicom");
    }

    [HttpGet("study/{studyInstanceUid}/instances")]
    public async Task<IActionResult> GetInstances(string studyInstanceUid)
    {
        var lookupContent = new StringContent(studyInstanceUid, System.Text.Encoding.UTF8, "text/plain");
        var lookup = await _http.PostAsync($"{_orthancBase}/tools/lookup", lookupContent);
        
        var lookupResult = JsonSerializer.Deserialize<JsonElement[]>(
            await lookup.Content.ReadAsStringAsync());

        if (lookupResult == null || lookupResult.Length == 0)
            return NotFound(new { error = "Study not found in Orthanc" });

        var orthancStudyId = lookupResult[0].GetProperty("ID").GetString();

        var studyResp = await _http.GetAsync($"{_orthancBase}/studies/{orthancStudyId}");
        var study = JsonSerializer.Deserialize<JsonElement>(await studyResp.Content.ReadAsStringAsync());
        var seriesIds = study.GetProperty("Series").EnumerateArray().Select(s => s.GetString()).ToList();

        var seriesList = new List<object>();
        foreach (var seriesId in seriesIds)
        {
            var serResp = await _http.GetAsync($"{_orthancBase}/series/{seriesId}");
            var ser = JsonSerializer.Deserialize<JsonElement>(await serResp.Content.ReadAsStringAsync());

            var seriesUid = ser.GetProperty("MainDicomTags").GetProperty("SeriesInstanceUID").GetString();
            var instanceIds = ser.GetProperty("Instances").EnumerateArray().Select(i => i.GetString()).ToList();

            var instanceList = new List<object>();
            foreach (var instanceId in instanceIds)
            {
                var instResp = await _http.GetAsync($"{_orthancBase}/instances/{instanceId}");
                var inst = JsonSerializer.Deserialize<JsonElement>(await instResp.Content.ReadAsStringAsync());
                
                var sopUid = inst.GetProperty("MainDicomTags").GetProperty("SOPInstanceUID").GetString();
                
                // Extraction optionnelle du nombre de frames pour accélérer le traitement côté front
                int frameCount = 1;
                if (inst.TryGetProperty("Metadata", out var meta) && meta.TryGetProperty("NumberOfFrames", out var framesProp))
                {
                    int.TryParse(framesProp.GetString(), out frameCount);
                }

                instanceList.Add(new
                {
                    SOPInstanceUID = sopUid, // PascalCase exact pour le main.js
                    orthancId = instanceId,
                    frameCount = frameCount
                });
            }

            seriesList.Add(new
            {
                StudyInstanceUID = studyInstanceUid,  // Injected pour faciliter le WADO-RS côté front
                SeriesInstanceUID = seriesUid,        // PascalCase exact
                orthancId = seriesId,
                instances = instanceList
            });
        }

        // Utilisation de PropertyNamingPolicy = null pour empêcher .NET de forcer la minuscule au début
        var options = new JsonSerializerOptions { PropertyNamingPolicy = null };
        return ObjectResult(new { StudyInstanceUID = studyInstanceUid, series = seriesList }, options);
    }

    [HttpGet("preview/{orthancInstanceId}")]
    public async Task<IActionResult> Preview(string orthancInstanceId)
    {
        var resp = await _http.GetAsync($"{_orthancBase}/instances/{orthancInstanceId}/preview");
        if (!resp.IsSuccessStatusCode)
            return StatusCode((int)resp.StatusCode, $"Orthanc preview error: {resp.ReasonPhrase}");
        
        var bytes = await resp.Content.ReadAsByteArrayAsync();

        // REGLAGE CORP / COEP CRITIQUE : Ajoute l'en-tête pour autoriser l'affichage des miniatures
        Response.Headers.Append("Cross-Origin-Resource-Policy", "cross-origin");        
        return File(bytes, "image/jpeg");
    }

    [HttpGet("framecount/{orthancInstanceId}")]
    public async Task<IActionResult> GetFrameCount(string orthancInstanceId)
    {
        var resp = await _http.GetAsync($"{_orthancBase}/instances/{orthancInstanceId}/frames");
        if (!resp.IsSuccessStatusCode)
            return StatusCode((int)resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        var frames = JsonSerializer.Deserialize<JsonElement[]>(body);
        return Ok(new { count = frames?.Length ?? 1 });
    }

    [HttpGet("frame/{orthancInstanceId}/{frameIndex}")]
    public async Task<IActionResult> GetFrame(string orthancInstanceId, int frameIndex)
    {
        var resp = await _http.GetAsync($"{_orthancBase}/instances/{orthancInstanceId}/frames/{frameIndex}/rendered");
        if (!resp.IsSuccessStatusCode)
            return StatusCode((int)resp.StatusCode);
        var bytes = await resp.Content.ReadAsByteArrayAsync();
        return File(bytes, "image/jpeg");
    }

    // Helper pour retourner un JSON en respectant notre configuration de casse exacte
    private IActionResult ObjectResult(object value, JsonSerializerOptions options)
    {
        return new ContentResult
        {
            ContentType = "application/json",
            Content = JsonSerializer.Serialize(value, options),
            StatusCode = 200
        };
    }
    
}