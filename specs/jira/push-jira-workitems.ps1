$ErrorActionPreference = "Stop"
$base = "https://abdallah-fahmy.atlassian.net"
$email = $env:JIRA_EMAIL
$token = $env:JIRA_API_TOKEN
$sw = if ($env:JIRA_SOFTWARE_PROJECT) { $env:JIRA_SOFTWARE_PROJECT } else { "CHUB" }
$auth = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("${email}:${token}"))
$headers = @{
    Authorization  = "Basic $auth"
    Accept         = "application/json"
    "Content-Type" = "application/json"
}

function Get-Adf([string]$text) {
    @{
        type    = "doc"
        version = 1
        content = @(@{ type = "paragraph"; content = @(@{ type = "text"; text = $text }) })
    }
}

function Invoke-Jira([string]$method, [string]$path, $bodyObj) {
    $uri = "$base$path"
    if ($null -eq $bodyObj) {
        return Invoke-RestMethod -Method $method -Uri $uri -Headers $headers
    }
    $body = $bodyObj | ConvertTo-Json -Depth 10 -Compress
    return Invoke-RestMethod -Method $method -Uri $uri -Headers $headers -Body $body
}

# Create software project if missing
$needProject = $true
try {
    Invoke-Jira GET "/rest/api/3/project/$sw" $null | Out-Null
    $needProject = $false
    Write-Host "Project $sw already exists"
}
catch { $needProject = $true }

if ($needProject) {
    $created = $false
    $templates = @(
        "com.pyxis.greenhopper.jira:gh-simplified-agility-kanban",
        "com.pyxis.greenhopper.jira:gh-simplified-kanban-classic",
        "com.atlassian.jira-core-project-templates:jira-work-management-simplified-process-tracking"
    )
    foreach ($tpl in $templates) {
        try {
            $payload = @{
                key          = $sw
                name         = "CampusHub"
                templateKey  = $tpl
                accessLevel  = "PRIVATE"
            }
            Invoke-RestMethod -Method Post -Uri "$base/rest/simplified/latest/project" -Headers $headers -Body ($payload | ConvertTo-Json -Compress) | Out-Null
            Write-Host "Created project $sw with $tpl"
            $created = $true
            break
        }
        catch {
            Write-Host "Template $tpl failed: $($_.ErrorDetails.Message)"
        }
    }
    if (-not $created) {
        throw "Could not create software project $sw. Create a Jira Software project and set JIRA_SOFTWARE_PROJECT."
    }
    Start-Sleep -Seconds 3
}

$meta = Invoke-Jira GET "/rest/api/3/issue/createmeta?projectKeys=$sw&expand=projects.issuetypes" $null
$names = @($meta.projects[0].issuetypes | ForEach-Object { $_.name })
Write-Host "Types: $($names -join ', ')"
$epicType = if ($names -contains "Epic") { "Epic" } elseif ($names -contains "Feature") { "Feature" } else { $names[0] }
$storyType = if ($names -contains "Story") { "Story" } elseif ($names -contains "Task") { "Task" } else { $names[0] }

$root = Split-Path -Parent $PSCommandPath
$data = Get-Content -Raw (Join-Path $root "stories.json") | ConvertFrom-Json
$map = Get-Content -Raw (Join-Path $root "jira-keys.json") | ConvertFrom-Json
$ideaById = @{}
foreach ($item in $map.items) { $ideaById[$item.id] = $item.jira }

function New-WorkItem([string]$typeName, [string]$summary, [string]$description, [string]$parentKey) {
    $fields = @{
        project     = @{ key = $sw }
        summary     = $summary
        issuetype   = @{ name = $typeName }
        labels      = @("campushub", "sdd")
        description = (Get-Adf $description)
    }
    if ($parentKey) { $fields.parent = @{ key = $parentKey } }
    return Invoke-Jira POST "/rest/api/3/issue" @{ fields = $fields }
}

function Add-PolarisLink([string]$ideaKey, [string]$workKey) {
    $attempts = @(
        @{ type = @{ name = "Polaris work item link" }; inwardIssue = @{ key = $ideaKey }; outwardIssue = @{ key = $workKey } },
        @{ type = @{ name = "Polaris work item link" }; inwardIssue = @{ key = $workKey }; outwardIssue = @{ key = $ideaKey } },
        @{ type = @{ name = "Relates" }; inwardIssue = @{ key = $ideaKey }; outwardIssue = @{ key = $workKey } }
    )
    foreach ($a in $attempts) {
        try {
            Invoke-Jira POST "/rest/api/3/issueLink" $a | Out-Null
            Write-Host "Linked $workKey <-> $ideaKey"
            return
        }
        catch {
            # try next
        }
    }
    Write-Host "WARN: could not link $workKey to $ideaKey"
}

$out = @()
$epicKeys = @{}
foreach ($epic in $data.epics) {
    $created = New-WorkItem $epicType $epic.summary $epic.description $null
    $epicKeys[$epic.key] = $created.key
    Write-Host "Work item $($created.key) $($epic.summary)"
    $idea = $ideaById[$epic.key]
    if ($idea) { Add-PolarisLink $idea $created.key }
    $out += @{ id = $epic.key; workItem = $created.key; idea = $idea; kind = "plan" }
}

foreach ($story in $data.stories) {
    $parent = $epicKeys[$story.epic]
    $desc = "CampusHub $($story.id). Spec: $($story.spec)."
    $created = New-WorkItem $storyType "$($story.id) $($story.summary)" $desc $parent
    Write-Host "Work item $($created.key) $($story.id)"
    $idea = $ideaById[$story.id]
    if ($idea) { Add-PolarisLink $idea $created.key }
    $out += @{ id = $story.id; workItem = $created.key; idea = $idea; spec = $story.spec; kind = "story" }
}

$path = Join-Path $root "jira-workitems.json"
@{ softwareProject = $sw; items = $out } | ConvertTo-Json -Depth 6 | Set-Content $path
Write-Host "Wrote $path"
