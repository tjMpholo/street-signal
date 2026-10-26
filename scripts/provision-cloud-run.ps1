param([string]$Project='street-signal-app-8f363',[string]$Region='africa-south1')
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
function Invoke-Cloud {
 param([string[]]$Arguments)
 & gcloud @Arguments
 if($LASTEXITCODE -ne 0){throw 'Google Cloud command failed. Review the error above.'}
}
Push-Location $taskRoot
try {
 Invoke-Cloud -Arguments @('services','enable','run.googleapis.com','cloudbuild.googleapis.com','artifactregistry.googleapis.com','secretmanager.googleapis.com',"--project=$Project",'--quiet')
 $runtime="street-signal-dashboard@$Project.iam.gserviceaccount.com"
 & gcloud iam service-accounts describe $runtime "--project=$Project" --format='value(email)' 2>$null
 if($LASTEXITCODE -ne 0){Invoke-Cloud -Arguments @('iam','service-accounts','create','street-signal-dashboard','--display-name=Street Signal dashboard',"--project=$Project",'--quiet')}
 foreach($role in @('roles/datastore.user','roles/firebaseauth.viewer')){
  Invoke-Cloud -Arguments @('projects','add-iam-policy-binding',$Project,"--member=serviceAccount:$runtime","--role=$role",'--condition=None','--quiet','--format=none')
 }
 $settings=Get-Content (Join-Path $taskRoot 'appsettings.json') -Raw | ConvertFrom-Json
 if([string]::IsNullOrWhiteSpace($settings.Firebase.ApiKey) -and (Test-Path (Join-Path $taskRoot '.local/firebase.json'))){
  $localSettings=Get-Content (Join-Path $taskRoot '.local/firebase.json') -Raw | ConvertFrom-Json
  $settings.Firebase.ApiKey=$localSettings.Firebase.ApiKey
 }
 if([string]::IsNullOrWhiteSpace($settings.Firebase.ApiKey)){throw 'Configure Firebase:ApiKey locally first.'}
 $keyFile=Join-Path $taskRoot '.local/cloud-api-key.txt'
 [IO.File]::WriteAllText($keyFile,$settings.Firebase.ApiKey)
 try {
  & gcloud secrets describe street-signal-firebase-api-key "--project=$Project" --format='value(name)' 2>$null
  if($LASTEXITCODE -ne 0){Invoke-Cloud -Arguments @('secrets','create','street-signal-firebase-api-key',"--project=$Project",'--replication-policy=automatic',"--data-file=$keyFile",'--quiet','--format=none')}
  else {Invoke-Cloud -Arguments @('secrets','versions','add','street-signal-firebase-api-key',"--project=$Project","--data-file=$keyFile",'--quiet','--format=none')}
 } finally {Remove-Item -LiteralPath $keyFile -ErrorAction SilentlyContinue}
 Invoke-Cloud -Arguments @('secrets','add-iam-policy-binding','street-signal-firebase-api-key',"--project=$Project","--member=serviceAccount:$runtime",'--role=roles/secretmanager.secretAccessor','--quiet','--format=none')
 & (Join-Path $PSScriptRoot 'deploy-cloud-run.ps1') -RuntimeServiceAccount $runtime -Project $Project -Region $Region
} finally {Pop-Location}
