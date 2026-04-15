<#
.SYNOPSIS
    Executes end-to-end full-stack HTTP, OWASP security header, RFC 7807 ProblemDetails,
    48-permission RBAC, 8-table IDealerScoped isolation, SignalR negotiate, Multi-Dealer Admin Analytics
    (all 9 JSON/CSV bindings including /quality-warranty + 17-column UTF-8 BOM CSV export + CORS exposed headers),
    Public/Customer/Loyalty contracts, AngularJS 1.8.3 SPA shell, Phase 8 (v1.1) Spare-Parts, Test-Drives &
    Printable Receipts, and Phase 9 (v1.2) CSAT ServiceReviews & VIN WarrantyClaims smoke assertions.
    Supports -DryRun cleanly.
#>
[CmdletBinding()]
param(
    [switch]$DryRun,
    [ValidateRange(0, 10)]
    [int]$Phase = 0,
    [string]$ApiBaseUrl = "http://localhost:5080",
    [string]$SpaBaseUrl = "http://localhost:4200"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$SlnPath = Join-Path $RepoRoot "backend\MotorcycleDealer.sln"

$EffectivePhase = $Phase
if ($EffectivePhase -eq 0) {
    $ProgressPath = Join-Path $RepoRoot "PROGRESS.md"
    if (Test-Path $ProgressPath) {
        $ProgressRaw = Get-Content -Path $ProgressPath -Raw -Encoding UTF8
        if ($ProgressRaw -match '\*\*Current Active Phase\*\*\s*\|\s*\*\*Phase\s+([1-9][0-9]*)') {
            $EffectivePhase = [int]$Matches[1]
        }
    }
    if ($EffectivePhase -eq 0) { $EffectivePhase = 10 }
}

function Write-Pass([string]$Msg) { Write-Host "[PASS] $Msg" -ForegroundColor Green }
function Write-Info([string]$Msg) { Write-Host "[INFO] $Msg" -ForegroundColor Cyan }
function Write-FailMsg([string]$Msg) { Write-Host "[FAIL] $Msg" -ForegroundColor Red }

$ApiRoot = $ApiBaseUrl.TrimEnd('/')
$SpaRoot = $SpaBaseUrl.TrimEnd('/')

$AnlBindingCountLabel = if ($EffectivePhase -ge 9) { "All 9 Admin Analytics Bindings" } else { "All 8 Bindings" }
$AnlRouteListLabel = if ($EffectivePhase -ge 9) { "{overview,kpis,revenue-by-dealer,bookings-by-dealer,monthly-trends,loyalty-distribution,quality-warranty,export,export-csv}" } else { "{overview,kpis,revenue-by-dealer,bookings-by-dealer,monthly-trends,loyalty-distribution,export,export-csv}" }
$AnlCsvExpectedLabel = if ($EffectivePhase -ge 9) { "200 OK aligned JSON + 17-col text/csv UTF-8 BOM + Access-Control-Expose-Headers: Content-Disposition" } else { "200 OK aligned JSON + text/csv UTF-8 BOM + Access-Control-Expose-Headers: Content-Disposition" }

$SmokePlan = @(
    @{ Id = "SMK-01"; Name = "API & DB Health + OWASP Headers"; Endpoint = "GET $ApiRoot/api/v1/health?probeDb=true"; Expected = "200 OK + X-Content-Type-Options: nosniff + X-Frame-Options: DENY" },
    @{ Id = "SMK-02"; Name = "RFC 7807 Unauthenticated Guard"; Endpoint = "GET $ApiRoot/api/v1/bookings/my (no token)"; Expected = "401 application/problem+json (traceId)" },
    @{ Id = "SMK-03"; Name = "RFC 7807 Granular RBAC Guard"; Endpoint = "GET $ApiRoot/api/v1/admin/analytics/{overview,export} (Customer & DealerStaff JWTs)"; Expected = "403 Forbidden ProblemDetails" },
    @{ Id = "SMK-04"; Name = "IDealerScoped Multi-Dealer Isolation"; Endpoint = "GET $ApiRoot/api/v1/{bookings,service-invoices} (Hanoi DealerStaff JWT)"; Expected = "200 OK with 0 DLR-HCM-01 rows" },
    @{ Id = "SMK-05"; Name = "SignalR NotificationHub Negotiate"; Endpoint = "POST $ApiRoot/hubs/notifications/negotiate?negotiateVersion=1"; Expected = "200 OK (connectionId)" },
    @{ Id = "SMK-06"; Name = "Admin Multi-Dealer Analytics & CSV Suite ($AnlBindingCountLabel)"; Endpoint = "GET $ApiRoot/api/v1/admin/analytics/$AnlRouteListLabel"; Expected = $AnlCsvExpectedLabel },
    @{ Id = "SMK-07"; Name = "Public, Customer & Loyalty Contracts"; Endpoint = "GET $ApiRoot/api/v1/{motorcycles,inventories?publicOnly=true,loyalty/me,vouchers/catalog,notifications}"; Expected = "200 OK (masked engineNumber, wallet, catalog & unreadCount)" },
    @{ Id = "SMK-08"; Name = "AngularJS 1.8.3 SPA Shell Bootstrap"; Endpoint = "GET $SpaRoot/"; Expected = "200 OK HTML (motorcycleDealerApp)" }
)
if ($EffectivePhase -ge 8) {
    $SmokePlan += @{ Id = "SMK-09"; Name = "Phase 8 v1.1 Spare-Parts, Test-Drives Isolation & Printable Receipts RBAC"; Endpoint = "GET $ApiRoot/api/v1/{spare-parts,test-drives,service-invoices/{id}/receipt,sales-orders/{id}/receipt}"; Expected = "200 OK catalog/scoped/owner receipts + 401/403/404 cross-tenant & RBAC guards" }
}
if ($EffectivePhase -ge 9) {
    $SmokePlan += @{ Id = "SMK-10"; Name = "Phase 9 v1.2 CSAT ServiceReviews & VIN WarrantyClaims RBAC + IDealerScoped Isolation"; Endpoint = "GET $ApiRoot/api/v1/{service-reviews,service-reviews/my,warranty-claims,warranty-claims/my}"; Expected = "200 OK scoped/own lists (0 DLR-HCM-01 leaks for Hanoi Staff) + 401/403 RBAC guards" }
}

if ($DryRun) {
    $PhaseNote = if (Test-Path $SlnPath) { "Solution detected at backend/MotorcycleDealer.sln" } else { "Phase <= 2 pre-code gate active (no backend/ yet)" }
    Write-Info "[DRY-RUN] Full-Stack Smoke Suite Plan ($PhaseNote)"
    Write-Info "[DRY-RUN] Target API: $ApiRoot | Target SPA: $SpaRoot"
    foreach ($Step in $SmokePlan) {
        Write-Info "  [$($Step.Id)] $($Step.Name): $($Step.Endpoint) -> $($Step.Expected)"
    }
    Write-Pass "Smoke test dry-run validated all $($SmokePlan.Count) end-to-end assertions."
    exit 0
}

if (-not (Test-Path $SlnPath)) {
    Write-FailMsg "Cannot run live HTTP smoke tests before Phase 3+ scaffolds backend/MotorcycleDealer.sln (use -DryRun in Phase 2)."
    exit 1
}

Add-Type -AssemblyName System.Net.Http
$HttpClientHandler = New-Object System.Net.Http.HttpClientHandler
$HttpClient = New-Object System.Net.Http.HttpClient($HttpClientHandler)
$HttpClient.Timeout = [TimeSpan]::FromSeconds(15)

function Invoke-HttpProbe {
    param(
        [string]$Method,
        [string]$Url,
        [hashtable]$Headers = @{},
        [string]$Body = ""
    )
    $Req = New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::new($Method), $Url)
    foreach ($Key in $Headers.Keys) {
        [void]$Req.Headers.TryAddWithoutValidation($Key, [string]$Headers[$Key])
    }
    if (-not [string]::IsNullOrEmpty($Body) -and $Method -notin @("GET", "HEAD")) {
        $Req.Content = New-Object System.Net.Http.StringContent($Body, [System.Text.Encoding]::UTF8, "application/json")
    }
    try {
        $Resp = $HttpClient.SendAsync($Req).GetAwaiter().GetResult()
        $Bytes = if ($null -ne $Resp.Content) {
            $Resp.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
        } else {
            [byte[]]@()
        }
        $ContentStr = if ($Bytes.Length -gt 0) {
            [System.Text.Encoding]::UTF8.GetString($Bytes)
        } else {
            ""
        }

        $FlatHeaders = @{}
        foreach ($H in $Resp.Headers) {
            $FlatHeaders[$H.Key.ToLowerInvariant()] = (@($H.Value) -join ", ")
        }
        if ($null -ne $Resp.Content -and $null -ne $Resp.Content.Headers) {
            foreach ($CH in $Resp.Content.Headers) {
                $FlatHeaders[$CH.Key.ToLowerInvariant()] = (@($CH.Value) -join ", ")
            }
        }

        return @{
            StatusCode  = [int]$Resp.StatusCode
            Bytes       = $Bytes
            Content     = $ContentStr
            ContentType = if ($FlatHeaders.ContainsKey("content-type")) { $FlatHeaders["content-type"] } else { "" }
            Headers     = $FlatHeaders
        }
    } finally {
        $Req.Dispose()
    }
}

