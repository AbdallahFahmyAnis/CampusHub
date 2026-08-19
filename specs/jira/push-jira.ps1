<#
.SYNOPSIS
  Create CampusHub SDD epics and stories in Jira Cloud.

.EXAMPLE
  $env:JIRA_BASE_URL = "https://your-site.atlassian.net"
  $env:JIRA_EMAIL = "you@example.com"
  $env:JIRA_API_TOKEN = "<api token>"
  $env:JIRA_PROJECT_KEY = "CH"
  pwsh specs/jira/push-jira.ps1
#>
$ErrorActionPreference = "Stop"
$base = $env:JIRA_BASE_URL
$email = $env:JIRA_EMAIL
$token = $env:JIRA_API_TOKEN
$project = $env:JIRA_PROJECT_KEY
if (-not $base -or -not $email -or -not $token -or -not $project) {
    Write-Error "Set JIRA_BASE_URL, JIRA_EMAIL, JIRA_API_TOKEN, and JIRA_PROJECT_KEY."
}

$root = Split-Path -Parent $PSCommandPath
$data = Get-Content -Raw (Join-Path $root "stories.json") | ConvertFrom-Json
$pair = "${email}:${token}"
$bytes = [Text.Encoding]::UTF8.GetBytes($pair)
$auth = [Convert]::ToBase64String($bytes)
$headers = @{
    Authorization = "Basic $auth"
    Accept        = "application/json"
    "Content-Type" = "application/json"
}

function New-JiraIssue([string]$type, [string]$summary, [string]$description, [string]$parentKey) {
    $fields = @{
        project   = @{ key = $project }
        summary   = $summary
        issuetype = @{ name = $type }
        labels    = @("campushub", "sdd")
        description = $description
    }
    if ($parentKey) {
        $fields.parent = @{ key = $parentKey }
    }
    $body = @{ fields = $fields } | ConvertTo-Json -Depth 6
    $url = "$base/rest/api/2/issue"
    return Invoke-RestMethod -Method Post -Uri $url -Headers $headers -Body $body
}

$epicKeys = @{}
foreach ($epic in $data.epics) {
    $created = New-JiraIssue "Epic" $epic.summary $epic.description $null
    $epicKeys[$epic.key] = $created.key
    Write-Host "Epic $($created.key) $($epic.summary)"
}

$mapPath = Join-Path $root "jira-keys.json"
$storiesOut = @()
foreach ($story in $data.stories) {
    $parent = $epicKeys[$story.epic]
    $desc = "Story $($story.id)`nSpec: $($story.spec)`nStatus: Done (shipped). Import maps to Done in Jira after workflow mapping."
    $created = New-JiraIssue "Story" "$($story.id) $($story.summary)" $desc $parent
    Write-Host "Story $($created.key) $($story.id)"
    $storiesOut += @{ id = $story.id; jira = $created.key; spec = $story.spec }
}

@{ epics = $epicKeys; stories = $storiesOut } | ConvertTo-Json -Depth 6 | Set-Content $mapPath
Write-Host "Wrote $mapPath"
Write-Host "Create a Jira Plan with JQL: labels = campushub AND labels = sdd"
