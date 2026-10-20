<#
.SYNOPSIS
    Registers a demo account and uploads the sample PDFs in demo-data/ through the real API, so a
    fresh DocuMind AI instance has something to look at immediately - without hand-crafting
    database rows or bypassing the real upload/validation/indexing pipeline.

.PARAMETER BaseUrl
    Where the API is reachable. Defaults to the Docker Compose frontend's reverse-proxied address
    (http://localhost:8080/api); pass http://localhost:5180/api instead if you're running the API
    directly via `dotnet run`.

.EXAMPLE
    ./scripts/seed-demo-data.ps1
    ./scripts/seed-demo-data.ps1 -BaseUrl http://localhost:5180/api
#>
param(
    [string]$BaseUrl = "http://localhost:8080/api",
    [string]$Email = "demo@documind.local",
    [string]$Password = "DemoPass123!",
    [string]$DisplayName = "Demo User"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$DemoDataDir = Join-Path (Split-Path -Parent $ScriptDir) "demo-data"

Write-Host "Seeding demo data against $BaseUrl ..."

$session = $null
$registerBody = @{ email = $Email; password = $Password; displayName = $DisplayName } | ConvertTo-Json
$headers = @{ "X-Requested-With" = "DocuMindSeedScript"; "Content-Type" = "application/json" }

try {
    $response = Invoke-WebRequest -Uri "$BaseUrl/auth/register" -Method Post -Body $registerBody -Headers $headers -SessionVariable session
    Write-Host "Registered new demo account: $Email"
}
catch {
    if ($_.Exception.Response.StatusCode.value__ -eq 400) {
        Write-Host "Account $Email already exists - logging in instead."
        $loginBody = @{ email = $Email; password = $Password } | ConvertTo-Json
        Invoke-WebRequest -Uri "$BaseUrl/auth/login" -Method Post -Body $loginBody -Headers $headers -SessionVariable session | Out-Null
    }
    else {
        throw
    }
}

$pdfFiles = Get-ChildItem -Path $DemoDataDir -Filter "*.pdf"
if ($pdfFiles.Count -eq 0) {
    throw "No PDFs found in $DemoDataDir"
}

$uploadedIds = @()
foreach ($file in $pdfFiles) {
    Write-Host "Uploading $($file.Name) ..."

    # Invoke-WebRequest's -Form parameter needs PowerShell 7+; this targets Windows PowerShell 5.1
    # too, so the multipart/form-data body is built by hand as raw bytes (passing -Body a byte[]
    # sends it exactly as-is, with no text re-encoding that could corrupt the binary PDF content).
    $boundary = [System.Guid]::NewGuid().ToString()
    $LF = "`r`n"
    $headerText = "--$boundary$LF" +
        "Content-Disposition: form-data; name=`"file`"; filename=`"$($file.Name)`"$LF" +
        "Content-Type: application/pdf$LF$LF"
    $footerText = "$LF--$boundary--$LF"

    $headerBytes = [System.Text.Encoding]::ASCII.GetBytes($headerText)
    $footerBytes = [System.Text.Encoding]::ASCII.GetBytes($footerText)
    $fileBytes = [System.IO.File]::ReadAllBytes($file.FullName)

    $bodyBytes = New-Object byte[] ($headerBytes.Length + $fileBytes.Length + $footerBytes.Length)
    [System.Buffer]::BlockCopy($headerBytes, 0, $bodyBytes, 0, $headerBytes.Length)
    [System.Buffer]::BlockCopy($fileBytes, 0, $bodyBytes, $headerBytes.Length, $fileBytes.Length)
    [System.Buffer]::BlockCopy($footerBytes, 0, $bodyBytes, $headerBytes.Length + $fileBytes.Length, $footerBytes.Length)

    $uploadHeaders = @{ "X-Requested-With" = "DocuMindSeedScript" }
    $uploadResponse = Invoke-WebRequest -Uri "$BaseUrl/documents" -Method Post -ContentType "multipart/form-data; boundary=$boundary" -Body $bodyBytes -Headers $uploadHeaders -WebSession $session
    $document = $uploadResponse.Content | ConvertFrom-Json
    $uploadedIds += $document.id
    Write-Host "  -> document id $($document.id), status $($document.processingStatus)"
}

Write-Host "Waiting for indexing to finish ..."
$deadline = (Get-Date).AddSeconds(30)
foreach ($id in $uploadedIds) {
    while ((Get-Date) -lt $deadline) {
        $statusResponse = Invoke-WebRequest -Uri "$BaseUrl/documents/$id/status" -Headers @{ "X-Requested-With" = "DocuMindSeedScript" } -WebSession $session
        $status = ($statusResponse.Content | ConvertFrom-Json).processingStatus
        if ($status -eq 3) { break }   # Indexed
        if ($status -eq 4) { Write-Warning "Document $id failed to index."; break }
        Start-Sleep -Milliseconds 500
    }
}

Write-Host ""
Write-Host "Done. Log in at the app with:"
Write-Host "  Email:    $Email"
Write-Host "  Password: $Password"