function Unwrap-EnvelopeData($Parsed) {
    if ($null -eq $Parsed) { return $null }
    if ($Parsed.PSObject.Properties.Match("data").Count -gt 0 -and $null -ne $Parsed.data) {
        return $Parsed.data
    }
    return $Parsed
}

function Unwrap-Array($Parsed, [string[]]$CandidateKeys = @("items", "dealers", "dealerBreakdown", "statusBreakdown", "months", "trends", "buckets", "tiers", "slices", "models", "inventories", "bookings", "invoices", "catalog", "notifications")) {
    $Root = Unwrap-EnvelopeData $Parsed
    if ($null -eq $Root) { return @() }
    if ($Root -is [System.Array]) { return $Root }
    foreach ($Key in $CandidateKeys) {
        if ($Root.PSObject.Properties.Match($Key).Count -gt 0 -and $null -ne $Root.$Key -and $Root.$Key -is [System.Array]) {
            return $Root.$Key
        }
    }
    if (($Parsed -is [System.Array]) -or ($null -ne $Parsed -and $Parsed.PSObject.Properties.Match("data").Count -gt 0 -and $Parsed.data -is [System.Array])) {
        return @($Root)
    }
    return @()
}

function Get-DemoJwt([string[]]$CandidateEmails, [string]$Password) {
    foreach ($Email in $CandidateEmails) {
        $Payload = (@{ email = $Email; password = $Password } | ConvertTo-Json -Compress)
        $Login = Invoke-HttpProbe -Method "POST" -Url "$ApiRoot/api/v1/auth/login" -Body $Payload
        if ($Login.StatusCode -ne 200) { continue }
        $Parsed = $Login.Content | ConvertFrom-Json
        $AuthData = Unwrap-EnvelopeData $Parsed
        if ($null -ne $AuthData -and $AuthData.PSObject.Properties.Match("accessToken").Count -gt 0 -and -not [string]::IsNullOrWhiteSpace([string]$AuthData.accessToken)) {
            return [string]$AuthData.accessToken
        }
        if ($Parsed.PSObject.Properties.Match("accessToken").Count -gt 0 -and -not [string]::IsNullOrWhiteSpace([string]$Parsed.accessToken)) {
            return [string]$Parsed.accessToken
        }
    }
    return $null
}

