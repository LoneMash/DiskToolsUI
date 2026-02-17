function Get-DiskInfo {
    param(
        [Parameter(Mandatory=$true)]
        [string]$DriveLetter
    )
    
    try {
        $disk = Get-WmiObject -Class Win32_LogicalDisk -Filter "DeviceID='$DriveLetter'"
        
        if ($null -eq $disk) {
            return @{
                Error = "Lecteur '$DriveLetter' introuvable"
            }
        }
        
        return @{
            DeviceID = $disk.DeviceID
            VolumeName = if ($disk.VolumeName) { $disk.VolumeName } else { "Sans nom" }
            FileSystem = $disk.FileSystem
            DriveType = switch ($disk.DriveType) {
                2 { "Disque amovible" }
                3 { "Disque local" }
                4 { "Lecteur réseau" }
                5 { "CD-ROM" }
                default { "Inconnu" }
            }
            TotalSize = "{0:N2} GB" -f ($disk.Size / 1GB)
            FreeSpace = "{0:N2} GB" -f ($disk.FreeSpace / 1GB)
            UsedSpace = "{0:N2} GB" -f (($disk.Size - $disk.FreeSpace) / 1GB)
            PercentFree = "{0:N2} %" -f (($disk.FreeSpace / $disk.Size) * 100)
        }
    }
    catch {
        return @{
            Error = "Erreur: $($_.Exception.Message)"
        }
    }
}

function Get-DiskSerial {
    param(
        [Parameter(Mandatory=$true)]
        [string]$DriveLetter
    )
    
    try {
        $disk = Get-WmiObject -Class Win32_LogicalDisk -Filter "DeviceID='$DriveLetter'"
        
        if ($null -eq $disk) {
            return @{
                Error = "Lecteur '$DriveLetter' introuvable"
            }
        }
        
        return @{
            DeviceID = $disk.DeviceID
            VolumeSerialNumber = if ($disk.VolumeSerialNumber) { $disk.VolumeSerialNumber } else { "Non disponible" }
            VolumeName = if ($disk.VolumeName) { $disk.VolumeName } else { "Sans nom" }
        }
    }
    catch {
        return @{
            Error = "Erreur: $($_.Exception.Message)"
        }
    }
}

function Get-FreeSpace {
    param(
        [Parameter(Mandatory=$true)]
        [string]$DriveLetter
    )
    
    try {
        $disk = Get-WmiObject -Class Win32_LogicalDisk -Filter "DeviceID='$DriveLetter'"
        
        if ($null -eq $disk) {
            return @{
                Error = "Lecteur '$DriveLetter' introuvable"
            }
        }
        
        $freeGB = [math]::Round($disk.FreeSpace / 1GB, 2)
        $totalGB = [math]::Round($disk.Size / 1GB, 2)
        $percentFree = [math]::Round(($disk.FreeSpace / $disk.Size) * 100, 2)
        
        return @{
            DeviceID = $disk.DeviceID
            FreeSpace = "$freeGB GB"
            TotalSpace = "$totalGB GB"
            PercentFree = "$percentFree %"
            Status = if ($percentFree -lt 10) { "⚠️ CRITIQUE" } 
                     elseif ($percentFree -lt 20) { "⚠️ ATTENTION" } 
                     else { "✅ OK" }
        }
    }
    catch {
        return @{
            Error = "Erreur: $($_.Exception.Message)"
        }
    }
}

function Get-CompleteStats {
    param(
        [Parameter(Mandatory=$true)]
        [string]$DriveLetter
    )
    
    try {
        $disk = Get-WmiObject -Class Win32_LogicalDisk -Filter "DeviceID='$DriveLetter'"
        
        if ($null -eq $disk) {
            return @{
                Error = "Lecteur '$DriveLetter' introuvable"
            }
        }
        
        $totalGB = [math]::Round($disk.Size / 1GB, 2)
        $freeGB = [math]::Round($disk.FreeSpace / 1GB, 2)
        $usedGB = [math]::Round(($disk.Size - $disk.FreeSpace) / 1GB, 2)
        $percentUsed = [math]::Round((($disk.Size - $disk.FreeSpace) / $disk.Size) * 100, 2)
        $percentFree = [math]::Round(($disk.FreeSpace / $disk.Size) * 100, 2)
        
        return @{
            "Lecteur" = $disk.DeviceID
            "Nom de volume" = if ($disk.VolumeName) { $disk.VolumeName } else { "Sans nom" }
            "Système de fichiers" = $disk.FileSystem
            "Type" = switch ($disk.DriveType) {
                2 { "Disque amovible" }
                3 { "Disque local" }
                4 { "Lecteur réseau" }
                5 { "CD-ROM" }
                default { "Inconnu" }
            }
            "Taille totale" = "$totalGB GB"
            "Espace utilisé" = "$usedGB GB ($percentUsed %)"
            "Espace libre" = "$freeGB GB ($percentFree %)"
            "Numéro de série" = if ($disk.VolumeSerialNumber) { $disk.VolumeSerialNumber } else { "N/A" }
            "État" = if ($percentFree -lt 10) { "⚠️ CRITIQUE - Espace insuffisant" } 
                     elseif ($percentFree -lt 20) { "⚠️ ATTENTION - Espace faible" } 
                     else { "✅ OK - Espace suffisant" }
        }
    }
    catch {
        return @{
            Error = "Erreur lors de la récupération des statistiques: $($_.Exception.Message)"
        }
    }
}