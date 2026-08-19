<#
.SYNOPSIS
  Create CampusHub SDD items as Ideas in Jira Product Discovery project MDP.

.EXAMPLE
  $env:JIRA_EMAIL = "you@example.com"
  $env:JIRA_API_TOKEN = "<https://id.atlassian.com/manage-profile/security/api-tokens>"
  pwsh specs/jira/push-jira.ps1
#>
$ErrorActionPreference = "Stop"
$base = if ($env:JIRA_BASE_URL) { $env:JIRA_BASE_URL.TrimEnd("/") } else { "https://abdallah-fahmy.atlassian.net" }
$email = $env:JIRA_EMAIL
$token = $env:JIRA_API_TOKEN
$project = if ($env:JIRA_PROJECT_KEY) { $env:JIRA_PROJECT_KEY } else { "MDP" }
$issueType = if ($env:JIRA_ISSUE_TYPE) { $env:JIRA_ISSUE_TYPE } else { "Idea" }
$parentKey = if ($env:JIRA_PARENT) { $env:JIRA_PARENT } else { $null }

if (-not $email -or -not $token) {
    Write-Error "Set JIRA_EMAIL and JIRA_API_TOKEN. Create a token at https://id.atlassian.com/manage-profile/security/api-tokens"
}

$root = Split-Path -Parent $PSCommandPath
$data = Get-Content -Raw (Join-Path $root "stories.json") | ConvertFrom-Json
$pair = "${email}:${token}"
$bytes = [Text.Encoding]::UTF8.GetBytes($pair)
$auth = [Convert]::ToBase64String($bytes)
$headers = @{
    Authorization  = "Basic $auth"
    Accept         = "application/json"
    "Content-Type" = "application/json"
}

function Get-Adf([string]$text) {
    @{
        type    = "doc"
        version = 1
        content = @(
            @{
                type    = "paragraph"
                content = @(@{ type = "text"; text = $text })
            }
        )
    }
}

function New-JpdIdea([string]$summary, [string]$description, [string]$parent) {
    $fields = @{
        project     = @{ key = $project }
        summary     = $summary
        issuetype   = @{ name = $issueType }
        labels      = @("campushub", "sdd")
        description = (Get-Adf $description)
    }
    if ($parent) {
        $fields.parent = @{ key = $parent }
    }
    $body = @{ fields = $fields } | ConvertTo-Json -Depth 8 -Compress
    $url = "$base/rest/api/3/issue"
    try {
        return Invoke-RestMethod -Method Post -Uri $url -Headers $headers -Body $body
    }
    catch {
        $resp = $_.ErrorDetails.Message
        if ($resp -match "issuetype" -or $resp -match "parent") {
            Write-Host "Retry without parent / with issue type from createmeta..."
        }
        throw
    }
}

Write-Host "Site $base project $project type $issueType"

$typesUrl = "$base/rest/api/3/issue/createmeta?projectKeys=$project&expand=projects.issuetypes.fields"
$meta = Invoke-RestMethod -Method Get -Uri $typesUrl -Headers $headers
$names = @($meta.projects[0].issuetypes | ForEach-Object { $_.name })
Write-Host "Issue types: $($names -join ', ')"
if ($names.Count -gt 0 -and $names -notcontains $issueType) {
    $issueType = $names[0]
    Write-Host "Using issue type $issueType"
}

$mapPath = Join-Path $root "jira-keys.json"
$created = @()

foreach ($epic in $data.epics) {
    $item = New-JpdIdea $epic.summary $epic.description $parentKey
    Write-Host "Created $($item.key) $($epic.summary)"
    $created += @{ id = $epic.key; jira = $item.key; kind = "plan" }
}

foreach ($story in $data.stories) {
    $desc = "CampusHub story $($story.id). Spec: $($story.spec). Shipped. View: https://abdallah-fahmy.atlassian.net/jira/polaris/projects/MDP/ideas/view/9a59bccf-ce6f-426f-8cec-d8c61b1deeed"
    $item = New-JpdIdea "$($story.id) $($story.summary)" $desc $parentKey
    Write-Host "Created $($item.key) $($story.id)"
    $created += @{ id = $story.id; jira = $item.key; spec = $story.spec; kind = "story" }
}

@{ site = $base; project = $project; items = $created } | ConvertTo-Json -Depth 6 | Set-Content $mapPath
Write-Host "Wrote $mapPath"
Write-Host "Open https://abdallah-fahmy.atlassian.net/jira/polaris/projects/MDP/ideas/view/9a59bccf-ce6f-426f-8cec-d8c61b1deeed"