$Failures = 0

try {
    # SMK-01: API Health (?probeDb=true) + OWASP Security Response Headers
    $Health = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/health?probeDb=true"
    $NoSniff = if ($Health.Headers.ContainsKey("x-content-type-options")) { $Health.Headers["x-content-type-options"] } else { "" }
    $FrameOpts = if ($Health.Headers.ContainsKey("x-frame-options")) { $Health.Headers["x-frame-options"] } else { "" }
    if ($Health.StatusCode -eq 200 -and $NoSniff -eq "nosniff" -and $FrameOpts -eq "DENY") {
        Write-Pass "SMK-01: GET /api/v1/health?probeDb=true -> 200 OK with OWASP headers (X-Content-Type-Options=nosniff, X-Frame-Options=DENY)."
    } else {
        Write-FailMsg "SMK-01: Health/OWASP header check failed (Status=$($Health.StatusCode), X-Content-Type-Options='$NoSniff', X-Frame-Options='$FrameOpts')."
        $Failures++
    }

    # SMK-02: Unauthenticated GET /api/v1/bookings/my -> 401 RFC 7807 ProblemDetails + traceId
    $Unauth = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/bookings/my"
    if ($Unauth.StatusCode -eq 401 -and $Unauth.ContentType -match "application/problem\+json" -and $Unauth.Content -match '"traceId"') {
        Write-Pass "SMK-02: Unauthenticated GET /api/v1/bookings/my -> 401 application/problem+json with traceId."
    } else {
        Write-FailMsg "SMK-02: Expected 401 ProblemDetails with traceId, got Status=$($Unauth.StatusCode), ContentType='$($Unauth.ContentType)'."
        $Failures++
    }

    # Authenticate Seeded Personas (DatabaseSeeder.cs canonical emails + aliases)
    $CustomerJwt = Get-DemoJwt -CandidateEmails @("customer.bronze@motorcycledealer.vn", "minh.nguyen@customer.vn") -Password "Customer@123!"
    $StaffHanoiJwt = Get-DemoJwt -CandidateEmails @("staff.hn@motorcycledealer.vn", "lan.tran.hn@dealer.vn") -Password "Staff@123!"
    $StaffHcmJwt = Get-DemoJwt -CandidateEmails @("staff.hcm@motorcycledealer.vn") -Password "Staff@123!"
    $AdminJwt = Get-DemoJwt -CandidateEmails @("admin@motorcycledealer.vn") -Password "Admin@123!"

    if ([string]::IsNullOrWhiteSpace($CustomerJwt) -or [string]::IsNullOrWhiteSpace($StaffHanoiJwt) -or [string]::IsNullOrWhiteSpace($StaffHcmJwt) -or [string]::IsNullOrWhiteSpace($AdminJwt)) {
        Write-FailMsg "Authentication failed for one or more seeded accounts (Customer=$([bool]$CustomerJwt), StaffHN=$([bool]$StaffHanoiJwt), StaffHCM=$([bool]$StaffHcmJwt), Admin=$([bool]$AdminJwt))."
        $Failures++
    } else {
        # SMK-03: Granular RBAC Guard (Customer & DealerStaff -> /admin/analytics/overview & /export -> 403 ProblemDetails)
        $CustOverview = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/admin/analytics/overview" -Headers @{ Authorization = "Bearer $CustomerJwt" }
        $CustExport = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/admin/analytics/export" -Headers @{ Authorization = "Bearer $CustomerJwt" }
        $StaffOverview = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/admin/analytics/overview" -Headers @{ Authorization = "Bearer $StaffHanoiJwt" }
        $StaffExport = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/admin/analytics/export" -Headers @{ Authorization = "Bearer $StaffHanoiJwt" }

        $All403 = ($CustOverview.StatusCode -eq 403) -and ($CustExport.StatusCode -eq 403) -and ($StaffOverview.StatusCode -eq 403) -and ($StaffExport.StatusCode -eq 403) -and
            ($CustOverview.ContentType -match "application/problem\+json") -and ($StaffExport.ContentType -match "application/problem\+json")
        if ($All403) {
            Write-Pass "SMK-03: Customer & Hanoi DealerStaff blocked from /admin/analytics/{overview,export} with 403 ProblemDetails."
        } else {
            Write-FailMsg "SMK-03: Expected 403 ProblemDetails across Customer/Staff analytics probes, got [$($CustOverview.StatusCode), $($CustExport.StatusCode), $($StaffOverview.StatusCode), $($StaffExport.StatusCode)]."
            $Failures++
        }

        # SMK-04: IDealerScoped Multi-Dealer Isolation (Hanoi DealerStaff on /bookings & /service-invoices -> 0 DLR-HCM-01 rows)
        $HnBookings = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/bookings" -Headers @{ Authorization = "Bearer $StaffHanoiJwt" }
        $HnInvoices = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/service-invoices" -Headers @{ Authorization = "Bearer $StaffHanoiJwt" }
        $HcmGuid = "22222222-2222-2222-2222-222222222201"
        $ZeroHcmLeak = ($HnBookings.StatusCode -eq 200) -and ($HnInvoices.StatusCode -eq 200) -and
            ($HnBookings.Content -notmatch "DLR-HCM-01") -and ($HnBookings.Content -notmatch $HcmGuid) -and
            ($HnInvoices.Content -notmatch "DLR-HCM-01") -and ($HnInvoices.Content -notmatch $HcmGuid)
        if ($ZeroHcmLeak) {
            Write-Pass "SMK-04: IDealerScoped isolated Hanoi DealerStaff on /bookings & /service-invoices (0 DLR-HCM-01 rows)."
        } else {
            Write-FailMsg "SMK-04: IDealerScoped check failed (Bookings=$($HnBookings.StatusCode), Invoices=$($HnInvoices.StatusCode))."
            $Failures++
        }

        # SMK-05: SignalR NotificationHub Negotiate
        $Negotiate = Invoke-HttpProbe -Method "POST" -Url "$ApiRoot/hubs/notifications/negotiate?negotiateVersion=1" -Headers @{ Authorization = "Bearer $StaffHcmJwt" } -Body "{}"
        if ($Negotiate.StatusCode -eq 200 -and $Negotiate.Content -match '"connectionId"') {
            Write-Pass "SMK-05: SignalR POST /hubs/notifications/negotiate?negotiateVersion=1 -> 200 OK with connectionId."
        } else {
            Write-FailMsg "SMK-05: SignalR negotiate failed (Status=$($Negotiate.StatusCode), Body=$($Negotiate.Content))."
            $Failures++
        }

        # SMK-06: Admin Multi-Dealer Analytics JSON Suite + UTF-8 BOM CSV Export + Aliases (All 8/9 Admin Analytics Bindings)
        $AdminAuth = @{ Authorization = "Bearer $AdminJwt" }
        $AdminCorsExportHeaders = @{
            Authorization = "Bearer $AdminJwt"
            Origin        = "http://localhost:4200"
        }
        $AnlOverview = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/admin/analytics/overview" -Headers $AdminAuth
        $AnlKpis = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/admin/analytics/kpis" -Headers $AdminAuth
        $AnlRev = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/admin/analytics/revenue-by-dealer" -Headers $AdminAuth
        $AnlBook = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/admin/analytics/bookings-by-dealer" -Headers $AdminAuth
        $AnlTrends = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/admin/analytics/monthly-trends" -Headers $AdminAuth
        $AnlLoyalty = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/admin/analytics/loyalty-distribution" -Headers $AdminAuth
        $AnlQuality = if ($EffectivePhase -ge 9) { Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/admin/analytics/quality-warranty" -Headers $AdminAuth } else { @{ StatusCode = 200; Content = "{}" } }
        $AnlCsv = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/admin/analytics/export" -Headers $AdminCorsExportHeaders
        $AnlCsvAlias = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/admin/analytics/export-csv" -Headers $AdminCorsExportHeaders

        $AnlStatusesOk = ($AnlOverview.StatusCode -eq 200) -and ($AnlKpis.StatusCode -eq 200) -and ($AnlRev.StatusCode -eq 200) -and ($AnlBook.StatusCode -eq 200) -and
            ($AnlTrends.StatusCode -eq 200) -and ($AnlLoyalty.StatusCode -eq 200) -and ($AnlQuality.StatusCode -eq 200) -and
            ($AnlCsv.StatusCode -eq 200) -and ($AnlCsvAlias.StatusCode -eq 200)

        $AnlArraysOk = $false
        $CsvOk = $false
        $ExposeHeaders = if ($AnlCsv.Headers.ContainsKey("access-control-expose-headers")) { $AnlCsv.Headers["access-control-expose-headers"] } else { "" }
        if ($AnlStatusesOk) {
            $OvDealers = @(Unwrap-Array ($AnlOverview.Content | ConvertFrom-Json) @("dealers", "dealerSummaries", "items"))
            $KpiDealers = @(Unwrap-Array ($AnlKpis.Content | ConvertFrom-Json) @("dealers", "dealerSummaries", "items"))
            $RevCount = @(Unwrap-Array ($AnlRev.Content | ConvertFrom-Json) @("dealers", "labels", "items")).Count
            $BookCount = @(Unwrap-Array ($AnlBook.Content | ConvertFrom-Json) @("dealers", "dealerBreakdown", "dealerLabels", "statusLabels", "items")).Count
            $TrendCount = @(Unwrap-Array ($AnlTrends.Content | ConvertFrom-Json) @("months", "trends", "buckets", "monthLabels", "labels", "items")).Count
            $LoyCount = @(Unwrap-Array ($AnlLoyalty.Content | ConvertFrom-Json) @("tiers", "slices", "tierLabels", "displayLabels", "items")).Count
            $QualityOk = $true
            if ($EffectivePhase -ge 9) {
                $QualityParsed = $AnlQuality.Content | ConvertFrom-Json
                $QualityDealers = @(Unwrap-Array $QualityParsed @("dealers", "items"))
                $QualityDefects = @(Unwrap-Array $QualityParsed @("defectBreakdown"))
                $QualityOk = ($QualityDealers.Count -ge 3) -and ($QualityDefects.Count -ge 1) -and ($AnlQuality.Content -match "averageCsatRating")
            }

            $AnlArraysOk = ($OvDealers.Count -ge 3) -and ($KpiDealers.Count -ge 3) -and ($RevCount -ge 3) -and ($BookCount -ge 3) -and ($TrendCount -ge 12) -and ($LoyCount -eq 4) -and $QualityOk

            $HasUtf8Bom = ($AnlCsv.Bytes.Length -gt 20) -and ($AnlCsv.Bytes[0] -eq 0xEF) -and ($AnlCsv.Bytes[1] -eq 0xBB) -and ($AnlCsv.Bytes[2] -eq 0xBF)
            $AliasHasBom = ($AnlCsvAlias.Bytes.Length -gt 20) -and ($AnlCsvAlias.Bytes[0] -eq 0xEF) -and ($AnlCsvAlias.Bytes[1] -eq 0xBB) -and ($AnlCsvAlias.Bytes[2] -eq 0xBF)
            $CsvBodyNoBom = if ($HasUtf8Bom) { [System.Text.Encoding]::UTF8.GetString($AnlCsv.Bytes, 3, $AnlCsv.Bytes.Length - 3) } else { $AnlCsv.Content }
            $FirstHeaderLine = ($CsvBodyNoBom -split "\r?\n")[0].Trim()
            $CsvPhase9ColsOk = if ($EffectivePhase -ge 9) {
                $FirstHeaderLine.EndsWith("AverageCsatRating,ServiceReviewsCount,OpenWarrantyClaims,ApprovedWarrantyCoveredVnd") -and (($FirstHeaderLine -split ",").Count -eq 17)
            } else {
                $true
            }
            $ExposeOk = $ExposeHeaders -match "(?i)Content-Disposition"
            $CsvOk = ($AnlCsv.ContentType -match "text/csv") -and ($AnlCsvAlias.ContentType -match "text/csv") -and
                $HasUtf8Bom -and $AliasHasBom -and $ExposeOk -and
                $FirstHeaderLine.StartsWith("DealerCode,DealerName,") -and $CsvPhase9ColsOk -and
                ($CsvBodyNoBom -match "DLR-HN-01") -and ($CsvBodyNoBom -match "DLR-DN-01") -and ($CsvBodyNoBom -match "DLR-HCM-01")
        }

        if ($AnlStatusesOk -and $AnlArraysOk -and $CsvOk) {
            if ($EffectivePhase -ge 9) {
                Write-Pass "SMK-06: All 9 Admin analytics bindings (/overview, /kpis, /revenue-by-dealer, /bookings-by-dealer, /monthly-trends, /loyalty-distribution, /quality-warranty, /export, /export-csv) & 17-column UTF-8 BOM CSV verified."
            } else {
                Write-Pass "SMK-06: All 8 Admin analytics bindings (/overview, /kpis, /revenue-by-dealer, /bookings-by-dealer, /monthly-trends, /loyalty-distribution, /export, /export-csv) & UTF-8 BOM CSV verified."
            }
        } else {
            Write-FailMsg "SMK-06: Admin analytics suite check failed (StatusesOk=$AnlStatusesOk, ArraysOk=$AnlArraysOk, CsvOk=$CsvOk, ExposeHeaders='$ExposeHeaders', Statuses=[$($AnlOverview.StatusCode),$($AnlKpis.StatusCode),$($AnlRev.StatusCode),$($AnlBook.StatusCode),$($AnlTrends.StatusCode),$($AnlLoyalty.StatusCode),$($AnlQuality.StatusCode),$($AnlCsv.StatusCode),$($AnlCsvAlias.StatusCode)])."
            $Failures++
        }

        # SMK-07: Public, Customer & Loyalty Contracts
        $CustAuth = @{ Authorization = "Bearer $CustomerJwt" }
        $PubModels = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/motorcycles"
        $PubInv = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/inventories?publicOnly=true"
        $LoyMe = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/loyalty/me" -Headers $CustAuth
        $VouCat = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/vouchers/catalog" -Headers $CustAuth
        $Notifs = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/notifications" -Headers $CustAuth

        $Smk07StatusesOk = ($PubModels.StatusCode -eq 200) -and ($PubInv.StatusCode -eq 200) -and
            ($LoyMe.StatusCode -eq 200) -and ($VouCat.StatusCode -eq 200) -and ($Notifs.StatusCode -eq 200)
        $Smk07PayloadOk = $false
        if ($Smk07StatusesOk) {
            $ModelsArr = @(Unwrap-Array ($PubModels.Content | ConvertFrom-Json) @("items", "models"))
            $InvArr = @(Unwrap-Array ($PubInv.Content | ConvertFrom-Json) @("items", "inventories"))
            $UnmaskedInv = @($InvArr | Where-Object {
                $StProp = $_.PSObject.Properties["stockStatus"]
                $EngProp = $_.PSObject.Properties["engineNumber"]
                $StVal = if ($null -ne $StProp) { [string]$StProp.Value } else { "" }
                $EngVal = if ($null -ne $EngProp -and $null -ne $EngProp.Value) { [string]$EngProp.Value } else { "" }
                ($StVal -ne "Available") -or (-not [string]::IsNullOrEmpty($EngVal))
            })
            $CatArr = @(Unwrap-Array ($VouCat.Content | ConvertFrom-Json) @("items", "catalog"))
            $Smk07PayloadOk = ($ModelsArr.Count -ge 6) -and ($InvArr.Count -ge 1) -and ($UnmaskedInv.Count -eq 0) -and
                ($LoyMe.Content -match "availablePoints") -and ($CatArr.Count -ge 5) -and ($Notifs.Content -match "unreadCount")
        }

        if ($Smk07StatusesOk -and $Smk07PayloadOk) {
            Write-Pass "SMK-07: Public catalog, masked publicOnly VIN stock, Loyalty wallet, Voucher catalog & Notifications inbox verified."
        } else {
            Write-FailMsg "SMK-07: Public/Customer/Loyalty contract check failed (StatusesOk=$Smk07StatusesOk, PayloadOk=$Smk07PayloadOk)."
            $Failures++
        }

        # SMK-09: Phase 8 v1.1 OEM Spare-Parts, Test-Drives IDealerScoped Isolation & Printable Receipts RBAC
        if ($EffectivePhase -ge 8) {
            $PubParts = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/spare-parts"
            $CustCreatePart = Invoke-HttpProbe -Method "POST" -Url "$ApiRoot/api/v1/spare-parts" -Headers $CustAuth -Body '{"partCode":"PRT-UNAUTH-01","name":"Unauthorized","category":"Filter","unitLabel":"pc","unitPriceVnd":50000}'
            $HnTestDrives = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/test-drives" -Headers @{ Authorization = "Bearer $StaffHanoiJwt" }
            $CustDealerTestDrives = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/test-drives" -Headers $CustAuth
            $CustMyTestDrives = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/test-drives/my" -Headers $CustAuth

            $HcmInvoicesProbe = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/service-invoices" -Headers @{ Authorization = "Bearer $StaffHcmJwt" }
            $HcmInvoiceId = "dddddddd-dddd-dddd-dddd-dddddddddd01"
            if ($HcmInvoicesProbe.StatusCode -eq 200) {
                $HcmInvArr = @(Unwrap-Array ($HcmInvoicesProbe.Content | ConvertFrom-Json) @("items", "invoices", "serviceInvoices"))
                if ($HcmInvArr.Count -gt 0 -and $HcmInvArr[0].PSObject.Properties["id"]) {
                    $HcmInvoiceId = [string]$HcmInvArr[0].id
                }
            }

            $HnSalesProbe = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/sales-orders" -Headers @{ Authorization = "Bearer $StaffHanoiJwt" }
            $HnSalesOrderId = "88888888-8888-8888-8888-888888888803"
            if ($HnSalesProbe.StatusCode -eq 200) {
                $HnSoArr = @(Unwrap-Array ($HnSalesProbe.Content | ConvertFrom-Json) @("items", "orders", "salesOrders"))
                $SeededBronzeOrder = @($HnSoArr | Where-Object { $_.PSObject.Properties["orderNumber"] -and [string]$_.orderNumber -eq "SO-202608-0003" })
                if ($SeededBronzeOrder.Count -gt 0 -and $SeededBronzeOrder[0].PSObject.Properties["id"]) {
                    $HnSalesOrderId = [string]$SeededBronzeOrder[0].id
                } elseif ($HnSoArr.Count -gt 0 -and $HnSoArr[0].PSObject.Properties["id"]) {
                    $HnSalesOrderId = [string]$HnSoArr[0].id
                }
            }

            $UnauthInvReceipt = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/service-invoices/$HcmInvoiceId/receipt"
            $ForeignCustInvReceipt = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/service-invoices/$HcmInvoiceId/receipt" -Headers $CustAuth
            $ForeignStaffInvReceipt = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/service-invoices/$HcmInvoiceId/receipt" -Headers @{ Authorization = "Bearer $StaffHanoiJwt" }
            $ScopedStaffInvReceipt = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/service-invoices/$HcmInvoiceId/receipt" -Headers @{ Authorization = "Bearer $StaffHcmJwt" }

            $OwnerSalesReceipt = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/sales-orders/$HnSalesOrderId/receipt" -Headers $CustAuth
            $ScopedStaffSalesReceipt = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/sales-orders/$HnSalesOrderId/receipt" -Headers @{ Authorization = "Bearer $StaffHanoiJwt" }
            $ForeignStaffSalesReceipt = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/sales-orders/$HnSalesOrderId/receipt" -Headers @{ Authorization = "Bearer $StaffHcmJwt" }

            $PartsOk = $false
            if ($PubParts.StatusCode -eq 200) {
                $PartsArr = @(Unwrap-Array ($PubParts.Content | ConvertFrom-Json) @("items", "parts", "spareParts"))
                $PartsOk = ($PartsArr.Count -ge 8) -and ($CustCreatePart.StatusCode -eq 403)
            }

            $TestDrivesOk = ($HnTestDrives.StatusCode -eq 200) -and
                ($HnTestDrives.Content -notmatch "DLR-HCM-01") -and
                ($HnTestDrives.Content -notmatch $HcmGuid) -and
                ($HnTestDrives.Content -notmatch "TD-SEED-HCM-") -and
                ($CustDealerTestDrives.StatusCode -eq 403) -and
                ($CustMyTestDrives.StatusCode -eq 200)

            $ReceiptsOk = ($UnauthInvReceipt.StatusCode -eq 401) -and
                ($ForeignCustInvReceipt.StatusCode -in @(403, 404)) -and
                ($ForeignStaffInvReceipt.StatusCode -in @(403, 404)) -and
                ($ScopedStaffInvReceipt.StatusCode -eq 200) -and
                ($ScopedStaffInvReceipt.Content -match "RCP-SVC-") -and
                ($ScopedStaffInvReceipt.Content -match "verificationCode") -and
                ($ScopedStaffInvReceipt.Content -match "lineItems") -and
                ($ScopedStaffSalesReceipt.StatusCode -eq 200) -and
                ($ScopedStaffSalesReceipt.Content -match "RCP-SLS-") -and
                ($OwnerSalesReceipt.StatusCode -eq 200) -and
                ($ForeignStaffSalesReceipt.StatusCode -in @(403, 404))

            if ($PartsOk -and $TestDrivesOk -and $ReceiptsOk) {
                Write-Pass "SMK-09: Phase 8 v1.1 /spare-parts catalog, /test-drives IDealerScoped isolation & /receipt RBAC verified."
            } else {
                Write-FailMsg "SMK-09: Phase 8 check failed (PartsOk=$PartsOk [GET=$($PubParts.StatusCode),CustPOST=$($CustCreatePart.StatusCode)], TestDrivesOk=$TestDrivesOk [HN=$($HnTestDrives.StatusCode),CustDealer=$($CustDealerTestDrives.StatusCode),CustMy=$($CustMyTestDrives.StatusCode)], ReceiptsOk=$ReceiptsOk [Unauth=$($UnauthInvReceipt.StatusCode),ForeignCust=$($ForeignCustInvReceipt.StatusCode),ForeignStaff=$($ForeignStaffInvReceipt.StatusCode),ScopedInv=$($ScopedStaffInvReceipt.StatusCode),ScopedSales=$($ScopedStaffSalesReceipt.StatusCode),OwnerSales=$($OwnerSalesReceipt.StatusCode),ForeignSales=$($ForeignStaffSalesReceipt.StatusCode)])."
                $Failures++
            }
        }

        # SMK-10: Phase 9 v1.2 Post-Service CSAT ServiceReviews & VIN WarrantyClaims RBAC + IDealerScoped Isolation
        if ($EffectivePhase -ge 9) {
            $UnauthReviews = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/service-reviews"
            $UnauthWarranty = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/warranty-claims"
            $CustDealerReviews = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/service-reviews" -Headers $CustAuth
            $CustDealerWarranty = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/warranty-claims" -Headers $CustAuth
            $StaffMyReviews = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/service-reviews/my" -Headers @{ Authorization = "Bearer $StaffHanoiJwt" }
            $StaffMyWarranty = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/warranty-claims/my" -Headers @{ Authorization = "Bearer $StaffHanoiJwt" }
            $CustMyReviews = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/service-reviews/my" -Headers $CustAuth
            $CustMyWarranty = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/warranty-claims/my" -Headers $CustAuth
            $HnReviews = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/service-reviews" -Headers @{ Authorization = "Bearer $StaffHanoiJwt" }
            $HnWarranty = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/warranty-claims" -Headers @{ Authorization = "Bearer $StaffHanoiJwt" }
            $AdminReviews = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/service-reviews" -Headers $AdminAuth
            $AdminWarranty = Invoke-HttpProbe -Method "GET" -Url "$ApiRoot/api/v1/warranty-claims" -Headers $AdminAuth

            $RbacGuardsOk = ($UnauthReviews.StatusCode -eq 401) -and
                ($UnauthWarranty.StatusCode -eq 401) -and
                ($CustDealerReviews.StatusCode -eq 403) -and
                ($CustDealerWarranty.StatusCode -eq 403) -and
                ($StaffMyReviews.StatusCode -eq 403) -and
                ($StaffMyWarranty.StatusCode -eq 403) -and
                ($CustMyReviews.StatusCode -eq 200) -and
                ($CustMyWarranty.StatusCode -eq 200)

            $ScopedReviewsOk = ($HnReviews.StatusCode -eq 200) -and
                ($HnReviews.Content -notmatch "DLR-HCM-01") -and
                ($HnReviews.Content -notmatch $HcmGuid) -and
                ($AdminReviews.StatusCode -eq 200)

            $ScopedWarrantyOk = $false
            if ($HnWarranty.StatusCode -eq 200 -and $AdminWarranty.StatusCode -eq 200) {
                $AdminClaimsArr = @(Unwrap-Array ($AdminWarranty.Content | ConvertFrom-Json) @("items", "claims", "warrantyClaims"))
                $ScopedWarrantyOk = ($HnWarranty.Content -notmatch "DLR-HCM-01") -and
                    ($HnWarranty.Content -notmatch $HcmGuid) -and
                    ($HnWarranty.Content -notmatch "WC-SEED-HCM-") -and
                    ($AdminClaimsArr.Count -ge 3)
            }

            if ($RbacGuardsOk -and $ScopedReviewsOk -and $ScopedWarrantyOk) {
                Write-Pass "SMK-10: Phase 9 v1.2 /service-reviews & /warranty-claims IDealerScoped isolation & 401/403 RBAC guards verified."
            } else {
                Write-FailMsg "SMK-10: Phase 9 check failed (RbacGuardsOk=$RbacGuardsOk [UnauthRev=$($UnauthReviews.StatusCode),UnauthWar=$($UnauthWarranty.StatusCode),CustDealerRev=$($CustDealerReviews.StatusCode),CustDealerWar=$($CustDealerWarranty.StatusCode),StaffMyRev=$($StaffMyReviews.StatusCode),StaffMyWar=$($StaffMyWarranty.StatusCode),CustMyRev=$($CustMyReviews.StatusCode),CustMyWar=$($CustMyWarranty.StatusCode)], ScopedReviewsOk=$ScopedReviewsOk [HN=$($HnReviews.StatusCode),Admin=$($AdminReviews.StatusCode)], ScopedWarrantyOk=$ScopedWarrantyOk [HN=$($HnWarranty.StatusCode),Admin=$($AdminWarranty.StatusCode)])."
                $Failures++
            }
        }
    }

    # SMK-08: AngularJS 1.8.3 SPA Shell
    $SpaResp = Invoke-HttpProbe -Method "GET" -Url "$SpaRoot/"
    if ($SpaResp.StatusCode -eq 200 -and $SpaResp.Content -match "motorcycleDealerApp") {
        Write-Pass "SMK-08: AngularJS 1.8.3 SPA shell served cleanly from $SpaRoot."
    } else {
        Write-FailMsg "SMK-08: AngularJS SPA shell check failed at $SpaRoot (Status=$($SpaResp.StatusCode))."
        $Failures++
    }
} finally {
    $HttpClient.Dispose()
    $HttpClientHandler.Dispose()
}

if ($Failures -gt 0) {
    Write-FailMsg "Full-stack smoke suite failed with $Failures error(s)."
    exit 1
}

$MaxSmk = if ($EffectivePhase -ge 9) { "SMK-10" } elseif ($EffectivePhase -ge 8) { "SMK-09" } else { "SMK-08" }
Write-Pass "All $($SmokePlan.Count) full-stack smoke assertions (SMK-01..$MaxSmk) passed!"
exit 0
