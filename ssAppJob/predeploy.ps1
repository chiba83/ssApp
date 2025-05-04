Invoke-Command -ComputerName 10.250.0.1 -ScriptBlock {
    Import-Module WebAdministration
    Stop-WebAppPool -Name "ssAppJob"
    Start-Sleep -Seconds 2
    Remove-Item -Path "C:\inetpub\wwwroot\app\job\*" -Recurse -Force
}
