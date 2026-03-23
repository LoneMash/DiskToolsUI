# PowerShell_Base.ps1 - Version 3.0
# Changelog :
#   2.2 - Ajout de Get-AvailableDrives pour lister tous les lecteurs disponibles
#   3.0 - Ajout de Get-InstalledKB et Get-SystemInfo
#         Renommage en PowerShell_Base (script unique pour toutes les actions)
Import-Module CimCmdlets
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
                4 { "RÃ©seau" }
            }
            # ClÃ© = lettre du lecteur (ex: "C"), valeur = description affichÃ©e
            $result[$drive.DeviceID] = "$($drive.DeviceID) â€ $label ($type) $freeGB/$totalGB GB"
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
                            4 { "Lecteur rÃ©seau" }
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

        $status = if ($percentFree -lt 10)   { "âš ï¸ CRITIQUE" }
                  elseif ($percentFree -lt 20) { "âš ï¸ ATTENTION" }
                  else                         { "âœ… OK" }

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

        $state = if ($percentFree -lt 10)   { "âš ï¸ CRITIQUE - Espace insuffisant" }
                 elseif ($percentFree -lt 20) { "âš ï¸ ATTENTION - Espace faible" }
                 else                         { "âœ… OK - Espace suffisant" }

        return @{
            "Lecteur"              = $logical.DeviceID
            "Nom de volume"        = if ($logical.VolumeName) { $logical.VolumeName } else { "Sans nom" }
            "SystÃ¨me de fichiers"  = $logical.FileSystem
            "Type"                 = switch ($logical.DriveType) {
                                        2 { "Disque amovible" }
                                        3 { "Disque local" }
                                        4 { "Lecteur rÃ©seau" }
                                        5 { "CD-ROM" }
                                        default { "Inconnu" }
                                     }
            "Taille totale"        = "$totalGB GB"
            "Espace utilisÃ©"       = "$usedGB GB ($percentUsed %)"
            "Espace libre"         = "$freeGB GB ($percentFree %)"
            # "NumÃ©ro de sÃ©rie (vol)" = if ($logical.VolumeSerialNumber) { $logical.VolumeSerialNumber } else { "N/A" }
            "NumÃ©ro de sÃ©rie (HW)" = if ($hardware.SerialNumber) { $hardware.SerialNumber } else { "N/A" }
            "Ã‰tat"                 = $state
        }
    }
    catch {
        return @{ Error = "Erreur dans Get-CompleteStats : $($_.Exception.Message)" }
    }
    
}
function Get-AllDisks {
    $disks = Get-WmiObject -Class Win32_LogicalDisk | Where-Object { $_.DriveType -in @(2,3,4) }

    $result = foreach ($disk in $disks) {
        [PSCustomObject]@{
            Lecteur    = $disk.DeviceID
            Nom        = if ($disk.VolumeName) { $disk.VolumeName } else { "Sans nom" }
            Taille     = "{0:N1} GB" -f ($disk.Size / 1GB)
            Libre      = "{0:N1} GB" -f ($disk.FreeSpace / 1GB)
            Systeme    = $disk.FileSystem
        }
    }

    return $result
}

# --------------------------------------------------------------------------
# Fonctions systÃ¨me (hors disque)
# --------------------------------------------------------------------------

function Get-InstalledKB {
    param(
        [string]$Filter = ""
    )

    try {
        $hotfixes = Get-HotFix | Sort-Object -Property InstalledOn -Descending -ErrorAction SilentlyContinue

        if ($Filter -and $Filter -ne "") {
            $hotfixes = $hotfixes | Where-Object {
                $_.HotFixID -like "*$Filter*" -or $_.Description -like "*$Filter*"
            }
        }

        $result = foreach ($kb in $hotfixes) {
            [PSCustomObject]@{
                HotFixID    = $kb.HotFixID
                Description = $kb.Description
                InstalledOn = if ($kb.InstalledOn) { $kb.InstalledOn.ToString("yyyy-MM-dd") } else { "Inconnue" }
                InstalledBy = if ($kb.InstalledBy) { $kb.InstalledBy } else { "Inconnu" }
            }
        }

        return $result
    }
    catch {
        return @{ Error = "Erreur dans Get-InstalledKB : $($_.Exception.Message)" }
    }
}

