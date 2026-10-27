using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using System.Text;

[ApiController]
[Route("api/dicom")]
[Authorize(Roles = "Admin,Medecin")]
public class DicomController : ControllerBase
{
    private readonly HttpClient _http;
    private readonly ILogger<DicomController> _logger;
    private readonly string _orthancBase = "http://localhost:8042";

    public DicomController(IHttpClientFactory f, ILogger<DicomController> logger)
    {
        _http = f.CreateClient("orthanc");
        _logger = logger;
    }

    [HttpGet("web/{**path}")]
    public async Task<IActionResult> DicomWebProxy(string path, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Contains("..", StringComparison.Ordinal))
        {
            return BadRequest(new { error = "Invalid DICOMweb path." });
        }

        var targetPath = $"{_orthancBase}/dicom-web/{path}{Request.QueryString}";
        using var request = new HttpRequestMessage(HttpMethod.Get, targetPath);

        if (Request.Headers.Accept.Count > 0)
{
    request.Headers.TryAddWithoutValidation("Accept", Request.Headers.Accept.ToArray());
}

        var upstream = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        Response.RegisterForDispose(upstream);

        if (!upstream.IsSuccessStatusCode)
{
    var code = upstream.StatusCode == System.Net.HttpStatusCode.NotFound ? 404 : 502;
    _logger.LogWarning("Orthanc DICOMweb {Path} returned {Status}", path, (int)upstream.StatusCode);
    return StatusCode(code);
}

