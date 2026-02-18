# DiskFunctions.ps1 - Version 2.2
# Changelog : Ajout de Get-AvailableDrives pour lister tous les lecteurs disponibles

$script:DiskDriveInfo    = $null
$script:DiskLogicalInfo  = $null
$script:DiskHardwareInfo = $null

function Initialize-DiskContext {
    param(
        [Parameter(Mandatory = $true)]
        [string]$DriveLetter
    )

    $normalizedLetter = $DriveLetter.TrimEnd(':').ToUpper()

    try {
        $logical = Get-WmiObject -Class Win32_LogicalDisk -Filter "DeviceID='$($normalizedLetter):'"
        if ($null -eq $logical) {
            throw "Lecteur '$DriveLetter' introuvable (Win32_LogicalDisk)."
        }

        $partition = Get-Partition -DriveLetter $normalizedLetter -ErrorAction Stop
        $disk      = $partition | Get-Disk -ErrorAction Stop

        $script:DiskLogicalInfo  = $logical
        $script:DiskHardwareInfo = $disk
        $script:DiskDriveInfo    = @{ DriveLetter = $normalizedLetter }

        return $true
    }
    catch {
        $script:DiskDriveInfo    = $null
        $script:DiskLogicalInfo  = $null
        $script:DiskHardwareInfo = $null

        return @{
            Error = "Erreur d'initialisation du disque '$DriveLetter' : $($_.Exception.Message)"
        }
    }
}

# Retourne la liste de tous les lecteurs locaux disponibles
function Get-AvailableDrives {
    try {
        $drives = Get-WmiObject -Class Win32_LogicalDisk | Where-Object { $_.DriveType -in @(2, 3, 4) }

        $result = @{}
        foreach ($drive in $drives) {
            $freeGB  = [math]::Round($drive.FreeSpace / 1GB, 1)
            $totalGB = [math]::Round($drive.Size / 1GB, 1)
            $label   = if ($drive.VolumeName) { $drive.VolumeName } else { "Sans nom" }
            $type    = switch ($drive.DriveType) {
                2 { "Amovible" }
                3 { "Local" }
                4 { "Réseau" }
            }
            # Clé = lettre du lecteur (ex: "C"), valeur = description affichée
            $result[$drive.DeviceID] = "$($drive.DeviceID) — $label ($type) $freeGB/$totalGB GB"
        }

        return $result
    }
    catch {
        return @{
            Error = "Erreur dans Get-AvailableDrives : $($_.Exception.Message)"
        }
    }
}

function Get-DiskInfo {
    param([Parameter(Mandatory = $true)][string]$DriveLetter)

    $initResult = Initialize-DiskContext -DriveLetter $DriveLetter
    if ($initResult -isnot [bool]) { return $initResult }

    try {
        $disk        = $script:DiskLogicalInfo
        $totalGB     = [math]::Round($disk.Size / 1GB, 2)
        $freeGB      = [math]::Round($disk.FreeSpace / 1GB, 2)
        $usedGB      = [math]::Round(($disk.Size - $disk.FreeSpace) / 1GB, 2)
        $percentFree = [math]::Round(($disk.FreeSpace / $disk.Size) * 100, 2)

        return @{
            DeviceID    = $disk.DeviceID
            VolumeName  = if ($disk.VolumeName) { $disk.VolumeName } else { "Sans nom" }
            FileSystem  = $disk.FileSystem
            DriveType   = switch ($disk.DriveType) {
                            2 { "Disque amovible" }
                            3 { "Disque local" }
                            4 { "Lecteur réseau" }
                            5 { "CD-ROM" }
                            default { "Inconnu" }
                          }
            TotalSize   = "$totalGB GB"
            FreeSpace   = "$freeGB GB"
            UsedSpace   = "$usedGB GB"
            PercentFree = "$percentFree %"
        }
    }
    catch {
        return @{ Error = "Erreur dans Get-DiskInfo : $($_.Exception.Message)" }
    }
}

