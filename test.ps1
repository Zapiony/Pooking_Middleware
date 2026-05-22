[Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
$body = '{"identificador":"admin","password":"admin"}'
$response = $null
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5501/api/v2/booking/auth/login" -Method Post -Body $body -ContentType "application/json"
    Write-Host "Status: $($response.StatusCode)"
    Write-Host "Content: $($response.Content)"
} catch {
    Write-Host "Error!"
    $ex = $_.Exception
    if ($ex.Response -ne $null) {
        Write-Host "Status: $($ex.Response.StatusCode)"
        $stream = $ex.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        Write-Host "Content: $($reader.ReadToEnd())"
    } else {
        Write-Host "Exception: $($ex.Message)"
    }
}