function Get-SystemInfo {
    try {
        $os   = Get-CimInstance -ClassName Win32_OperatingSystem
        $cpu  = Get-CimInstance -ClassName Win32_Processor | Select-Object -First 1
        $cs   = Get-CimInstance -ClassName Win32_ComputerSystem
        $bios = Get-CimInstance -ClassName Win32_BIOS

        $totalRAM = [math]::Round($cs.TotalPhysicalMemory / 1GB, 1)
        $freeRAM  = [math]::Round($os.FreePhysicalMemory / 1MB, 1)
        $uptime   = (Get-Date) - $os.LastBootUpTime

        return @{
            "Nom du poste"     = $cs.Name
            "Domaine"          = $cs.Domain
            "OS"               = $os.Caption
            "Version"          = $os.Version
            "Build"            = $os.BuildNumber
            "Architecture"     = $os.OSArchitecture
            "Processeur"       = $cpu.Name.Trim()
            "Coeurs"           = "$($cpu.NumberOfCores) coeurs / $($cpu.NumberOfLogicalProcessors) threads"
            "RAM totale"       = "$totalRAM GB"
            "RAM libre"        = "$freeRAM GB"
            "Fabricant"        = $cs.Manufacturer
            "Modele"           = $cs.Model
            "BIOS"             = $bios.SMBIOSBIOSVersion
            "Numero de serie"  = $bios.SerialNumber
            "Uptime"           = "{0}j {1}h {2}m" -f $uptime.Days, $uptime.Hours, $uptime.Minutes
        }
    }
    catch {
        return @{ Error = "Erreur dans Get-SystemInfo : $($_.Exception.Message)" }
    }
}

function Get-UsbHistory {
    # Supprimer toutes les erreurs non-terminantes (accès registre, CIM)
    # pour éviter que CheckErrors() ne les traite comme un échec
    $ErrorActionPreference = 'SilentlyContinue'

    try {
        $usbStorPath = "HKLM:\SYSTEM\CurrentControlSet\Enum\USBSTOR"

        if (-not (Test-Path $usbStorPath)) {
            return @{ Error = "Clé registre USBSTOR introuvable." }
        }

        $results = @()

        foreach ($deviceClass in Get-ChildItem $usbStorPath) {
            $classname = $deviceClass.PSChildName
            $vendor  = ""
            $product = ""

            if ($classname -match "Ven_([^&]+)") { $vendor  = $Matches[1].Replace("_", " ").Trim() }
            if ($classname -match "Prod_([^&]+)") { $product = $Matches[1].Replace("_", " ").Trim() }

            $displayName = if ($vendor -and $product) { "$vendor $product" }
                           elseif ($product) { $product }
                           elseif ($vendor) { $vendor }
                           else { $classname }

            foreach ($instance in Get-ChildItem $deviceClass.PSPath) {
                $serial = $instance.PSChildName
                if ($serial -match "^&\d+$") { $serial = "N/A" }

                $status = "Déconnecté"

                # Statut actuel via PnP
                $escapedSerial = $serial.Replace("\", "\\")
                $pnp = Get-CimInstance -ClassName Win32_PnPEntity -Filter "DeviceID LIKE '%$escapedSerial%'" |
                       Select-Object -First 1
                if ($pnp -and $pnp.Status -eq "OK") { $status = "Connecté" }

                $results += [PSCustomObject]@{
                    Nom         = $displayName
                    'N° série'  = $serial
                    Statut      = $status
                }
            }
        }

        if ($results.Count -eq 0) {
            return @{ Error = "Aucun périphérique USB trouvé dans le registre." }
        }

        return $results | Sort-Object 'Dernière conn.' -Descending
    }
    catch {
        return @{ Error = "Erreur dans Get-UsbHistory : $($_.Exception.Message)" }
    }
}

function Get-ExternalCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Command
    )

    try {
        # Exécuter via cmd /c pour supporter les commandes système, .vbs, .bat, etc.
        $output = cmd /c $Command 2>&1

        # Filtrer les lignes vides et retourner comme texte brut (mode Log)
        $lines = $output | Where-Object { $_ -and $_.ToString().Trim() -ne "" }

        if (-not $lines) {
            return "Aucune sortie retournée par la commande."
        }

        return ($lines | ForEach-Object { $_.ToString() })
    }
    catch {
        return "Erreur lors de l'exécution : $($_.Exception.Message)"
    }
}

function Get-PingLive {
    param(
        [string]$Target     = "localhost",
        [int]$Count         = 20,
        [int]$IntervalMs    = 1000
    )

    for ($i = 1; $i -le $Count; $i++) {
        try {
            $ping = Test-Connection -ComputerName $Target -Count 1 -ErrorAction Stop
            $ms   = $ping.ResponseTime

            $statut = if ($ms -lt 10) { "Excellent" }
                      elseif ($ms -lt 50) { "Bon" }
                      elseif ($ms -lt 100) { "Moyen" }
                      else { "Lent" }

            [PSCustomObject]@{
                '#'       = $i
                Heure     = (Get-Date).ToString("HH:mm:ss")
                Cible     = $Target
                'Temps'   = "$ms ms"
                Statut    = $statut
            }
        }
        catch {
            [PSCustomObject]@{
                '#'       = $i
                Heure     = (Get-Date).ToString("HH:mm:ss")
                Cible     = $Target
                'Temps'   = "---"
                Statut    = "Timeout"
            }
        }

        if ($i -lt $Count) {
            Start-Sleep -Milliseconds $IntervalMs
        }
    }
}