function Get-DiskSerial {
    param([Parameter(Mandatory = $true)][string]$DriveLetter)

    $initResult = Initialize-DiskContext -DriveLetter $DriveLetter
    if ($initResult -isnot [bool]) { return $initResult }

    try {
        $logical  = $script:DiskLogicalInfo
        $hardware = $script:DiskHardwareInfo

        return @{
            DeviceID             = $logical.DeviceID
            VolumeName           = if ($logical.VolumeName) { $logical.VolumeName } else { "Sans nom" }
            VolumeSerialNumber   = if ($logical.VolumeSerialNumber) { $logical.VolumeSerialNumber } else { "Non disponible" }
            HardwareSerialNumber = if ($hardware.SerialNumber) { $hardware.SerialNumber } else { "Non disponible" }
        }
    }
    catch {
        return @{ Error = "Erreur dans Get-DiskSerial : $($_.Exception.Message)" }
    }
}

function Get-FreeSpace {
    param([Parameter(Mandatory = $true)][string]$DriveLetter)

    $initResult = Initialize-DiskContext -DriveLetter $DriveLetter
    if ($initResult -isnot [bool]) { return $initResult }

    try {
        $disk        = $script:DiskLogicalInfo
        $freeGB      = [math]::Round($disk.FreeSpace / 1GB, 2)
        $totalGB     = [math]::Round($disk.Size / 1GB, 2)
        $percentFree = [math]::Round(($disk.FreeSpace / $disk.Size) * 100, 2)

        $status = if ($percentFree -lt 10)   { "⚠️ CRITIQUE" }
                  elseif ($percentFree -lt 20) { "⚠️ ATTENTION" }
                  else                         { "✅ OK" }

        return @{
            DeviceID    = $disk.DeviceID
            FreeSpace   = "$freeGB GB"
            TotalSpace  = "$totalGB GB"
            PercentFree = "$percentFree %"
            Status      = $status
        }
    }
    catch {
        return @{ Error = "Erreur dans Get-FreeSpace : $($_.Exception.Message)" }
    }
}

function Get-CompleteStats {
    param([Parameter(Mandatory = $true)][string]$DriveLetter)

    $initResult = Initialize-DiskContext -DriveLetter $DriveLetter
    if ($initResult -isnot [bool]) { return $initResult }

    try {
        $logical     = $script:DiskLogicalInfo
        $hardware    = $script:DiskHardwareInfo
        $totalGB     = [math]::Round($logical.Size / 1GB, 2)
        $freeGB      = [math]::Round($logical.FreeSpace / 1GB, 2)
        $usedGB      = [math]::Round(($logical.Size - $logical.FreeSpace) / 1GB, 2)
        $percentUsed = [math]::Round((($logical.Size - $logical.FreeSpace) / $logical.Size) * 100, 2)
        $percentFree = [math]::Round(($logical.FreeSpace / $logical.Size) * 100, 2)

        $state = if ($percentFree -lt 10)   { "⚠️ CRITIQUE - Espace insuffisant" }
                 elseif ($percentFree -lt 20) { "⚠️ ATTENTION - Espace faible" }
                 else                         { "✅ OK - Espace suffisant" }

        return @{
            "Lecteur"              = $logical.DeviceID
            "Nom de volume"        = if ($logical.VolumeName) { $logical.VolumeName } else { "Sans nom" }
            "Système de fichiers"  = $logical.FileSystem
            "Type"                 = switch ($logical.DriveType) {
                                        2 { "Disque amovible" }
                                        3 { "Disque local" }
                                        4 { "Lecteur réseau" }
                                        5 { "CD-ROM" }
                                        default { "Inconnu" }
                                     }
            "Taille totale"        = "$totalGB GB"
            "Espace utilisé"       = "$usedGB GB ($percentUsed %)"
            "Espace libre"         = "$freeGB GB ($percentFree %)"
            # "Numéro de série (vol)" = if ($logical.VolumeSerialNumber) { $logical.VolumeSerialNumber } else { "N/A" }
            "Numéro de série (HW)" = if ($hardware.SerialNumber) { $hardware.SerialNumber } else { "N/A" }
            "État"                 = $state
        }
    }
    catch {
        return @{ Error = "Erreur dans Get-CompleteStats : $($_.Exception.Message)" }
    }
}
