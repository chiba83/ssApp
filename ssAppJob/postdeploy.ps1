Invoke-Command -ComputerName 10.250.0.1 -ScriptBlock {
    Import-Module WebAdministration
    Start-WebAppPool -Name "ssAppJob"
}