        var contentType = upstream.Content.Headers.ContentType?.ToString();
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            Response.ContentType = contentType;
        }

        Response.Headers["Cross-Origin-Resource-Policy"] = "cross-origin";
        Response.StatusCode = (int)upstream.StatusCode;

        await using var stream = await upstream.Content.ReadAsStreamAsync(ct);
        await stream.CopyToAsync(Response.BodyWriter.AsStream(), ct);
        return new EmptyResult();
    }

    [HttpGet("file/{orthancInstanceId}")]
    public async Task<IActionResult> GetInstanceFile(string orthancInstanceId)
    {
        var resp = await _http.GetAsync($"{_orthancBase}/instances/{orthancInstanceId}/file");
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Orthanc instance file request failed for {OrthancInstanceId} with status {StatusCode}.", orthancInstanceId, (int)resp.StatusCode);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Orthanc file request failed." });
        }

        var bytes = await resp.Content.ReadAsByteArrayAsync();
        return File(bytes, "application/dicom");
    }

    [HttpGet("study/{studyInstanceUid}/instances")]
    public async Task<IActionResult> GetInstances(string studyInstanceUid)
    {
        var lookupContent = new StringContent(studyInstanceUid, Encoding.UTF8, "text/plain");
        var lookup = await _http.PostAsync($"{_orthancBase}/tools/lookup", lookupContent);

        if (!lookup.IsSuccessStatusCode)
        {
            _logger.LogWarning("Orthanc /tools/lookup failed for study {StudyInstanceUid} with status {StatusCode}.", studyInstanceUid, (int)lookup.StatusCode);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Orthanc study lookup failed." });
        }

        var lookupBody = await lookup.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(lookupBody))
        {
            _logger.LogWarning("Orthanc /tools/lookup returned empty content for study {StudyInstanceUid}.", studyInstanceUid);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Orthanc study lookup returned no data." });
        }

        JsonElement[] lookupResult;
        try
        {
            lookupResult = JsonSerializer.Deserialize<JsonElement[]>(lookupBody) ?? Array.Empty<JsonElement>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Orthanc /tools/lookup returned invalid JSON for study {StudyInstanceUid}.", studyInstanceUid);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Orthanc study lookup response was invalid." });
        }

        if (lookupResult.Length == 0)
            return NotFound(new { error = "Study not found in Orthanc" });

        var orthancStudyId = lookupResult[0].TryGetProperty("ID", out var lookupId) ? lookupId.GetString() : null;
        if (string.IsNullOrWhiteSpace(orthancStudyId))
        {
            _logger.LogWarning("Orthanc /tools/lookup did not include an ID for study {StudyInstanceUid}.", studyInstanceUid);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Orthanc study lookup response was incomplete." });
        }

        var studyResp = await _http.GetAsync($"{_orthancBase}/studies/{orthancStudyId}");
        if (!studyResp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Orthanc /studies/{OrthancStudyId} failed for study {StudyInstanceUid} with status {StatusCode}.", orthancStudyId, studyInstanceUid, (int)studyResp.StatusCode);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Orthanc study fetch failed." });
        }

        var studyBody = await studyResp.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(studyBody))
        {
            _logger.LogWarning("Orthanc /studies/{OrthancStudyId} returned empty content for study {StudyInstanceUid}.", orthancStudyId, studyInstanceUid);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Orthanc study data was empty." });
        }

        JsonElement study;
        try
        {
            study = JsonSerializer.Deserialize<JsonElement>(studyBody);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Orthanc /studies/{OrthancStudyId} returned invalid JSON for study {StudyInstanceUid}.", orthancStudyId, studyInstanceUid);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Orthanc study data was invalid." });
        }

        var seriesIds = study.GetProperty("Series").EnumerateArray().Select(s => s.GetString()).ToList();

        var seriesList = new List<object>();
        foreach (var seriesId in seriesIds)
        {
            var serResp = await _http.GetAsync($"{_orthancBase}/series/{seriesId}");
            if (!serResp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Orthanc /series/{OrthancSeriesId} failed with status {StatusCode}.", seriesId, (int)serResp.StatusCode);
                return StatusCode(StatusCodes.Status502BadGateway, new { error = "Orthanc series fetch failed." });
            }

            var serBody = await serResp.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(serBody))
            {
                _logger.LogWarning("Orthanc /series/{OrthancSeriesId} returned empty content.", seriesId);
                return StatusCode(StatusCodes.Status502BadGateway, new { error = "Orthanc series data was empty." });
            }

            JsonElement ser;
            try
            {
                ser = JsonSerializer.Deserialize<JsonElement>(serBody);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Orthanc /series/{OrthancSeriesId} returned invalid JSON.", seriesId);
                return StatusCode(StatusCodes.Status502BadGateway, new { error = "Orthanc series data was invalid." });
            }

            var seriesUid = ser.GetProperty("MainDicomTags").GetProperty("SeriesInstanceUID").GetString();
            var instanceIds = ser.GetProperty("Instances").EnumerateArray().Select(i => i.GetString()).ToList();

            var instanceList = new List<object>();
            foreach (var instanceId in instanceIds)
            {
                var instResp = await _http.GetAsync($"{_orthancBase}/instances/{instanceId}");
                if (!instResp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Orthanc /instances/{OrthancInstanceId} failed with status {StatusCode}.", instanceId, (int)instResp.StatusCode);
                    return StatusCode(StatusCodes.Status502BadGateway, new { error = "Orthanc instance fetch failed." });
                }

                var instBody = await instResp.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(instBody))
                {
                    _logger.LogWarning("Orthanc /instances/{OrthancInstanceId} returned empty content.", instanceId);
                    return StatusCode(StatusCodes.Status502BadGateway, new { error = "Orthanc instance data was empty." });
                }

                JsonElement inst;
                try
                {
                    inst = JsonSerializer.Deserialize<JsonElement>(instBody);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Orthanc /instances/{OrthancInstanceId} returned invalid JSON.", instanceId);
                    return StatusCode(StatusCodes.Status502BadGateway, new { error = "Orthanc instance data was invalid." });
                }

                var sopUid = inst.GetProperty("MainDicomTags").GetProperty("SOPInstanceUID").GetString();

                int frameCount = 1;
                if (inst.TryGetProperty("Metadata", out var meta) && meta.TryGetProperty("NumberOfFrames", out var framesProp))
                {
                    int.TryParse(framesProp.GetString(), out frameCount);
                }

                instanceList.Add(new
                {
                    SOPInstanceUID = sopUid,
                    orthancId = instanceId,
                    frameCount = frameCount
                });
            }

            seriesList.Add(new
            {
                StudyInstanceUID = studyInstanceUid,
                SeriesInstanceUID = seriesUid,
                orthancId = seriesId,
                instances = instanceList
            });
        }

        var options = new JsonSerializerOptions { PropertyNamingPolicy = null };
        return ObjectResult(new { StudyInstanceUID = studyInstanceUid, series = seriesList }, options);
    }

    [HttpGet("preview/{orthancInstanceId}")]
    public async Task<IActionResult> Preview(string orthancInstanceId)
    {
        var resp = await _http.GetAsync($"{_orthancBase}/instances/{orthancInstanceId}/preview");
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Orthanc preview request failed for {OrthancInstanceId} with status {StatusCode}.", orthancInstanceId, (int)resp.StatusCode);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Orthanc preview request failed." });
        }

        var bytes = await resp.Content.ReadAsByteArrayAsync();
        Response.Headers.Append("Cross-Origin-Resource-Policy", "cross-origin");
        return File(bytes, "image/jpeg");
    }

    [HttpGet("framecount/{orthancInstanceId}")]
    public async Task<IActionResult> GetFrameCount(string orthancInstanceId)
    {
        var resp = await _http.GetAsync($"{_orthancBase}/instances/{orthancInstanceId}/frames");
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Orthanc frame count request failed for {OrthancInstanceId} with status {StatusCode}.", orthancInstanceId, (int)resp.StatusCode);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Orthanc frame count request failed." });
        }

        var body = await resp.Content.ReadAsStringAsync();
        JsonElement[]? frames;
        try
        {
            frames = JsonSerializer.Deserialize<JsonElement[]>(body);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Orthanc frame count response for {OrthancInstanceId} was invalid JSON.", orthancInstanceId);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Orthanc frame count response was invalid." });
        }

        return Ok(new { count = frames?.Length ?? 1 });
    }

    [HttpGet("frame/{orthancInstanceId}/{frameIndex}")]
    public async Task<IActionResult> GetFrame(string orthancInstanceId, int frameIndex)
    {
        var resp = await _http.GetAsync($"{_orthancBase}/instances/{orthancInstanceId}/frames/{frameIndex}/rendered");
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Orthanc rendered frame request failed for {OrthancInstanceId} frame {FrameIndex} with status {StatusCode}.", orthancInstanceId, frameIndex, (int)resp.StatusCode);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Orthanc rendered frame request failed." });
        }

        var bytes = await resp.Content.ReadAsByteArrayAsync();
        return File(bytes, "image/jpeg");
    }

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